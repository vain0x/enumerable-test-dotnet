namespace EnumerableTest.Runner

module Seq =
  let indexed xs =
    xs |> Seq.mapi (fun i x -> (i, x))

module Result =
  open Basis.Core

  let catch f =
    try
      f () |> Success
    with
    | e -> Failure e

module Observable =
  open System
  open System.Threading

  type IConnectableObservable<'x> =
    inherit IObservable<'x>

    abstract member Connect: unit -> unit

  /// Naive implementation. DON'T REUSE THIS.
  type Subject<'x>() =
    let gate = obj()
    let observers = ref [||]
    let result = ref None

    interface IObservable<'x> with
      override this.Subscribe(observer) =
        lock (gate)
          (fun () ->
            match !result with
            | None ->
              observers := [| yield! !observers; yield observer |]
            | Some (Some error) ->
              observer.OnError(error)
            | Some None ->
              observer.OnCompleted()
            { new IDisposable with
                override this.Dispose() = ()
            }
          )

    interface IObserver<'x> with
      override this.OnNext(value) =
        lock gate
          (fun () ->
            for observer in !observers do
              observer.OnNext(value)
          )

      override this.OnError(error) =
        lock gate
          (fun () ->
            result := error |> Some |> Some
            for observer in !observers do
              observer.OnError(error)
          )

      override this.OnCompleted() =
        lock gate
          (fun () ->
            result := Some None
            for observer in !observers do
              observer.OnCompleted()
          )

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
