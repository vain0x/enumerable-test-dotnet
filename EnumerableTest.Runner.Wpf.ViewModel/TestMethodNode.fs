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

  let lastResult = ReactiveProperty.create None
  
  let lastResultUntyped =
    lastResult |> ReactiveProperty.map
      (function
        | Some testMethod ->
          testMethod :> obj
        | None ->
          NotExecutedResult.Instance :> obj
      )
    :> IReadOnlyReactiveProperty<_>

  let testStatistic =
    lastResult |> ReactiveProperty.map
      (function
        | Some testMethod ->
          TestStatistic.ofTestMethod testMethod
        | None ->
          TestStatistic.notCompleted
      )
    :> IReadOnlyReactiveProperty<_>

  override this.Name = name

  member this.CancelCommand = cancelCommand

  member this.LastResult = lastResultUntyped

  override this.TestStatistic = testStatistic

  override val Children =
    ObservableCollection<TestTreeNode>()

  override val IsExpanded =
    ReactiveProperty.create false :> IReadOnlyReactiveProperty<_>

  member this.UpdateSchema(_) =
    lastResult.Value <- None

  member this.UpdateResult(testMethod) =
    lastResult.Value <- Some testMethod
