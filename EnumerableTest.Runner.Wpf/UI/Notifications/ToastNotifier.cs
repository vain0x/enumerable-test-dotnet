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

namespace EnumerableTest.Runner.Wpf.UI.Notifications
{
    public sealed class AppToastNotification
        : ToastNotification
    {
        public override TimeSpan? Duration =>
            TimeSpan.FromSeconds(5.0);

        public override Duration FadeDuration =>
            new Duration(TimeSpan.FromSeconds(1.0));

        public Notification Notification { get; }

        public AppToastNotification(Notification notification)
        {
            Notification = notification;
        }
    }

    public sealed class NotificationTemplateSelector
        : DataTemplateSelector
    {
        string TempalteNameOrNull(NotificationType type)
        {
            if (type == NotificationType.Info)
            {
                return "InfoNotificationTemplate";
            }
            else if (type == NotificationType.Warning)
            {
                return "WarningNotificationTemplate";
            }

            return null;
        }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var element = container as FrameworkElement;
            var notification = item as Notification;
            if (element == null || notification == null) return null;

            var templateNameOrNull = TempalteNameOrNull(notification.Type);
            return
                templateNameOrNull == null
                    ? null
                    : element.FindResource(templateNameOrNull) as DataTemplate;
        }
    }

    sealed class ToastNotifier
        : IObserver<Notification>
    {
        public ToastNotificationCollection Collection { get; } =
            new ToastNotificationCollection();

        public Window Window { get; }

        public void OnNext(Notification notification)
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
