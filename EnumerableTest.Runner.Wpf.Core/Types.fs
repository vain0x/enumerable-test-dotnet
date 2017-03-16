namespace EnumerableTest.Runner.Wpf

type NotExecutedResult private () =
  static member val Instance =
    new NotExecutedResult()

namespace EnumerableTest.Runner.Wpf.UI.Notifications
  type ToastNotificationType =
    | Success = 0
    | Info = 1
    | Warning = 2
    | Error = 3

  type ToastNotification(typ: ToastNotificationType, message: string) =
    member this.Type = typ
    member this.Message = message

  type IToastNotifier =
    abstract Notify: ToastNotification -> unit
