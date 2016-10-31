namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.IO
open System.Reflection
open System.Threading
open Basis.Core
open DotNetKit.Observing
open EnumerableTest
open EnumerableTest.Runner
open EnumerableTest.Sdk

module TestClassResult =
  let toSerializable (testClassResult: TestClassResult) =
    let (testClass, methodResults) =
      testClassResult
    let methodResults =
      methodResults |> Seq.map
        (fun (testMethod, result) ->
          (testMethod.MethodName, result |> Result.toObj)
        )
      |> Seq.toArray
    (testClass.TypeFullName, methodResults)

type TestStatus =
  | NotCompleted
  | Passed
  | Violated
  | Error

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module TestStatus =
  let ofGroupTest (groupTest: GroupTest) =
    if groupTest.IsPassed
      then Passed
      else Violated

  let ofAssertion (assertion: Assertion) =
    if assertion.IsPassed
      then Passed
      else Violated

  let ofTestResult =
    function
    | Success (AssertionTest test) ->
      test.Assertion |> ofAssertion
    | Success (GroupTest test) ->
      test |> ofGroupTest
    | Failure _ ->
      Error

  let ofTestMethodResult =
    function
    | Success groupTest ->
      groupTest |> ofGroupTest
    | Failure _ ->
      Error

type NotExecutedResult() =
  static member val Instance =
    new NotExecutedResult()

type TestMethodNode(name: string) =
  let lastResult = Uptodate.Create(None)

  let lastResultUntyped =
    lastResult.Select
      (function
        | Some (Success test) -> test :> obj
        | Some (Failure testError) -> testError :> obj
        | None -> NotExecutedResult.Instance :> obj
      )

  let testStatus =
    lastResult.Select
      (function
        | Some testMethodResult ->
          TestStatus.ofTestMethodResult testMethodResult
        | None ->
          TestStatus.NotCompleted
      )

  member this.Name = name

  member this.LastResult = lastResultUntyped

  member this.TestStatus = testStatus

  member this.Update() =
    lastResult.Value <- None

  member this.UpdateResult(result: obj) =
    lastResult.Value <- result |> Result.ofObj<GroupTest, TestError>

type TestClassNode(name: string) =
  let children =
    ObservableCollection<TestMethodNode>([||])

  let tryFindNode methodName =
    children |> Seq.tryFind (fun ch -> ch.Name = methodName)

  let isPassed = Uptodate.Create(None)

  let testStatus = Uptodate.Create(NotCompleted)

  member this.Children = children

  member this.Name = name

  member this.TestStatus = testStatus

  member this.Update(testMethodResults: array<string * obj>) =
    let (existingNodes, newTestMethods) =
      testMethodResults |> Seq.paritionMap
        (fun (methodName, result) ->
          match tryFindNode methodName with
          | Some node -> (node, result) |> Some
          | None -> None
        )
    let removedNodes =
      children |> Seq.except (existingNodes |> Seq.map fst) |> Seq.toArray
    for removedNode in removedNodes do
      children.Remove(removedNode) |> ignore<bool>
    for (methodName, result) in newTestMethods do
      let node = TestMethodNode(methodName)
      node.UpdateResult(result)
      children.Add(node)
    for (node, result) in existingNodes do
      node.UpdateResult(result)

module Counter =
  let private counter = ref 0
  let generate () =
    counter |> incr
    !counter

module AppDomain =
  type DisposableAppDomain(appDomain: AppDomain) =
    member this.Value = appDomain

    interface IDisposable with
      override this.Dispose() =
        AppDomain.Unload(appDomain)

  let create name =
    let appDomain = AppDomain.CreateDomain(name, null, AppDomain.CurrentDomain.SetupInformation)
    new DisposableAppDomain(appDomain)

  type private MarshalByRefValue<'x>(value: 'x) =
    inherit MarshalByRefObject()

    member val Value = value with get, set

  let run (f: unit -> 'x) (this: AppDomain) =
    let result = MarshalByRefValue(None)
    this.DoCallBack
      (fun () ->
        result.Value <- f () |> Some
      )
    result.Value |> Option.get

module Model =
  let loadAssembly (assemblyName: AssemblyName) =
    let assembly = Assembly.Load(assemblyName)
    let testSuite = TestSuite.ofAssembly assembly
    let results = ResizeArray()
    let execution =
      testSuite |> TestSuite.runAsync
    execution |> Observable.subscribe
      (fun testClassResult ->
        results.Add(testClassResult |> TestClassResult.toSerializable)
      )
      |> ignore<IDisposable>
    execution.Connect()
    execution |> Observable.wait
    results :> seq<_>

type TestTree() =
  let children = ObservableCollection<TestClassNode>()

  let tryFindChild testClassFullName =
    children |> Seq.tryFind (fun ch -> ch.Name = testClassFullName)

  let disposables = ResizeArray<IDisposable>()

  let context =
    SynchronizationContext.Current

  let watchAssemblyFile (load: unit -> unit) (file: FileInfo) =
    let watcher = new FileSystemWatcher(file.DirectoryName, file.Name)
    watcher.NotifyFilter <- NotifyFilters.LastWrite
    watcher.Changed.Add(fun _ -> load ())
    watcher.EnableRaisingEvents <- true
    disposables.Add(watcher)

  let updateResult result =
    for (testClassFullName, testMethodResults) in result do
      match tryFindChild testClassFullName with
      | Some node ->
        node.Update(testMethodResults)
      | None ->
        let node = TestClassNode(testClassFullName)
        node.Update(testMethodResults)
        children.Add(node)

  member this.LoadAssemblyInNewDomain(assemblyName: AssemblyName) =
    let domainName =
      sprintf "EnumerableTest.Runner[%s]#%d" assemblyName.Name (Counter.generate ())
    use runnerDomain =
      AppDomain.create domainName
    let result =
      runnerDomain.Value |> AppDomain.run (fun () -> Model.loadAssembly assemblyName)
    context.Send ((fun _ -> updateResult result), ())

  member this.LoadFile(file: FileInfo) =
    let assemblyName = AssemblyName.GetAssemblyName(file.FullName)
    let load () = this.LoadAssemblyInNewDomain(assemblyName)
    watchAssemblyFile load file
    load ()

  member this.Children =
    children

  member this.Dispose() =
    for disposable in disposables do
      disposable.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
