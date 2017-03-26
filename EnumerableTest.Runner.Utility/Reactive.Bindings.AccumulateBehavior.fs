namespace EnumerableTest.Runner

open System
open System.Collections
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Collections.Specialized
open System.ComponentModel
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open Basis.Core
open Reactive.Bindings
open EnumerableTest.Runner

type AccumulateBehavior<'x> internal (group: GroupSig<'x>) =
  let accumulation = ReactiveProperty.create group.Unit

  let subscriptions = Dictionary<obj, _ * ResizeArray<_>>()

  let dispose () =
    let kvs = subscriptions |> Seq.toArray
    for KeyValue (_, (head, tail)) in kvs do
      for subscription in Seq.append [|head|] tail do
        subscription ()

  let add key subscription =
    match subscriptions.TryGetValue(key) with
    | (true, (head, tail)) ->
      tail.Add(subscription)
    | (false, _) ->
      subscriptions.Add(key, (subscription, ResizeArray()))

  let tryRemove key =
    match subscriptions.TryGetValue(key) with
    | (true, (head, tail)) ->
      head ()
      if tail.Count = 0 then
        subscriptions.Remove(key)
      else
        let index = tail.Count - 1
        let head = tail.[index]
        tail.RemoveAt(index)
        subscriptions.[key] <- (head, tail)
        true
    | (false, _) ->
      false

  member this.Accumulation =
    accumulation :> IReadOnlyReactiveProperty<_>

  member this.Add(behavior: IReadOnlyReactiveProperty<'x>) =
    let removed = new AsyncSubject<unit>()
    behavior
      .StartWith(group.Unit)
      .TakeUntil(removed)
      .Concat(Observable.Return(group.Unit))
    |> Observable.pairwise
    |> Observable.subscribe
      (fun (oldValue, newValue) ->
        accumulation.Value <-
          group.Multiply(group.Divide(accumulation.Value, oldValue), newValue)
      )
    |> ignore
    add behavior (removed.OnNext >> removed.OnCompleted)
    Disposable.Create(fun () -> tryRemove behavior |> ignore)

  member this.Remove(obj) =
    tryRemove obj

  member this.Dispose() =
    dispose ()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module AccumulateBehavior =
  let create group =
    new AccumulateBehavior<'x>(group)
