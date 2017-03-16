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

  override this.Notify(_) =
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

  override this.Notify(notification) =
    subject.OnNext(notification)

  override this.Subscribe(observer) =
    subject.Subscribe(observer)

  override this.Dispose() =
    subject.Dispose()

[<Extension>]
type NotifierExtension() =
  [<Extension>]
  static member Warnings(this: Notifier) =
    let warnings =
      this |> Observable.filter (fun n -> n.Type = NotificationType.Warning)
    warnings.ToReadOnlyReactiveCollection()
    :> IReadOnlyList<_>

[<AutoOpen>]
module NotificationExtension =
  let (|Info|Warning|) (notification: Notification) =
    match notification.Type with
    | NotificationType.Info ->
      Info notification.Message
    | NotificationType.Warning ->
      Warning (notification.Message, notification.Data)
