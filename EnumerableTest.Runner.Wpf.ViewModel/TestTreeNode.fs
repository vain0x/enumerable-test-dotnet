namespace EnumerableTest.Runner.Wpf

open System
open System.Collections.ObjectModel
open System.Linq
open System.Reactive.Disposables
open System.Windows.Input
open Basis.Core
open Reactive.Bindings
open EnumerableTest.Runner

[<AbstractClass>]
type TestTreeNode() =
  abstract member Name: string

  abstract member Children: ObservableCollection<TestTreeNode>

  abstract member TestStatistic: IReadOnlyReactiveProperty<TestStatistic>

  abstract member IsExpanded: IReadOnlyReactiveProperty<bool>

  member this.AddChild(child) =
    this.Children.Add(child)

  member this.RemoveChild(name) =
    this.Children
    |> Seq.tryFindIndex (fun ch -> ch.Name = name)
    |> Option.iter (fun index -> this.Children.RemoveAt(index))

  member this.RouteOrFailure(path: list<string>): Result<TestTreeNode, TestTreeNode * string * list<string>> =
    match path with
    | [] ->
      this |> Success
    | name :: path ->
      match this.Children |> Seq.tryFind (fun node -> node.Name = name) with
      | Some node ->
        node.RouteOrFailure(path)
      | None ->
        (this, name, path) |> Failure

    member this.TryRoute(path: list<string>) =
      this.RouteOrFailure(path) |> Result.toOption
