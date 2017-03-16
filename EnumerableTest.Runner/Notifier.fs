namespace EnumerableTest.Runner

open System.Collections.Generic
open System.Collections.ObjectModel
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Runtime.CompilerServices
open Reactive.Bindings

[<Sealed>]
type NullNotifier() =
  inherit Notifier()

  override this.NotifyInfo(_) =
    ()

  override this.NotifyWarning(_, _) =
    ()

  override this.Subscribe(_) =
    Disposable.Empty

  override this.Dispose() =
    ()

[<Sealed>]
type ConcreteNotifier() =
  inherit Notifier()

  let subject =
    new Subject<_>()

  override this.NotifyInfo(message) =
    subject.OnNext(Info message)

  override this.NotifyWarning(message, data) =
    let warning =
      {
        Message =
          message
        Data =
          data |> Seq.map KeyValuePair
      }
    subject.OnNext(Warning warning)

  override this.Subscribe(observer) =
    subject.Subscribe(observer)

  override this.Dispose() =
    subject.Dispose()

[<Extension>]
type NotifierExtension() =
  [<Extension>]
  static member Warnings(this: Notifier) =
    let warnings = this |> Observable.choose (function | Warning w -> Some w | _ -> None)
    warnings.ToReadOnlyReactiveCollection()
    :> IReadOnlyList<_>
