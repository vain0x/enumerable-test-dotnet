namespace EnumerableTest.Runner.Wpf

open System
open System.Collections
open System.Collections.Generic

type UptodateCollectionNotification<'x> =
  {
    IsAdded             : bool
    Value               : 'x
  }

module UptodateCollectionNotification =
  let ofAdded x =
    {
      IsAdded           = true
      Value             = x
    }

  let ofRemoved x =
    {
      IsAdded           = false
      Value             = x
    }

/// NOTE: Thread-unsafe.
[<AbstractClass>]
type ReadOnlyUptodateCollection<'x>() =
  abstract Count: int
  abstract GetEnumerator: unit -> IEnumerator<'x>
  abstract Subscribe: IObserver<UptodateCollectionNotification<'x>> -> IDisposable
  abstract Dispose: unit -> unit

  interface IReadOnlyCollection<'x> with
    override this.Count = this.Count
    override this.GetEnumerator() = this.GetEnumerator()
    override this.GetEnumerator() = this.GetEnumerator() :> IEnumerator

  interface IObservable<UptodateCollectionNotification<'x>> with
    member this.Subscribe(observer) =
      for x in this do
        observer.OnNext(UptodateCollectionNotification.ofAdded x)
      this.Subscribe(observer)

  interface IDisposable with
    override this.Dispose() = this.Dispose()

[<AbstractClass>]
type UptodateCollection<'x>() =
  inherit ReadOnlyUptodateCollection<'x>()

  abstract Add: 'x -> unit
  abstract Remove: 'x -> bool

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module UptodateCollection =
  open System.Collections.ObjectModel
  open System.Reactive.Subjects

  let create () =
    let subject = new Subject<_>()
    let collection = Collection()
    { new UptodateCollection<'x>() with
        override this.Count = collection.Count

        override this.GetEnumerator() = collection.GetEnumerator()

        override this.Subscribe(observer) = subject.Subscribe(observer)

        override this.Dispose() =
          subject.OnCompleted()

        override this.Add(x) =
          collection.Add(x)
          subject.OnNext(UptodateCollectionNotification.ofAdded x)

        override this.Remove(x) =
          if collection.Remove(x) then
            subject.OnNext(UptodateCollectionNotification.ofRemoved x)
            true
          else
            false
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ReadOnlyUptodateCollection =
  open System.Collections.ObjectModel
  open System.Collections.Specialized
  open System.Reactive.Linq
  open EnumerableTest.Runner
  open Reactive.Bindings
  open Reactive.Bindings.Extensions

  let ofUptodate (uptodate: IReadOnlyReactiveProperty<'x>) =
    let subscribe (observer: IObserver<UptodateCollectionNotification<_>>) =
      uptodate.Pairwise() |> Observable.subscribe
        (fun window ->
          observer.OnNext(UptodateCollectionNotification.ofRemoved window.OldItem)
          observer.OnNext(UptodateCollectionNotification.ofAdded window.NewItem)
        )
    { new ReadOnlyUptodateCollection<'x>() with
        override this.Count = 1
        override this.GetEnumerator() = (Seq.singleton uptodate.Value).GetEnumerator()
        override this.Subscribe(observer) = subscribe observer
        override this.Dispose() = ()
    }

  let ofObservableCollection (collection: ObservableCollection<'x>) =
    let subscribe (observer: IObserver<UptodateCollectionNotification<_>>) =
      collection.CollectionChanged |> Observable.subscribe
        (fun e ->
          if e.OldItems |> isNull |> not then
            for x in e.OldItems |> Seq.cast do
              observer.OnNext(UptodateCollectionNotification.ofRemoved x)
          if e.NewItems |> isNull |> not then
            for x in e.NewItems |> Seq.cast do
              observer.OnNext(UptodateCollectionNotification.ofAdded x)
        )
    { new ReadOnlyUptodateCollection<'x>() with
        override this.Count = collection.Count
        override this.GetEnumerator() = collection.GetEnumerator()
        override this.Subscribe(observer) = subscribe observer
        override this.Dispose() = ()
    }

  let map f (this: ReadOnlyUptodateCollection<_>) =
    let getEnumerator () =
      (this |> Seq.map f).GetEnumerator()
    let subscribe (observer: IObserver<UptodateCollectionNotification<_>>) =
      this |> Observable.subscribe
        (fun notification ->
          let y = f notification.Value
          if notification.IsAdded then
            observer.OnNext(UptodateCollectionNotification.ofAdded y)
          else
            observer.OnNext(UptodateCollectionNotification.ofRemoved y)
        )
    { new ReadOnlyUptodateCollection<_>() with
        override this.Count = this.Count
        override this.GetEnumerator() = getEnumerator ()
        override this.Subscribe(observer) = subscribe observer
        override this.Dispose() = ()
    }

  let flatten (this: ReadOnlyUptodateCollection<ReadOnlyUptodateCollection<_>>) =
    let count () =
      this |> Seq.sumBy (fun collection -> collection.Count)
    let getEnumerator () =
      (this |> Seq.collect id).GetEnumerator()
    let subscribe (observer: IObserver<UptodateCollectionNotification<_>>) =
      let subscriptions = Dictionary<obj, _>()
      let outerSubscription =
        this |> Observable.subscribe
          (fun notification ->
            let collection = notification.Value
            if notification.IsAdded then
              subscriptions.Add(collection, collection |> Observable.subscribe observer.OnNext)
            else
              match subscriptions.TryGetValue(collection) with
              | (true, subscription) ->
                subscription.Dispose()
              | (false, _) ->
                ()
          )
      { new IDisposable with
          override this.Dispose() =
            outerSubscription.Dispose()
            for KeyValue (_, d) in subscriptions do
              d.Dispose()
      }
    { new ReadOnlyUptodateCollection<_>() with
        override it.Count = count ()
        override it.GetEnumerator() = getEnumerator ()
        override it.Subscribe(observer) = subscribe observer
        override it.Dispose() = ()
    }

  let collect f this =
    this |> map f |> flatten

  let sumBy (groupSig: GroupSig<_>) (this: ReadOnlyUptodateCollection<_>) =
    let accumulation = new ReactiveProperty<_>(initialValue = groupSig.Unit)
    let subscription =
      this |> Observable.subscribe
        (fun n ->
          accumulation.Value <-
            if n.IsAdded
              then groupSig.Multiply(accumulation.Value, n.Value)
              else groupSig.Divide(accumulation.Value, n.Value)
        )
    accumulation
