namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.Linq
open System.Reactive.Concurrency
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Windows.Input
open Basis.Core
open Reactive.Bindings
open EnumerableTest.Runner

type TestTreeUpdateWarning =
  | MissingNode
    of TestTreeNode * string * list<string>
  | NotTestMethodNode
    of TestTreeNode

[<Sealed>]
type TestTree(runner: PermanentTestRunner, notifier: Notifier) =
  let root = FolderNode.CreateRoot()

  let scheduler = SynchronizationContextScheduler(SynchronizationContext.capture ())

  let notifyWarning =
    fun _ -> todo ""

  let tryRoute path (node: TestTreeNode) =
    match node.RouteOrFailure(path) with
    | Success node ->
      Some node
    | Failure (node, name, path) ->
      notifyWarning (MissingNode (node, name, path))
      None

  let tryRouteTestMethodNode path (node: TestTreeNode) =
    tryRoute path node |> Option.bind
      (fun node ->
        match node with
        | :? TestMethodNode as node ->
          Some node
        | _ ->
          notifyWarning (NotTestMethodNode node)
          None
      )

  let updateSchema cancelCommand (difference: TestSuiteSchemaDifference) =
    for schema in difference.Removed do
      root |> tryRoute (schema.Path |> TestClassPath.fullPath) |> Option.iter
        (fun node ->
          node.RemoveChild(schema.Path.Name)
        )

    for schema in difference.Added do
      let node = root.FindOrAddFolderNode(schema.Path |> TestClassPath.fullPath)
      for schema in schema.Methods do
        node.AddChild(TestMethodNode(schema, cancelCommand))

    for KeyValue (fullPath, difference) in difference.Modified do
      root |> tryRoute fullPath |> Option.iter
        (fun node ->
          for schema in difference.Removed do
            node.RemoveChild(schema.MethodName)
          for KeyValue (name, schema) in difference.Modified do
            node |> tryRouteTestMethodNode [name]  |> Option.iter
              (fun node ->
                node.UpdateSchema(schema)
              )
          for schema in difference.Added do
            node.AddChild(TestMethodNode(schema, cancelCommand))
        )

  let updateResult (result: TestResult) =
    let path = result.TypeFullName |> TestClassPath.ofFullName |> TestClassPath.fullPath
    root |> tryRoute path |> Option.iter
      (fun classNode ->
        match result.Result with
        | Success testMethod ->
          classNode |> tryRouteTestMethodNode [testMethod.MethodName] |> Option.iter
            (fun node ->
              node.UpdateResult(testMethod)
            )
        | Failure e ->
          // Update one of test method nodes to show the error.
          // It's no problem it has no test method nodes
          // because no need to show the instantiation error in the case.
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
        testAssembly.SchemaUpdated.ObserveOn(scheduler)
        |> Observable.subscribe (updateSchema testAssembly.CancelCommand)
        |> subscriptions.Add

        testAssembly.TestResults.ObserveOn(scheduler)
        |> Observable.subscribe updateResult
        |> subscriptions.Add
      )
    |> subscriptions.Add

  member this.Root = root

  member this.Dispose() =
    subscriptions.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
