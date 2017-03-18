namespace EnumerableTest.Runner.UI.Notifications

open System
open System.Collections.Generic
open System.Collections.ObjectModel
open System.Reactive.Disposables
open System.Reactive.Subjects
open System.Runtime.CompilerServices
open Reactive.Bindings

[<RequireQualifiedAccess>]
type AsyncNotificationType =
  | Successful
  | Info
  | Warning

type IAsyncNotification =
  abstract Type: AsyncNotificationType

type AsyncNotification<'s, 'i, 'w> =
  | SuccessfulNotification
    of 's
  | InfoNotification
    of 'i
  | WarningNotification
    of 'w
with
  member this.Match(onSuccess, onInfo, onWarning) =
    match this with
    | SuccessfulNotification n ->
      onSuccess n
    | InfoNotification n ->
      onInfo n
    | WarningNotification n ->
      onWarning n

  member this.Type =
    match this with
    | SuccessfulNotification _ ->
      AsyncNotificationType.Successful
    | InfoNotification _ ->
      AsyncNotificationType.Info
    | WarningNotification _ ->
      AsyncNotificationType.Warning

  override this.ToString() =
    match this with
    | SuccessfulNotification n ->
      n |> box |> string
    | InfoNotification n ->
      n |> box |> string
    | WarningNotification n ->
      n |> box |> string

  interface IAsyncNotification with
    override this.Type = this.Type

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
type AsyncNotification =
  static member FromSuccess(n) =
    SuccessfulNotification n

  static member FromInfo(n) =
    InfoNotification n

  static member FromWarning(n) =
    WarningNotification n

[<AutoOpen>]
module AsyncNotificationExtension =
  let (|Successful|Info|Warning|) (notification: AsyncNotification<_, _, _>) =
    match notification with
    | SuccessfulNotification n ->
      Successful n
    | InfoNotification n ->
      Info n
    | WarningNotification n ->
      Warning n

type INotifier<'s, 'i, 'w> =
  inherit IDisposable
  inherit IObservable<AsyncNotification<'s, 'i, 'w>>
  inherit IObserver<AsyncNotification<'s, 'i, 'w>>

type Notifier<'s, 'i, 'w>(subject: Subject<_>) =
  abstract Dispose: unit -> unit

  override this.Dispose() =
    subject.Dispose()

  interface IDisposable with
    override this.Dispose() =
      this.Dispose()

  interface IObservable<AsyncNotification<'s, 'i, 'w>> with
    override this.Subscribe(observer) =
      subject.Subscribe(observer)

  interface IObserver<AsyncNotification<'s, 'i, 'w>> with
    override this.OnNext(value) =
      subject.OnNext(value)

    override this.OnError(error) =
      subject.OnError(error)

    override this.OnCompleted() =
      subject.OnCompleted()

  interface INotifier<'s, 'i, 'w>

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Notifier =
  let empty<'s, 'i, 'w> =
    { new INotifier<'s, 'i, 'w> with
        override this.Dispose() = ()

        override this.Subscribe(observer) = Disposable.Empty

        override this.OnNext(value) = ()
        override this.OnError(error) = ()
        override this.OnCompleted() = ()
    }

  let notify notification (notifier: INotifier<_, _, _>) =
    notifier.OnNext(notification)

  let observeWarnings (notifier: INotifier<_, _, _>) =
    notifier |> Observable.choose
      (function
        | Successful _
        | Info _ ->
          None
        | Warning warning ->
          Some warning
      )

  let collectWarnings notifier =
    (notifier |> observeWarnings).ToReactiveCollection()

[<Extension>]
type NotifierExtension =
  [<Extension>]
  static member NotifySuccess(notifier, notification) =
    notifier |> Notifier.notify (AsyncNotification.FromSuccess(notification))

  [<Extension>]
  static member NotifyInfo(notifier, notification) =
    notifier |> Notifier.notify (AsyncNotification.FromInfo(notification))

  [<Extension>]
  static member NotifyWarning(notifier, notification) =
    notifier |> Notifier.notify (AsyncNotification.FromWarning(notification))
