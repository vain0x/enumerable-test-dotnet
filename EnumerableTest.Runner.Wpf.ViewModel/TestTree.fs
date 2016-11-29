namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.Linq
open System.Reactive.Disposables
open System.Windows.Input
open Basis.Core
open Reactive.Bindings
open EnumerableTest.Runner

[<Sealed>]
type TestTree(runner: PermanentTestRunner) =
  let root = FolderNode.CreateRoot()

  let context = SynchronizationContext.capture ()
  let send f x = context |> SynchronizationContext.send (fun () -> f x)

  let updateSchema cancelCommand (difference: TestSuiteSchemaDifference) =
    for schema in difference.Removed do
      root.TryRoute(schema.Path |> TestClassPath.fullPath) |> Option.iter
        (fun node ->
          node.RemoveChild(schema.Path.Name)
        )

    for schema in difference.Added do
      let node = root.FindOrAddFolderNode(schema.Path |> TestClassPath.fullPath)
      for schema in schema.Methods do
        node.AddChild(TestMethodNode(schema, cancelCommand))

    for KeyValue (fullPath, difference) in difference.Modified do
      root.TryRoute(fullPath) |> Option.iter
        (fun node ->
          for schema in difference.Removed do
            node.RemoveChild(schema.MethodName)
          for KeyValue (name, schema) in difference.Modified do
            node.TryRoute([name]) |> Option.bind tryCast |> Option.iter
              (fun node -> (node: TestMethodNode).UpdateSchema(schema))
          for schema in difference.Added do
            node.AddChild(TestMethodNode(schema, cancelCommand))
        )

  let updateResult (result: TestResult) =
    let path = result.TypeFullName |> TestClassPath.ofFullName |> TestClassPath.fullPath
    root.TryRoute(path) |> Option.iter
      (fun classNode ->
        match result.Result with
        | Success testMethod ->
          classNode.Children
          |> Seq.tryFind (fun n -> n.Name = testMethod.MethodName)
          |> Option.bind tryCast
          |> Option.iter
            (fun node ->
              (node: TestMethodNode).UpdateResult(testMethod)
            )
        | Failure e ->
          // Update one of test method nodes to show the error.
          classNode.Children |> Seq.tryPick tryCast |> Option.iter
            (fun node ->
              let testMethod = TestMethod.ofInstantiationError e
              (node: TestMethodNode).UpdateResult(testMethod)
            )
      )

  let subscriptions = new CompositeDisposable()

  do
    runner.AssemblyAdded |> Observable.subscribe
      (fun testAssembly ->
        testAssembly.SchemaUpdated
        |> Observable.subscribe (updateSchema testAssembly.CancelCommand |> send)
        |> subscriptions.Add

        testAssembly.TestResults
        |> Observable.subscribe (updateResult |> send)
        |> subscriptions.Add
      )
    |> subscriptions.Add

  member this.Root = root

  member this.Dispose() =
    subscriptions.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
