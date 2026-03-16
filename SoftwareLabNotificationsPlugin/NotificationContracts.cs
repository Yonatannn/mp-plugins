using System;
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

    public enum NotificationCloseBehavior
    {
        AutoClose,
        ManualClose
    }

    public sealed class NotificationDisplayOptions
    {
        public const int DefaultAutoCloseDurationMs = 2500;

        public static NotificationDisplayOptions Default { get; } = CreateAutoClose();

        public NotificationDisplayOptions(NotificationCloseBehavior closeBehavior, int autoCloseDurationMs = DefaultAutoCloseDurationMs)
        {
            if (closeBehavior == NotificationCloseBehavior.AutoClose && autoCloseDurationMs <= 0)
                throw new ArgumentOutOfRangeException(nameof(autoCloseDurationMs), "Auto-close duration must be greater than zero.");

            CloseBehavior = closeBehavior;
            AutoCloseDurationMs = autoCloseDurationMs;
        }

        public NotificationCloseBehavior CloseBehavior { get; }
        public int AutoCloseDurationMs { get; }

        public bool RequiresManualClose => CloseBehavior == NotificationCloseBehavior.ManualClose;

        public static NotificationDisplayOptions CreateAutoClose(int autoCloseDurationMs = DefaultAutoCloseDurationMs)
        {
            return new NotificationDisplayOptions(NotificationCloseBehavior.AutoClose, autoCloseDurationMs);
        }

        public static NotificationDisplayOptions CreateManualClose()
        {
            return new NotificationDisplayOptions(NotificationCloseBehavior.ManualClose);
        }
    }

    public interface INotificationService
    {
        void ShowMessage(string message);
        void ShowMessage(string message, NotificationSeverity severity);
        void ShowMessage(IReadOnlyList<NotificationLine> lines);
    }

    public interface IConfigurableNotificationService : INotificationService
    {
        void ShowMessage(string message, NotificationDisplayOptions options);
        void ShowMessage(string message, NotificationSeverity severity, NotificationDisplayOptions options);
        void ShowMessage(IReadOnlyList<NotificationLine> lines, NotificationDisplayOptions options);
    }

    public static class NotificationServiceExtensions
    {
        public static void ShowMessage(this INotificationService service, string message, NotificationDisplayOptions options)
        {
            if (service == null)
                return;

            if (service is IConfigurableNotificationService configurableService)
            {
                configurableService.ShowMessage(message, options ?? NotificationDisplayOptions.Default);
                return;
            }

            service.ShowMessage(message);
        }

        public static void ShowMessage(this INotificationService service, string message, NotificationSeverity severity, NotificationDisplayOptions options)
        {
            if (service == null)
                return;

            if (service is IConfigurableNotificationService configurableService)
            {
                configurableService.ShowMessage(message, severity, options ?? NotificationDisplayOptions.Default);
                return;
            }

            service.ShowMessage(message, severity);
        }

        public static void ShowMessage(this INotificationService service, IReadOnlyList<NotificationLine> lines, NotificationDisplayOptions options)
        {
            if (service == null)
                return;

            if (service is IConfigurableNotificationService configurableService)
            {
                configurableService.ShowMessage(lines, options ?? NotificationDisplayOptions.Default);
                return;
            }

            service.ShowMessage(lines);
        }
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
