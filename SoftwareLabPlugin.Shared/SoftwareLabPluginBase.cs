using MissionPlanner.SoftwareLab.Notifications;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public abstract class SoftwareLabPluginBase : MissionPlanner.Plugin.Plugin
    {
        private readonly Dictionary<string, float> localParamCache = new Dictionary<string, float>();
        private bool hasWarnedMissingNotificationsPlugin;

        public override string Version => "1.0";
        public override string Author => "Software";

        public override bool Init()
        {
            return true;
        }

        public override bool Loop()
        {
            return true;
        }

        protected bool IsConnected()
        {
            return Host?.comPort?.BaseStream != null && Host.comPort.BaseStream.IsOpen;
        }

        protected bool EnsureConnected()
        {
            if (IsConnected())
                return true;

            ShowAutoCloseMessage("Vehicle not connected");
            return false;
        }

        protected bool TryGetCachedParam(string name, out float value)
        {
            value = 0;

            if (!IsConnected())
                return false;

            if (localParamCache.TryGetValue(name, out value))
                return true;

            var paramTable = Host.comPort.MAV?.param;
            if (paramTable == null || !paramTable.ContainsKey(name) || !TryConvertToFloat(paramTable[name], out value))
                return false;

            localParamCache[name] = value;
            return true;
        }

        protected bool SetSingleParam(string name, float value, bool showSuccessMessage = true, bool showFailureMessage = true)
        {
            if (!EnsureConnected())
                return false;

            try
            {
                bool success = Host.comPort.setParam(name, value);

                if (!success)
                {
                    if (showFailureMessage)
                        ShowAutoCloseMessage(GetSetParamMessage(name, value, false), NotificationSeverity.Error);

                    return false;
                }

                localParamCache[name] = value;

                if (showSuccessMessage)
                    ShowAutoCloseMessage(GetSetParamMessage(name, value, true), NotificationSeverity.Success);

                return true;
            }
            catch (Exception ex)
            {
                if (showFailureMessage)
                    ShowAutoCloseMessage(GetSetParamExceptionMessage(name, ex), NotificationSeverity.Error);

                return false;
            }
        }

        protected bool LoadMenu(string menuText, Func<ToolStripMenuItem> createMenu, string actionDescription)
        {
            try
            {
                if (Host?.FDMenuHud == null)
                    return false;

                RemoveMenuByText(menuText);
                Host.FDMenuHud.Items.Add(createMenu());
                return true;
            }
            catch (Exception ex)
            {
                ShowPluginError(actionDescription, ex);
                return false;
            }
        }

        protected bool ExitMenu(string menuText)
        {
            RemoveMenuByText(menuText);
            return true;
        }

        protected void RemoveMenuByText(string menuText)
        {
            if (Host?.FDMenuHud == null)
                return;

            for (int i = Host.FDMenuHud.Items.Count - 1; i >= 0; i--)
            {
                if (Host.FDMenuHud.Items[i] is ToolStripMenuItem menuItem &&
                    string.Equals(menuItem.Text, menuText, StringComparison.Ordinal))
                {
                    Host.FDMenuHud.Items.RemoveAt(i);
                }
            }
        }

        protected static ToolStripMenuItem CreateMenuItem(string text, EventHandler onClick)
        {
            return new ToolStripMenuItem(text, null, onClick);
        }

        protected static IReadOnlyList<NotificationLine> CreateNotificationLines(
            IReadOnlyCollection<string> successMessages,
            IReadOnlyCollection<string> failureMessages)
        {
            List<NotificationLine> lines = new List<NotificationLine>(2);
            AddNotificationLine(lines, successMessages, NotificationSeverity.Success);
            AddNotificationLine(lines, failureMessages, NotificationSeverity.Error);
            return lines;
        }

        protected void ShowPluginError(string action, Exception ex)
        {
            MessageBox.Show(
                $"Failed to {action}.\n{ex.Message}",
                Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        protected void ShowAutoCloseMessage(string message)
        {
            ShowAutoCloseMessage(message, NotificationSeverity.Info);
        }

        protected void ShowAutoCloseMessage(string message, NotificationSeverity severity)
        {
            ShowNotification(
                new List<NotificationLine>
                {
                    new NotificationLine(message, severity)
                },
                NotificationDisplayOptions.Default);
        }

        protected void ShowAutoCloseMessage(IReadOnlyList<NotificationLine> lines)
        {
            ShowNotification(lines, NotificationDisplayOptions.Default);
        }

        protected void ShowNotification(IReadOnlyList<NotificationLine> lines, NotificationDisplayOptions options)
        {
            if (lines == null || lines.Count == 0)
                return;

            NotificationDisplayOptions displayOptions = GetEffectiveDisplayOptions(lines, options);
            INotificationService notificationService = NotificationServiceRegistry.Current;
            if (notificationService != null)
            {
                hasWarnedMissingNotificationsPlugin = false;
                notificationService.ShowMessage(lines, displayOptions);
                return;
            }

            WarnMissingNotificationsPluginOnce();
            MessageBox.Show(
                string.Join(Environment.NewLine, lines.Select(line => line.Text)),
                Name,
                MessageBoxButtons.OK,
                lines.Any(line => line.Severity == NotificationSeverity.Error) ? MessageBoxIcon.Error : MessageBoxIcon.Information);
        }

        private static NotificationDisplayOptions GetEffectiveDisplayOptions(
            IReadOnlyList<NotificationLine> lines,
            NotificationDisplayOptions options)
        {
            NotificationDisplayOptions displayOptions = options ?? NotificationDisplayOptions.Default;
            return lines.Any(line => line.Severity == NotificationSeverity.Error)
                ? NotificationDisplayOptions.CreateManualClose()
                : displayOptions;
        }

        private void WarnMissingNotificationsPluginOnce()
        {
            if (hasWarnedMissingNotificationsPlugin)
                return;

            hasWarnedMissingNotificationsPlugin = true;
            MessageBox.Show(
                "SoftwareLabNotificationsPlugin.dll must also be loaded in Mission Planner to show plugin notifications.",
                Name,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private static void AddNotificationLine(ICollection<NotificationLine> lines, IReadOnlyCollection<string> messages, NotificationSeverity severity)
        {
            if (messages == null || messages.Count == 0)
                return;

            lines.Add(new NotificationLine(string.Join(", ", messages), severity));
        }

        private static bool TryConvertToFloat(object rawValue, out float value)
        {
            try
            {
                if (float.TryParse(rawValue?.ToString(), out value))
                    return true;

                value = Convert.ToSingle(rawValue);
                return true;
            }
            catch
            {
                value = 0;
                return false;
            }
        }

        protected static string GetSetParamMessage(string name, float value, bool success)
        {
            return success ? $"Set {name} to {value}" : $"Failed to set {name} to {value}";
        }

        protected static string GetSetParamExceptionMessage(string name, Exception ex)
        {
            return $"Failed to set {name}: {ex.Message}";
        }
    }
}
