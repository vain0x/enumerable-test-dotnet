namespace EnumerableTest.Runner

module Observable =
  open System
  open System.Collections
  open System.Collections.Generic
  open System.Reactive.Disposables
  open System.Reactive.Linq
  open System.Reactive.Subjects
  open System.Threading

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

  let subscribeEnd f (observable: IObservable<_>) =
    let observer =
      { new IObserver<_> with
          override this.OnNext(value) = ()
          override this.OnError(error) =
            error |> Some |> f
          override this.OnCompleted() =
            f None
      }
    observable.Subscribe(observer)

  let waitTimeout (timeout: TimeSpan) observable =
    use event = new ManualResetEvent(initialState = false)
    use subscription =
      observable
      |> subscribeEnd (fun _ -> event.Set() |> ignore<bool>)
    event.WaitOne(timeout)

  let wait observable =
    observable
    |> waitTimeout Timeout.InfiniteTimeSpan
    |> ignore<bool>

  /// Creates a connectable observable
  /// which executes async tasks when connected and notifies each result.
  let ofParallel computations =
    let gate = obj()
    let subject = new Subject<_>()
    let computation =
      async {
        let! (_: array<unit>) =
          computations |> Seq.map
            (fun computation ->
              async {
                let! x = computation
                lock gate (fun () -> (subject :> IObserver<_>).OnNext(x))
              }
            )
          |> Async.Parallel
        (subject :> IObserver<_>).OnCompleted()
      }
    { new IConnectableObservable<_> with
        override this.Connect() =
          Async.Start(computation)
          Disposable.Empty
        override this.Subscribe(observer) =
          (subject :> IObservable<_>).Subscribe(observer)
    }

  let startParallel computations =
    let gate = obj()
    let subject = new Subject<_>()
    let computations = computations |> Seq.toArray
    let connect () =
      let mutable count = 0
      for computation in computations do
        async {
          let! x = computation
          lock gate (fun () -> (subject :> IObserver<_>).OnNext(x))
          if Interlocked.Increment(&count) = computations.Length then
            lock gate (fun () -> (subject :> IObserver<_>).OnCompleted())
        } |> Async.Start
    { new IConnectableObservable<_> with
        override this.Connect() =
          connect ()
          Disposable.Empty
        override this.Subscribe(observer) =
          (subject :> IObservable<_>).Subscribe(observer)
    }
