namespace EnumerableTest.Runner.Wpf

open Basis.Core
open DotNetKit.Observing
open EnumerableTest.Sdk
open EnumerableTest.Runner

type TestMethodNode(name: string) =
  let lastResult = Uptodate.Create((None: option<TestMethod>))

  let lastResultUntyped =
    lastResult.Select
      (function
        | Some testMethod ->
          testMethod :> obj
        | None ->
          NotExecutedResult.Instance :> obj
      )

  let testStatus =
    lastResult.Select
      (function
        | Some testMethod ->
          TestStatus.ofTestMethod testMethod
        | None ->
          TestStatus.NotCompleted
      )

  member this.Name = name

  member this.LastResult = lastResultUntyped

  member this.TestStatus = testStatus

  member this.UpdateSchema() =
    lastResult.Value <- None

  member this.Update(testMethod: TestMethod) =
    lastResult.Value <- Some testMethod

  interface INodeViewModel with
    override this.IsExpanded =
      Uptodate.False
