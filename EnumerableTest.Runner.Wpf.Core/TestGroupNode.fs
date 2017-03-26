namespace EnumerableTest.Runner.Wpf

open System.Collections.ObjectModel
open Reactive.Bindings
open EnumerableTest.Runner
open EnumerableTest.Sdk

[<Sealed>]
type TestGroupNode(groupTest: SerializableGroupTest) =
  inherit TestTreeNode()

  let children =
    groupTest.Tests
    |> Seq.choose
      (function
        | GroupTest groupTest ->
          TestGroupNode(groupTest) :> TestTreeNode |> Some
        | AssertionTest _ ->
          None
      )
    |> ReactiveCollection.ofSeq

  member this.GroupTest = groupTest

  override this.Name =
    groupTest.Name

  override val TestStatistic =
    groupTest |> TestStatistic.ofGroupTest |> ReactiveProperty.create
    :> IReadOnlyReactiveProperty<_>

  override this.Children =
    children

  override val IsExpanded =
    groupTest.IsPassed |> not |> ReactiveProperty.create
    :> IReadOnlyReactiveProperty<_>
