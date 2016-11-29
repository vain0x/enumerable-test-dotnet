namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.Linq
open System.Reactive.Disposables
open System.Windows.Input
open Basis.Core
open Reactive.Bindings
open EnumerableTest.Runner

/// Represents a test class or a namespace.
[<Sealed>]
type FolderNode(name: string) =
  inherit TestTreeNode()

  let children = ObservableCollection<TestTreeNode>()

  let testStatistic =
    children
    |> ReadOnlyUptodateCollection.ofObservableCollection
    |> ReadOnlyUptodateCollection.collect
      (fun ch -> ch.TestStatistic |> ReadOnlyUptodateCollection.ofUptodate)
    |> ReadOnlyUptodateCollection.sumBy TestStatistic.groupSig
    :> IReadOnlyReactiveProperty<_>

  override this.Name = name

  override this.Children = children

  override this.TestStatistic = testStatistic

  override val IsExpanded =
    testStatistic |> ReactiveProperty.map
      (fun testStatistic ->
        testStatistic.AssertionCount
        |> AssertionCount.isAllGreen
        |> not
      )
    :> IReadOnlyReactiveProperty<_>

  member this.FindOrAddFolderNode(path: list<string>) =
    match this.RouteOrFailure(path) with
    | Success node ->
      node
    | Failure (parent, path) ->
      let rec loop (parent: TestTreeNode) =
        function
        | [] ->
          parent
        | name :: path ->
          let node = FolderNode(name)
          parent.AddChild(node)
          loop node path
      loop parent path

  static member CreateRoot() =
    FolderNode("")
