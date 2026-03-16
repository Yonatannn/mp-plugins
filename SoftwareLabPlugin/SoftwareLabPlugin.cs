using MissionPlanner.SoftwareLab.Notifications;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public partial class SoftwareLabPlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name => "SoftwareLab Controls Plugin";
        public override string Version => "1.0";
        public override string Author => "Software";

        private const string GpsPrimaryParamName = "GPS_PRIMARY";
        private const string GpsAutoSwitchParamName = "GPS_AUTO_SWITCH";
        private const string ConfigFileName = "PluginConfig.json";
        private const string GpsMenuText = "GPS Control";
        private const string FrSkyMenuText = "FrSky Control";

        private string configFilePath;
        private ToolStripMenuItem gpsMenu;
        private ToolStripMenuItem frskyMenu;
        private bool hasWarnedMissingNotificationsPlugin;

        private readonly Dictionary<string, float> localParamCache = new Dictionary<string, float>();

        public override bool Init()
        {
            try
            {
                string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrWhiteSpace(pluginFolder))
                    return false;

                configFilePath = Path.Combine(pluginFolder, ConfigFileName);
                CreateDefaultConfig();
                return true;
            }
            catch (Exception ex)
            {
                ShowPluginError("initialize plugin", ex);
                return false;
            }
        }

        public override bool Loaded()
        {
            try
            {
                if (Host?.FDMenuHud == null)
                    return false;

                RemoveExistingMenus();
                gpsMenu = CreateGpsMenu();
                frskyMenu = CreateFrSkyMenu();

                Host.FDMenuHud.Items.Add(gpsMenu);
                Host.FDMenuHud.Items.Add(frskyMenu);
                return true;
            }
            catch (Exception ex)
            {
                ShowPluginError("load plugin menu", ex);
                return false;
            }
        }

        public override bool Loop()
        {
            return true;
        }

        private void RemoveExistingMenus()
        {
            RemoveMenuByText(GpsMenuText);
            RemoveMenuByText(FrSkyMenuText);
        }

        private void RemoveMenuByText(string menuText)
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

        private ToolStripMenuItem CreateGpsMenu()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(GpsMenuText);
            ToolStripMenuItem autoItem = CreateMenuItem("Toggle Auto Switch", (s, e) => ToggleGpsAuto((ToolStripMenuItem)s));

            menu.DropDownOpening += (s, e) => UpdateGpsAutoText(autoItem);
            menu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("Set Primary GPS (GPS 1)", (s, e) => SetSingleParam(GpsPrimaryParamName, 0)),
                CreateMenuItem("Set Secondary GPS (GPS 2)", (s, e) => SetSingleParam(GpsPrimaryParamName, 1)),
                autoItem
            });

            return menu;
        }

        private ToolStripMenuItem CreateFrSkyMenu()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(FrSkyMenuText);
            menu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("Give Control to FrSky", (s, e) => ApplyControlConfig(true)),
                CreateMenuItem("Take Control from FrSky", (s, e) => ApplyControlConfig(false))
            });

            return menu;
        }

        private static ToolStripMenuItem CreateMenuItem(string text, EventHandler onClick)
        {
            return new ToolStripMenuItem(text, null, onClick);
        }

        private static void ShowPluginError(string action, Exception ex)
        {
            MessageBox.Show(
                $"Failed to {action}.\n{ex.Message}",
                "SoftwareLab Controls Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void ShowAutoCloseMessage(string message)
        {
            ShowAutoCloseMessage(message, NotificationSeverity.Info);
        }

        private void ShowAutoCloseMessage(string message, NotificationSeverity severity)
        {
            ShowAutoCloseMessage(new List<NotificationLine>
            {
                new NotificationLine(message, severity)
            });
        }

        private void ShowAutoCloseMessage(IReadOnlyList<NotificationLine> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            INotificationService notificationService = NotificationServiceRegistry.Current;
            if (notificationService != null)
            {
                hasWarnedMissingNotificationsPlugin = false;
                notificationService.ShowMessage(lines);
                return;
            }

            WarnMissingNotificationsPluginOnce();
            MessageBox.Show(
                string.Join(Environment.NewLine, lines.Select(line => line.Text)),
                "SoftwareLab Info",
                MessageBoxButtons.OK,
                lines.Any(line => line.Severity == NotificationSeverity.Error) ? MessageBoxIcon.Error : MessageBoxIcon.Information);
        }

        private void WarnMissingNotificationsPluginOnce()
        {
            if (hasWarnedMissingNotificationsPlugin)
                return;

            hasWarnedMissingNotificationsPlugin = true;
            MessageBox.Show(
                "SoftwareLabNotificationsPlugin.dll must also be loaded in Mission Planner to show plugin notifications.",
                "SoftwareLab Controls Plugin",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        public override bool Exit()
        {
            RemoveExistingMenus();
            return true;
        }
    }
}
