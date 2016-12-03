namespace EnumerableTest.Runner

open System
open System.Reactive.Disposables
open System.Reactive.Linq
open System.Reactive.Subjects
open Basis.Core

type IFuture<'x> =
  inherit IObservable<'x>
  inherit IDisposable

  abstract Value: 'x with get

type FutureSource<'x>() =
  let mutable value = None

  let subject = new ReplaySubject<'x>()

  member this.Value
    with get () =
      value |> Option.getOrElse
        (fun () -> subject.FirstAsync().Wait())
    and set x =
      value <- Some x
      subject.OnNext(x)
      subject.OnCompleted()
      subject.Dispose()

  member this.Subscribe(observer) =
    subject.Subscribe(observer)

  member this.Dispose() =
    subject.Dispose()

  interface IFuture<'x> with
    override this.Value =
      this.Value

  interface IObservable<'x> with
    override this.Subscribe(observer) =
      this.Subscribe(observer)

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()
