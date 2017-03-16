using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using DotNetKit.Windows.Controls;

namespace EnumerableTest.Runner.Wpf.UI.Notifications
{
    sealed class ToastNotifier
        : IToastNotifier
    {
        public ToastNotificationCollection Collection { get; } =
            new ToastNotificationCollection();

        public Window Window { get; }

        SimpleToastNotificationTheme Theme(ToastNotificationType type)
        {
            switch (type)
            {
                case ToastNotificationType.Success:
                    return SimpleToastNotificationTheme.Success;
                case ToastNotificationType.Info:
                    return SimpleToastNotificationTheme.Info;
                case ToastNotificationType.Warning:
                    return SimpleToastNotificationTheme.Warning;
                case ToastNotificationType.Error:
                default:
                    return SimpleToastNotificationTheme.Error;
            }
        }

        public void Notify(ToastNotification notification)
        {
            Window.Dispatcher.InvokeAsync(() =>
            {
                var tn =
                    new SimpleToastNotification(Theme(notification.Type))
                    {
                        Title = "EnumerableTest",
                        Message = notification.Message,
                    };
                Collection.Add(tn);
            });
        }

        public void Display(Window owner)
        {
            Window.Owner = owner;
            Window.Show();
        }

        public ToastNotifier(Window owner)
        {
            Window = new ToastNotificationWindow(Collection, owner: null);
        }
    }
}
