namespace EnumerableTest.Runner.Wpf

module Counter =
  open System.Threading

  let private counter = ref 0
  let generate () =
    Interlocked.Increment(counter)

module ReadOnlyList =
  open System.Collections.Generic
  open EnumerableTest.Runner

  type SymmetricDifference<'k, 'x, 'y> =
    {
      Left                      : IReadOnlyList<'x>
      Intersect                 : IReadOnlyList<'k * 'x * 'y>
      Right                     : IReadOnlyList<'y>
    }

  let symmetricDifferenceBy
      (xKey: 'x -> 'k)
      (yKey: 'y -> 'k)
      (xs: IReadOnlyList<'x>)
      (ys: IReadOnlyList<'y>)
    =
    let xMap = xs |> Seq.map (fun x -> (xKey x, x)) |> Map.ofSeq
    let (intersect, right) =
      ys |> Seq.paritionMap
        (fun y ->
          let k = yKey y
          xMap
          |> Map.tryFind k
          |> Option.map (fun x -> (k, x, y))
        )
    let intersectKeys =
      intersect |> Seq.map (fun (k, _, _) -> k) |> set
    let left =
      xs
      |> Seq.filter (fun x -> intersectKeys |> Set.contains (xKey x) |> not)
      |> Seq.toArray
    {
      Left                      = left :> IReadOnlyList<_>
      Intersect                 = intersect
      Right                     = right
    }

module AppDomain =
  open System
  open System.Threading
  open EnumerableTest.Runner

  type DisposableAppDomain(appDomain: AppDomain) =
    member this.Value = appDomain

    member this.Dispose() =
      AppDomain.Unload(appDomain)

    interface IDisposable with
      override this.Dispose() =
        this.Dispose()

  let create name =
    let appDomain = AppDomain.CreateDomain(name, null, AppDomain.CurrentDomain.SetupInformation)
    new DisposableAppDomain(appDomain)

  let run (f: unit -> 'x) (this: AppDomain) =
    let result = MarshalByRefObject.ofValue None
    this.DoCallBack
      (fun () ->
        result.Value <- f () |> Some
      )
    result.Value |> Option.get

  let runObservable (f: IObserver<'y> -> 'x) (this: AppDomain) =
    let gate = obj()
    let notifications = MarshalByRefObject.ofValue [||]
    let isCompleted = MarshalByRefObject.ofValue false
    let mutable subscribers = [||]
    let mutable index = 0
    let mutable timerOrNone = None
    let observer =
      { new IObserver<_> with
          override this.OnNext(x) =
            lock gate
              (fun () -> notifications.Value <- Array.append notifications.Value [| x |])
          override this.OnError(_) = ()
          override this.OnCompleted() =
            lock gate (fun () -> isCompleted.Value <- true)
      }
    let notify _ =
      lock gate
        (fun () ->
          while index < notifications.Value.Length do
            for observer in subscribers do
              (observer :> IObserver<_>).OnNext(notifications.Value.[index])
            index <- index + 1
          if isCompleted.Value && index = notifications.Value.Length then
            match timerOrNone with
            | Some timer ->
              (timer :> IDisposable).Dispose()
              timerOrNone <- None
              for observer in subscribers do
                (observer :> IObserver<_>).OnCompleted()
            | None -> ()
        )
    let result =
      this |> run (fun () -> f observer)
    let connectable =
      { new Observable.IConnectableObservable<'y> with
          override this.Subscribe(observer) =
            subscribers <- Array.append subscribers [| observer |]
            { new IDisposable with
                override this.Dispose() = ()
            }
          override this.Connect() =
            timerOrNone <-
              new Timer(notify, (), TimeSpan.Zero, TimeSpan.FromMilliseconds(17.0))
              |> Some
      }
    (result, connectable)

module SynchronizationContext =
  open System.Threading

  let immediate =
    { new SynchronizationContext() with
        override this.Post(f, state) =
          f.Invoke(state)
        override this.Send(f, state) =
          f.Invoke(state)
    }

  let current () =
    SynchronizationContext.Current |> Option.ofObj

  let capture () =
    match current () with
    | Some c -> c
    | None -> immediate

  let send f (this: SynchronizationContext) =
    this.Send((fun _ -> f ()), ())

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Command =
  open System.Windows.Input

  let never =
    let canExecuteChanged = Event<_, _>()
    { new ICommand with
        override this.CanExecute(_) = false
        override this.Execute(_) = ()
        [<CLIEvent>]
        override this.CanExecuteChanged = canExecuteChanged.Publish
    }

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module FileInfo =
  open System
  open System.IO
  open System.Reactive.Linq

  let subscribeChanged (threshold: TimeSpan) onChanged (file: FileInfo) =
    let watcher = new FileSystemWatcher(file.DirectoryName, file.Name)
    watcher.NotifyFilter <- NotifyFilters.LastWrite
    watcher.Changed
      .Select(ignore)
      .Throttle(threshold)
      .Add(onChanged)
    watcher.EnableRaisingEvents <- true
    watcher :> IDisposable

module Observable =
  open System
  open System.Collections
  open System.Collections.Generic
  open System.Reactive.Linq

  type NotificationCollection<'x>(source: IObservable<'x>) =
    let notifications = ResizeArray()
    let mutable result = (None: option<option<exn>>)
    let subscription =
      source.Subscribe
        ( notifications.Add
        , fun error ->
            result <- Some (Some error)
        , fun () ->
            result <- Some None
        )

    member this.Result = result

    member this.Count = notifications.Count

    interface IEnumerable<'x> with
      override this.GetEnumerator() =
        notifications.GetEnumerator() :> IEnumerator<_>

      override this.GetEnumerator() =
        notifications.GetEnumerator() :> IEnumerator

    interface IDisposable with
      override this.Dispose() =
        subscription.Dispose()

  let collectNotifications (this: IObservable<_>) =
    new NotificationCollection<_>(this)

module ReactiveProperty =
  open System.Reactive.Linq
  open Reactive.Bindings

  let create x =
    new ReactiveProperty<_>(initialValue = x)

  let map (f: 'x -> 'y) (this: IReadOnlyReactiveProperty<'x>) =
    this.Select(f).ToReactiveProperty()

module ReactiveCommand =
  open System
  open Reactive.Bindings

  let create (canExecute: IReadOnlyReactiveProperty<_>) =
    new ReactiveCommand<_>(canExecuteSource = canExecute)

  let ofFunc (f: _ -> unit) canExecute =
    let it = create canExecute
    it.Subscribe(f) |> ignore<IDisposable>
    it

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
  abstract member Count: int
  abstract member GetEnumerator: unit -> IEnumerator<'x>
  abstract member Subscribe: IObserver<UptodateCollectionNotification<'x>> -> IDisposable
  abstract member Dispose: unit -> unit

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

  abstract member Add: 'x -> unit
  abstract member Remove: 'x -> bool

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
