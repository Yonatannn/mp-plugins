using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public partial class SoftwareLabPlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name => "SoftwareLab Plugin";
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

        private readonly Dictionary<string, float> localParamCache = new Dictionary<string, float>();
        private static readonly List<Form> activeNotifications = new List<Form>();
        private const int NotificationWidth = 360;
        private const int NotificationHeight = 180;
        private const int NotificationSpacing = 12;
        private static readonly Color SuccessNotificationColor = Color.Green;
        private static readonly Color FailureNotificationColor = Color.Red;

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
                "SoftwareLab Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        public override bool Exit()
        {
            RemoveExistingMenus();
            return true;
        }
    }
}
