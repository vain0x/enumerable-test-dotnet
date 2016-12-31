namespace EnumerableTest.Runner

open System
open System.Collections.Concurrent
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open System.Threading
open System.Threading.Tasks

[<Sealed>]
type ConcurrentActionQueue() =
  let queue = ConcurrentQueue()

  let gotEmpty = new Subject<unit>()

  let isRunning = ref 0

  let rec consume () =
    async {
      if Interlocked.Exchange(isRunning, 1) = 0 then
        try
          let rec loop () =
            async {
              match queue.TryDequeue() with
              | (true, action) ->
                do! action
                return! loop ()
              | (false, _) ->
                ()
            }
          do! loop ()
        finally
          Interlocked.Exchange(isRunning, 0) |> ignore
        if queue.IsEmpty then
          gotEmpty.OnNext(())
        else
          return! consume ()
    }

  member this.Enqueue(action): unit =
    queue.Enqueue(action)
    consume () |> Async.Start

  member val GotEmpty =
    Observable.Create
      ( fun (observer: IObserver<_>) ->
          if queue.IsEmpty then
            observer.OnNext(())
            Disposable.Empty
          else
            gotEmpty.Subscribe(observer)
      )
