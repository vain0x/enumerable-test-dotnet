namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open DotNetKit.Observing
open EnumerableTest.Runner

type TestClassNode(assemblyShortName: string, name: string) =
  let children =
    ObservableCollection<TestMethodNode>([||])

  let childrenAsUptodateCollection =  children

  let tryFindNode methodName =
    children |> Seq.tryFind (fun ch -> ch.Name = methodName)

  let testStatus = Uptodate.Create(NotCompleted)

  let isPassed =
    testStatus.Select
      (function
        | NotCompleted | Passed ->
          true
        | Violated | Error ->
          false
      )

  let isExpanded =
    isPassed.Select(not)

  let testStatistic =
    children
    |> ReadOnlyUptodateCollection.ofObservableCollection
    |> ReadOnlyUptodateCollection.collect (fun ch -> ch.TestStatistic)
    |> ReadOnlyUptodateCollection.sumBy TestStatistic.groupSig

  member this.Children = children

  member this.AssemblyShortName = assemblyShortName

  member this.Name = name

  member this.TestStatus = testStatus

  member this.TestStatistic = testStatistic

  member this.CalcTestStatus() =
    children
    |> Seq.toArray
    |> Array.map (fun ch -> ch.TestStatus.Value)
    |> TestStatus.ofArray

  member this.UpdateTestStatus() =
    testStatus.Value <- this.CalcTestStatus()

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
    this.UpdateTestStatus()

  member this.Update(testMethod: TestMethod) =
    match children |> Seq.tryFind (fun node -> node.Name = testMethod.MethodName) with
    | Some node ->
      node.Update(testMethod)
    | None ->
      let node = TestMethodNode(testMethod.MethodName)
      node.Update(testMethod)
      children.Insert(0, node)
    this.UpdateTestStatus()

  interface INodeViewModel with
    override this.IsExpanded = isExpanded
