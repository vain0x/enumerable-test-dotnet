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

  let add obj subscription =
    match subscriptions.TryGetValue(obj) with
    | (true, (head, tail)) ->
      tail.Add(subscription)
    | (false, _) ->
      subscriptions.Add(obj, (subscription, ResizeArray()))

  let tryRemove obj =
    match subscriptions.TryGetValue(obj) with
    | (true, (head, tail)) ->
      head ()
      if tail.Count = 0 then
        subscriptions.Remove(obj)
      else
        let index = tail.Count - 1
        let head = tail.[index]
        tail.RemoveAt(index)
        subscriptions.[obj] <- (head, tail)
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

  member this.Add(collection: ObservableCollection<'x>) =
    let removed = new AsyncSubject<unit>()
    let list = ResizeArray()
    let onChanged (e: NotifyCollectionChangedEventArgs) =
      let add value =
        list.Add(value)
        accumulation.Value <-
          group.Multiply(accumulation.Value, value)
      let remove value =
        if list.Remove(value) then
          accumulation.Value <-
            group.Divide(accumulation.Value, value)
      let clear () =
        accumulation.Value <-
          list |> Seq.fold (fun a value -> group.Divide(a, value)) accumulation.Value
        list.Clear()
      match e.Action with
      | NotifyCollectionChangedAction.Move -> ()
      | NotifyCollectionChangedAction.Reset ->
        clear ()
      | _ ->
        let toArray (list: IList) =
          if list |> isNull
          then Array.empty
          else list |> Seq.cast |> Seq.toArray
        e.OldItems |> toArray |> Seq.iter remove
        e.NewItems |> toArray |> Seq.iter add
    let initialChange =
      NotifyCollectionChangedEventArgs
        ( NotifyCollectionChangedAction.Add
        , collection |> Seq.toArray
        , 0
        )
    let finalChange =
      NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)
    collection
      .CollectionChanged
      .StartWith(initialChange)
      .TakeUntil(removed)
      .Concat(Observable.Return(finalChange))
    |> Observable.subscribe onChanged
    |> ignore
    add collection (removed.OnNext >> removed.OnCompleted)

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
