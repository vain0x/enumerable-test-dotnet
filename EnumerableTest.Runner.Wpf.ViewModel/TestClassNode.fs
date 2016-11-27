namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open EnumerableTest.Runner

type TestClassNode(name: string) =
  let children =
    ObservableCollection<TestMethodNode>([||])

  let tryFindNode methodName =
    children |> Seq.tryFind (fun ch -> ch.Name = methodName)

  let testStatistic =
    children
    |> ReadOnlyUptodateCollection.ofObservableCollection
    |> ReadOnlyUptodateCollection.collect
      (fun ch -> ch.TestStatistic |> ReadOnlyUptodateCollection.ofUptodate)
    |> ReadOnlyUptodateCollection.sumBy TestStatistic.groupSig

  let testStatus =
    testStatistic |> ReactiveProperty.map TestStatus.ofTestStatistic

  let isPassed =
    testStatus |> ReactiveProperty.map
      (function
        | NotCompleted | Passed ->
          true
        | Violated | Error ->
          false
      )

  let isExpanded =
    isPassed |> ReactiveProperty.map not

  member this.Children = children

  member this.Name = name

  member this.TestStatus = testStatus

  member this.TestStatistic = testStatistic

  member this.UpdateSchema(testClassSchema: TestClassSchema) =
    let difference =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestMethodNode).Name)
        (fun testMethodSchema -> (testMethodSchema: TestMethodSchema).MethodName)
        (children |> Seq.toArray)
        testClassSchema.Methods
    for removedNode in difference.Left do
      children.Remove(removedNode) |> ignore<bool>
    for (_, node, _) in difference.Intersect do
      node.UpdateSchema()
    for testMethodSchema in difference.Right do
      children.Add(TestMethodNode(testMethodSchema.MethodName))

  member this.Update(testMethod: TestMethod) =
    match children |> Seq.tryFind (fun node -> node.Name = testMethod.MethodName) with
    | Some node ->
      node.Update(testMethod)
    | None ->
      let node = TestMethodNode(testMethod.MethodName)
      node.Update(testMethod)
      children.Insert(0, node)

  interface INodeViewModel with
    override this.IsExpanded = isExpanded |> ReactiveProperty.asReadOnly
