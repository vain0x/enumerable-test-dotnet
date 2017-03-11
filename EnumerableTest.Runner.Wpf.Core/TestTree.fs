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
    function
    | MissingNode (node, name, path) ->
      let message =
        sprintf "Node '%s' doesn't have a child node named '%s'." node.Name name
      let data =
        [|
          ("Node", node :> obj)
          ("Path", (name :: path |> List.toArray :> obj))
        |]
      notifier.NotifyWarning(message, data)
    | NotTestMethodNode node ->
      let message =
        sprintf "Node '%s' isn't a test method node." node.Name
      let data =
        [| ("Node", node :> obj) |]
      notifier.NotifyWarning(message, data)

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
      root |> tryRoute (schema.TypeFullName |> Type.FullName.fullPath) |> Option.iter
        (fun node ->
          node.RemoveChild(schema.TypeFullName.Name)
        )

    for schema in difference.Added do
      let node = root.FindOrAddFolderNode(schema.TypeFullName |> Type.FullName.fullPath)
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
    let path = result.TypeFullName |> Type.FullName.fullPath
    root |> tryRoute path |> Option.iter
      (fun classNode ->
        match result.Result with
        | Success testMethodResult ->
          classNode |> tryRouteTestMethodNode [testMethodResult.MethodName] |> Option.iter
            (fun node ->
              node.UpdateResult(testMethodResult)
            )
        | Failure e ->
          // Update one of test method nodes to show the error.
          // It's no problem it has no test method nodes
          // because no need to show the instantiation error in the case.
          classNode.Children |> Seq.tryPick tryCast |> Option.iter
            (fun node ->
              let testMethodResult = TestMethodResult.ofInstantiationError e
              (node: TestMethodNode).UpdateResult(testMethodResult)
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
