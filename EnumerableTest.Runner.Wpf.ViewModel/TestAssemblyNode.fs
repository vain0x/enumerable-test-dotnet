namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.IO
open System.Reactive.Disposables
open System.Reflection
open System.Threading
open EnumerableTest.Sdk
open EnumerableTest.Runner

type TestAssemblyNode(testAssembly: TestAssembly) =
  let context =
    SynchronizationContext.capture ()

  let children = ObservableCollection<_>()

  let updateSchema (schema: TestSuiteSchema) =
    let difference =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestClassNode).Name)
        (fun testClassSchema -> (testClassSchema: TestClassSchema).TypeFullName)
        (children |> Seq.toArray)
        schema
    for removedNode in difference.Left do
      children.Remove(removedNode) |> ignore<bool>
    for (_, updatedNode, testClassSchema) in difference.Intersect do
      updatedNode.UpdateSchema(testClassSchema)
    for testClassSchema in difference.Right do
      let node = TestClassNode(testClassSchema.TypeFullName)
      node.UpdateSchema(testClassSchema)
      children.Add(node)

  let updateResult (testMethodResult: TestMethodResult) =
    children
    |> Seq.tryFind (fun node -> node.Name = testMethodResult.TypeFullName)
    |> Option.iter (fun node -> node.Update(testMethodResult.Method))

  let testStatistic =
    children
    |> ReadOnlyUptodateCollection.ofObservableCollection
    |> ReadOnlyUptodateCollection.collect
      (fun node -> (node: TestClassNode).TestStatistic |> ReadOnlyUptodateCollection.ofUptodate)
    |> ReadOnlyUptodateCollection.sumBy TestStatistic.groupSig

  let subscription =
    new CompositeDisposable()

  do
    testAssembly.TestSchema
    |> Observable.subscribe
      (fun schema ->
        context |> SynchronizationContext.send
          (fun () -> updateSchema schema)
      )
    |> subscription.Add

  do
    testAssembly.TestMethodCompleted
    |> Observable.subscribe
      (fun result ->
        context |> SynchronizationContext.send
          (fun () -> updateResult result)
      )
    |> subscription.Add

  member this.Name =
    testAssembly.AssemblyName.Name

  member this.Children =
    children

  member this.CancelCommand =
    testAssembly.CancelCommand

  member this.TestStatistic =
    testStatistic

  member this.Dispose() =
    subscription.Dispose()

  interface INodeViewModel with
    override val IsExpanded =
      ReactiveProperty.create true
      |> ReactiveProperty.asReadOnly

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
