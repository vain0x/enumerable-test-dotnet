namespace EnumerableTest.Runner.Wpf

open System.Collections.ObjectModel
open DotNetKit.Observing
open EnumerableTest.Runner

type TestClassNode(name: string) =
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

  member this.Name = name

  member this.TestStatus = testStatus

  member this.IsPassed = isPassed

  member this.CalcTestStatus() =
    let children = children |> Seq.toArray
    let rec loop i current =
      if i = children.Length then
        current
      else
        let loop = loop (i + 1)
        match (current, children.[i].TestStatus.Value) with
        | (_, NotCompleted) | (NotCompleted, _) ->
          NotCompleted
        | (Error, _) | (_, Error) ->
          Error |> loop
        | (Violated, _) | (_, Violated) ->
          Violated |> loop
        | _ ->
          Passed |> loop
    loop 0 Passed

  member this.Update(testClass: TestClass) =
    let (existingNodes, newTestMethods) =
      testClass |> TestClass.testMethods
      |> Seq.paritionMap
        (fun testMethod ->
          match tryFindNode testMethod.MethodName with
          | Some node -> (node, testMethod) |> Some
          | None -> None
        )
    let removedNodes =
      children |> Seq.except (existingNodes |> Seq.map fst) |> Seq.toArray
    for removedNode in removedNodes do
      children.Remove(removedNode) |> ignore<bool>
    for testMethod in newTestMethods do
      let node = TestMethodNode(testMethod.MethodName)
      node.Update(testMethod)
      children.Add(node)
    for (node, testMethod) in existingNodes do
      node.Update(testMethod)
    testStatus.Value <- this.CalcTestStatus()
