using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Threading.Tasks;
using System.Windows;
using DotNetKit.Windows.Controls;
using EnumerableTest.Runner.UI.Notifications;

namespace EnumerableTest.Runner.Wpf.UI.Notifications
{
    public sealed class AppToastNotification
        : ToastNotification
    {
        public override TimeSpan? Duration =>
            TimeSpan.FromSeconds(5.0);

        public override Duration FadeDuration =>
            new Duration(TimeSpan.FromSeconds(1.0));

        public IAsyncNotification Notification { get; }

        public string Message => Notification.ToString();

        public AppToastNotification(IAsyncNotification notification)
        {
            Notification = notification;
        }
    }

    public sealed class NotificationTemplateSelector
        : DataTemplateSelector
    {
        string TempalteNameOrNull(AsyncNotificationType type)
        {
            if (type == AsyncNotificationType.Successful)
            {
                return "SuccessfulNotificationTemplate";
            }
            else if (type == AsyncNotificationType.Info)
            {
                return "InfoNotificationTemplate";
            }
            else if (type == AsyncNotificationType.Warning)
            {
                return "WarningNotificationTemplate";
            }

            return null;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            var notification = item as AppToastNotification;
            if (element == null || notification == null) return null;

            var templateNameOrNull = TempalteNameOrNull(notification.Notification.Type);
            return
                templateNameOrNull == null
                    ? null
                    : element.FindResource(templateNameOrNull) as DataTemplate;
        }
    }

    sealed class ToastNotifier
        : IObserver<IAsyncNotification>
    {
        public ToastNotificationCollection Collection { get; } =
            new ToastNotificationCollection();

        public Window Window { get; }

        public void OnNext(IAsyncNotification notification)
        {
            Collection.Add(new AppToastNotification(notification));
        }

        public void OnError(Exception _)
        {
        }

        public void OnCompleted()
        {
        }

        public void Display()
        {
            Window.Show();
        }

        public ToastNotifier(Window owner)
        {
            Window = new ToastNotificationWindow(Collection, owner);
        }
    }
}
