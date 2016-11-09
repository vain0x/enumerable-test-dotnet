namespace EnumerableTest.Runner.Wpf

open System
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

  let testStatistic =
    lastResult.Select
      (function
        | Some testMethod ->
          TestStatistic.ofTestMethod testMethod
        | None ->
          TestStatistic.notCompleted
      )

  let testStatus =
    testStatistic.Select(Func<_, _>(TestStatus.ofTestStatistic))

  member this.Name = name

  member this.LastResult = lastResultUntyped

  member this.TestStatus = testStatus

  member this.TestStatistic = testStatistic

  member this.UpdateSchema() =
    lastResult.Value <- None

  member this.Update(testMethod: TestMethod) =
    lastResult.Value <- Some testMethod

  interface INodeViewModel with
    override this.IsExpanded =
      Uptodate.False
