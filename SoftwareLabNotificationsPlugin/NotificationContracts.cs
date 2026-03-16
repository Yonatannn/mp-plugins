using System.Collections.Generic;

namespace MissionPlanner.SoftwareLab.Notifications
{
    public enum NotificationSeverity
    {
        Info,
        Success,
        Error
    }

    public sealed class NotificationLine
    {
        public NotificationLine(string text)
            : this(text, NotificationSeverity.Info)
        {
        }

        public NotificationLine(string text, NotificationSeverity severity)
        {
            Text = text ?? string.Empty;
            Severity = severity;
        }

        public string Text { get; }
        public NotificationSeverity Severity { get; }
    }

    public interface INotificationService
    {
        void ShowMessage(string message);
        void ShowMessage(string message, NotificationSeverity severity);
        void ShowMessage(IReadOnlyList<NotificationLine> lines);
    }

    public static class NotificationServiceRegistry
    {
        private static readonly object SyncRoot = new object();
        private static INotificationService current;

        public static INotificationService Current
        {
            get
            {
                lock (SyncRoot)
                {
                    return current;
                }
            }
        }

        internal static void Register(INotificationService service)
        {
            lock (SyncRoot)
            {
                current = service;
            }
        }

        internal static void Unregister(INotificationService service)
        {
            lock (SyncRoot)
            {
                if (ReferenceEquals(current, service))
                    current = null;
            }
        }
    }
}
