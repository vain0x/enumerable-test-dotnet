namespace EnumerableTest.Runner.Wpf

open System
open System.Reactive.Linq
open Basis.Core
open EnumerableTest.Sdk
open EnumerableTest.Runner
open Reactive.Bindings

type TestMethodNode(name: string) =
  let lastResult = new ReactiveProperty<_>(initialValue = (None: option<TestMethod>))

  let lastResultUntyped: ReactiveProperty<_> =
    lastResult |> ReactiveProperty.map
      (function
        | Some testMethod ->
          testMethod :> obj
        | None ->
          NotExecutedResult.Instance :> obj
      )

  let testStatistic =
    lastResult |> ReactiveProperty.map
      (function
        | Some testMethod ->
          TestStatistic.ofTestMethod testMethod
        | None ->
          TestStatistic.notCompleted
      )

  let testStatus =
    testStatistic |> ReactiveProperty.map TestStatus.ofTestStatistic

  member this.Name = name

  member this.LastResult = lastResultUntyped

  member this.TestStatus = testStatus

  member this.TestStatistic = testStatistic

  member this.UpdateSchema() =
    lastResult.Value <- None

  member this.Update(testMethod: TestMethod) =
    lastResult.Value <- Some testMethod

  interface INodeViewModel with
    override val IsExpanded =
      ReactiveProperty.create false
      |> ReactiveProperty.asReadOnly
