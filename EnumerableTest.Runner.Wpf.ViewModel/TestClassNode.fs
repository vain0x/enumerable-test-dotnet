namespace EnumerableTest.Runner.Wpf

open System.Collections.ObjectModel
open DotNetKit.Observing
open EnumerableTest.Runner

type TestClassNode(assemblyShortName: string, name: string) =
  let children =
    ObservableCollection<TestMethodNode>([||])

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

  member this.Children = children

  member this.AssemblyShortName = assemblyShortName

  member this.Name = name

  member this.TestStatus = testStatus

  member this.IsPassed = isPassed

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

  member this.Update(testClass: TestClass) =
    let difference =
      ReadOnlyList.symmetricDifferenceBy
        (fun node -> (node: TestMethodNode).Name)
        (fun testMethod -> (testMethod: TestMethod).MethodName)
        (children |> Seq.toArray)
        (testClass |> TestClass.testMethods)
    for removedNode in difference.Left do
      children.Remove(removedNode) |> ignore<bool>
    for testMethod in difference.Right do
      let node = TestMethodNode(testMethod.MethodName)
      node.Update(testMethod)
      children.Add(node)
    for (_, node, testMethod) in difference.Intersect do
      node.Update(testMethod)
    this.UpdateTestStatus()
