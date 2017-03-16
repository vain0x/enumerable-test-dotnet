namespace EnumerableTest.Runner.Wpf.UI.Notifications

open System
open EnumerableTest.Runner
open EnumerableTest.Runner.Wpf

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module ToastNotifier =
  let subscribeNotifier (notifier: Notifier) (toastNotifier: IToastNotifier) =
    notifier |> Observable.subscribe
      (function
        | Info message ->
          toastNotifier.Notify(ToastNotification(ToastNotificationType.Info, message))
        | Warning warning ->
          let message = warning.Message
          toastNotifier.Notify(ToastNotification(ToastNotificationType.Warning, message))
      )
