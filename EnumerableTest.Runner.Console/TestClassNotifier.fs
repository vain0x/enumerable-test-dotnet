namespace EnumerableTest.Runner.Console

open System
open System.Collections.Generic
open System.Reactive.Disposables
open System.Reactive.Subjects
open Basis.Core
open EnumerableTest.Runner

type internal MutableTestClass =
  {
    TypeFullName:
      string
    mutable InstantiationError:
      option<exn>
    Result:
      ResizeArray<TestMethod>
    NotCompletedMethods:
      HashSet<TestMethodSchema>
  }
with
  static member FromSchema(testClassSchema: TestClassSchema): MutableTestClass =
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

  member this.UpdateResult(result: Result<TestMethod, exn>) =
    match result with
    | Success testMethod ->
      let testMethodSchema = { MethodName = testMethod.MethodName }: TestMethodSchema
      if this.NotCompletedMethods.Remove(testMethodSchema) then
        this.Result.Add(testMethod)
    | Failure instantiationError ->
      this.InstantiationError <- Some instantiationError

  member this.MakeReadOnly(): TestClass =
    {
      TypeFullName =
        this.TypeFullName
      InstantiationError =
        this.InstantiationError
      Result =
        this.Result |> Seq.toArray
      NotCompletedMethods =
        this.NotCompletedMethods |> Seq.toArray
    }

  member this.IsCompleted =
    this.InstantiationError |> Option.isSome
    || this.NotCompletedMethods.Count = 0

/// An observable which otifies TestClass instances.
/// 1. Subscribes TestAssembly and collects test results.
/// 2. Whenever a test class is completed, notifies it.
/// 3. When completed, notifies rest test classes (with not-completed test methods) and get disposed.
type TestClassNotifier(testSuiteSchema: TestSuiteSchema, testAssembly: TestAssembly) =
  let classes =
    testSuiteSchema
    |> Seq.map
      (fun testClassSchema ->
        let path = testClassSchema.Path |> TestClassPath.fullPath
        (path, MutableTestClass.FromSchema(testClassSchema))
      )
    |> Dictionary.ofSeq

  let gate =
    obj()

  let subject =
    new Subject<_>()

  let notify path (testClass: MutableTestClass) =
    if classes.Remove(path) then
      subject.OnNext(testClass.MakeReadOnly())
      if classes.Count = 0 then
        subject.OnCompleted()

  let subscription =
    testAssembly.TestResults |> Observable.subscribe
      (fun testResult ->
        let path = testResult.TypeFullName |> TestClassPath.ofFullName |> TestClassPath.fullPath
        lock gate
          (fun () ->
            match classes |> Dictionary.tryFind path with
            | Some testClass ->
              testClass.UpdateResult(testResult.Result)
              if testClass.IsCompleted then
                notify path testClass
            | None ->
              todo "warning"
          )
      )

  member this.Complete() =
    subscription.Dispose()
    lock gate
      (fun () ->
        for KeyValue (path, testClass) in classes |> Seq.toArray do
          notify path testClass
      )
    subject.Dispose()

  member this.Subscribe(observer) =
    subject.Subscribe(observer)

  member this.Dispose() =
    subscription.Dispose()
    subject.Dispose()

  interface IObservable<TestClass> with
    override this.Subscribe(observer) =
      this.Subscribe(observer)

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
