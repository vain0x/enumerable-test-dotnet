namespace EnumerableTest.Runner

open System
open System.Collections.Generic
open System.Reactive.Disposables
open System.Reactive.Subjects
open Basis.Core
open EnumerableTest.Runner

type internal MutableTestClassResult =
  {
    TypeFullName:
      string
    mutable InstantiationError:
      option<exn>
    Result:
      ResizeArray<TestMethodResult>
    NotCompletedMethods:
      HashSet<TestMethodSchema>
  }
with
  static member FromSchema(testClassSchema: TestClassSchema): MutableTestClassResult =
    let methods =
      testClassSchema.Methods |> Seq.map
        (fun testMethodSchema ->
          (testMethodSchema.MethodName, None)
        )
      |> Dictionary.ofSeq
    {
      TypeFullName =
        testClassSchema.TypeFullName
      InstantiationError =
        None
      Result =
        ResizeArray()
      NotCompletedMethods =
        HashSet(testClassSchema.Methods)
    }

  member this.UpdateResult(result: Result<TestMethodResult, exn>) =
    match result with
    | Success testMethodResult ->
      let testMethodSchema = { MethodName = testMethodResult.MethodName }: TestMethodSchema
      if this.NotCompletedMethods.Remove(testMethodSchema) then
        this.Result.Add(testMethodResult)
    | Failure instantiationError ->
      this.InstantiationError <- Some instantiationError

  member this.MakeReadOnly(): TestClassResult =
    {
      TypeFullName =
        this.TypeFullName
      InstantiationError =
        this.InstantiationError
      TestMethodResults =
        this.Result |> Seq.toArray
      NotCompletedMethods =
        this.NotCompletedMethods |> Seq.toArray
    }

  member this.IsCompleted =
    this.InstantiationError |> Option.isSome
    || this.NotCompletedMethods.Count = 0

/// An observable which otifies TestClassResult instances.
/// 1. Subscribes TestAssembly and collects test results.
/// 2. Whenever a test class is completed, notifies it.
/// 3. When completed, notifies rest test classes (with not-completed test methods) and get disposed.
type TestClassNotifier(testSuiteSchema: TestSuiteSchema, testAssembly: TestAssembly) =
  let classes =
    testSuiteSchema
    |> Seq.map
      (fun testClassSchema ->
        let path = testClassSchema.Path |> TestClassPath.fullPath
        (path, MutableTestClassResult.FromSchema(testClassSchema))
      )
    |> Dictionary.ofSeq

  let gate =
    obj()

  let subject =
    new Subject<_>()

  let notify path (testClassResult: MutableTestClassResult) =
    if classes.Remove(path) then
      subject.OnNext(testClassResult.MakeReadOnly())
      if classes.Count = 0 then
        subject.OnCompleted()

  let subscription =
    testAssembly.TestResults |> Observable.subscribe
      (fun testResult ->
        let path = testResult.TypeFullName |> TestClassPath.ofFullName |> TestClassPath.fullPath
        lock gate
          (fun () ->
            match classes |> Dictionary.tryFind path with
            | Some testClassResult ->
              testClassResult.UpdateResult(testResult.Result)
              if testClassResult.IsCompleted then
                notify path testClassResult
            | None ->
              todo "warning"
          )
      )

  member this.Complete() =
    subscription.Dispose()
    lock gate
      (fun () ->
        for KeyValue (path, testClassResult) in classes |> Seq.toArray do
          notify path testClassResult
      )
    subject.Dispose()

  member this.Subscribe(observer) =
    subject.Subscribe(observer)

  member this.Dispose() =
    subscription.Dispose()
    subject.Dispose()

  interface IObservable<TestClassResult> with
    override this.Subscribe(observer) =
      this.Subscribe(observer)

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
