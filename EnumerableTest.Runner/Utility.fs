namespace EnumerableTest.Runner

open System.Collections.Generic

module Seq =
  let indexed xs =
    xs |> Seq.mapi (fun i x -> (i, x))

  /// Applies f for each element in xs and partition them into two list.
  /// The first is y's where f x = Some y
  /// and the other is x's where f x = None.
  let paritionMap (f: 'x -> option<'y>) (xs: seq<'x>): (IReadOnlyList<'y> * IReadOnlyList<'x>) =
    let firsts = ResizeArray()
    let seconds = ResizeArray()
    for x in xs do
      match f x with
      | Some y ->
        firsts.Add(y)
      | None ->
        seconds.Add(x)
    (firsts :> IReadOnlyList<_>, seconds :> IReadOnlyList<_>)

module Result =
  open Basis.Core

  let catch f =
    try
      f () |> Success
    with
    | e -> Failure e

  let toObj =
    function
    | Success x ->
      x :> obj
    | Failure x ->
      x :> obj

  let ofObj<'s, 'f> (o: obj) =
    match o with
    | :? 's as value ->
      value |> Success |> Some
    | :? 'f as value ->
      value |> Failure |> Some
    | _ ->
      None

module Observable =
  open System
  open System.Threading
  open DotNetKit.Observing

  type IConnectableObservable<'x> =
    inherit IObservable<'x>

    abstract member Connect: unit -> unit

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

  let wait observable =
    use event = new ManualResetEvent(initialState = false)
    observable
    |> subscribeEnd (fun _ -> event.Set() |> ignore<bool>)
    |> ignore<IDisposable>
    event.WaitOne() |> ignore<bool>

  /// Creates a connectable observable
  /// which executes async tasks when connected and notifies each result.
  let ofParallel asyncs =
    let subject = Subject.Create()
    let computation =
      async {
        let! (_: array<unit>) =
          asyncs |> Seq.map
            (fun a ->
              async {
                let! x = a
                (subject :> IObserver<_>).OnNext(x)
              }
            )
          |> Async.Parallel
        (subject :> IObserver<_>).OnCompleted()
      }
    { new IConnectableObservable<_> with
        override this.Connect() =
          Async.Start(computation)
        override this.Subscribe(observer) =
          (subject :> IObservable<_>).Subscribe(observer)
    }

module String =
  open Basis.Core

  let replace (source: string) (dest: string) (this: string) =
    this.Replace(source, dest)

  let convertToLF this =
    this |> replace "\r\n" "\n" |> replace "\r" "\n"

  let splitByLinebreak this =
    this |> convertToLF |> Str.splitBy "\n"

module Async =
  let result x =
    async {
      return x
    }

  let run f =
    async {
      return f ()
    }

  let map f a =
    async {
      let! x = a
      return f x
    }

module Disposable =
  open System

  let dispose (x: obj) =
    match x with
    | null -> ()
    | :? IDisposable as disposable ->
      disposable.Dispose()
    | _ -> ()

  let ofObj (x: obj) =
    { new IDisposable with
        override this.Dispose() =
          x |> dispose
    }
