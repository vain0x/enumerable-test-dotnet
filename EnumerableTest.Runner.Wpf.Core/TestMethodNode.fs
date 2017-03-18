namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.Linq
open System.Reactive.Disposables
open System.Windows.Input
open Basis.Core
open Reactive.Bindings
open EnumerableTest.Runner

[<Sealed>]
type TestMethodNode(testMethodSchema: TestMethodSchema, cancelCommand: ICommand) =
  inherit TestTreeNode()

  let name = testMethodSchema.MethodName

  let children =
    ObservableCollection<_>()

  let lastResult = ReactiveProperty.create None
  
  let lastResultUntyped =
    lastResult |> ReactiveProperty.map
      (function
        | Some testMethodResult ->
          testMethodResult :> obj
        | None ->
          NotExecutedResult.Instance :> obj
      )
    :> IReadOnlyReactiveProperty<_>

  let testStatistic =
    lastResult |> ReactiveProperty.map
      (function
        | Some testMethodResult ->
          TestStatistic.ofTestMethod testMethodResult
        | None ->
          TestStatistic.notCompleted
      )
    :> IReadOnlyReactiveProperty<_>

  override this.Name = name

  member this.CancelCommand = cancelCommand

  member this.LastResult = lastResultUntyped

  override this.TestStatistic = testStatistic

  override this.Children = children

  override val IsExpanded =
    testStatistic |> ReactiveProperty.map (TestStatistic.isPassed >> not)
    :> IReadOnlyReactiveProperty<_>

  member this.UpdateSchema(_) =
    lastResult.Value <- None
    children.Clear()

  member this.UpdateResult(testMethodResult) =
    lastResult.Value <- Some testMethodResult
    testMethodResult.Result.Tests |> Seq.choose tryCast |> Seq.iter
      (fun groupTest ->
        children.Add(TestGroupNode(groupTest))
      )
