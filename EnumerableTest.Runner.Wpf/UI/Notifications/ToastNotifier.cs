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
    public abstract class AppToastNotification
        : DotNetKit.Windows.Controls.ToastNotification
    {
        public override TimeSpan? Duration =>
            TimeSpan.FromSeconds(5.0);

        public override Duration FadeDuration =>
            new Duration(TimeSpan.FromSeconds(1.0));

        public string Message { get; }

        public AppToastNotification(string message)
        {
            Message = message;
        }
    }

    public sealed class InfoToastNotification
        : AppToastNotification
    {
        public InfoToastNotification(string message)
            : base(message)
        {
        }
    }

    public sealed class WarningToastNotification
        : AppToastNotification
    {
        public WarningToastNotification(string message)
            : base(message)
        {
        }
    }

    sealed class ToastNotifier
        : IToastNotifier
    {
        public ToastNotificationCollection Collection { get; } =
            new ToastNotificationCollection();

        public Window Window { get; }

        AppToastNotification ToastNotification(ToastNotification notification)
        {
            switch (notification.Type)
            {
                case ToastNotificationType.Info:
                    return new InfoToastNotification(notification.Message);
                case ToastNotificationType.Warning:
                    return new WarningToastNotification(notification.Message);
                default:
                    throw new Exception();
            }
        }

        public void Notify(ToastNotification notification)
        {
            Collection.Add(ToastNotification(notification));
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
