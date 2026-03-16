using MissionPlanner;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public class SoftwareLabPlugin : MissionPlanner.Plugin.Plugin
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

        private Dictionary<string, float> localParamCache = new Dictionary<string, float>();
        private static readonly List<Form> activeNotifications = new List<Form>();
        private const int NotificationWidth = 360;
        private const int NotificationHeight = 180;
        private const int NotificationSpacing = 12;
        private static readonly Color SuccessNotificationColor = Color.DarkGreen;
        private static readonly Color FailureNotificationColor = Color.DarkRed;

        private sealed class NotificationLine
        {
            public NotificationLine(string text, Color color)
            {
                Text = text;
                Color = color;
            }

            public string Text { get; }
            public Color Color { get; }
        }

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
                if (Host.FDMenuHud.Items[i] is ToolStripMenuItem menuItem && string.Equals(menuItem.Text, menuText, StringComparison.Ordinal))
                    Host.FDMenuHud.Items.RemoveAt(i);
            }
        }

        private ToolStripMenuItem CreateGpsMenu()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(GpsMenuText);
            ToolStripMenuItem autoItem = CreateMenuItem("Toggle Auto Switch", (s, e) => ToggleGpsAuto((ToolStripMenuItem)s));

            menu.DropDownOpening += (s, e) => UpdateGpsAutoText(autoItem);
            menu.DropDownItems.Add(CreateMenuItem("Set Primary GPS (GPS 1)", (s, e) => SetSingleParam(GpsPrimaryParamName, 0)));
            menu.DropDownItems.Add(CreateMenuItem("Set Secondary GPS (GPS 2)", (s, e) => SetSingleParam(GpsPrimaryParamName, 1)));
            menu.DropDownItems.Add(autoItem);

            return menu;
        }

        private ToolStripMenuItem CreateFrSkyMenu()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(FrSkyMenuText);
            menu.DropDownItems.Add(CreateMenuItem("Give Control to FrSky", (s, e) => ApplyControlConfig(true)));
            menu.DropDownItems.Add(CreateMenuItem("Take Control from FrSky", (s, e) => ApplyControlConfig(false)));
            return menu;
        }

        private static ToolStripMenuItem CreateMenuItem(string text, EventHandler onClick)
        {
            return new ToolStripMenuItem(text, null, onClick);
        }

        private bool IsConnected()
        {
            return Host?.comPort?.BaseStream != null && Host.comPort.BaseStream.IsOpen;
        }

        private bool EnsureConnected()
        {
            if (IsConnected())
                return true;

            ShowAutoCloseMessage("Vehicle not connected");
            return false;
        }

        private bool TryGetCachedParam(string name, out float value)
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

        private bool SetSingleParam(string name, float value, bool showSuccessMessage = true, bool showFailureMessage = true)
        {
            if (!EnsureConnected())
                return false;

            try
            {
                bool success = Host.comPort.setParam(name, value);

                if (!success)
                {
                    if (showFailureMessage)
                        ShowAutoCloseMessage(GetSetParamMessage(name, value, false), FailureNotificationColor);

                    return false;
                }

                localParamCache[name] = value;

                if (showSuccessMessage)
                    ShowAutoCloseMessage(GetSetParamMessage(name, value, true), SuccessNotificationColor);

                return true;
            }
            catch (Exception ex)
            {
                if (showFailureMessage)
                    ShowAutoCloseMessage(GetSetParamExceptionMessage(name, ex), FailureNotificationColor);

                return false;
            }
        }

        private void ToggleGpsAuto(ToolStripMenuItem item)
        {
            if (!EnsureConnected())
                return;

            try
            {
                if (!TryGetCachedParam(GpsAutoSwitchParamName, out float currentValue))
                {
                    currentValue = Convert.ToSingle(Host.comPort.GetParam(GpsAutoSwitchParamName));
                    localParamCache[GpsAutoSwitchParamName] = currentValue;
                }

                float nextValue = Math.Abs(currentValue - 1f) < 0.001f ? 0f : 1f;

                if (SetSingleParam(GpsAutoSwitchParamName, nextValue, true))
                    UpdateGpsAutoText(item);
            }
            catch (Exception ex)
            {
                ShowAutoCloseMessage($"Failed to toggle switch: {ex.Message}");
            }
        }

        private void UpdateGpsAutoText(ToolStripMenuItem item)
        {
            if (item == null)
                return;

            if (TryGetCachedParam(GpsAutoSwitchParamName, out float value))
                item.Text = Math.Abs(value - 1f) < 0.001f ? "Switch Auto Switch to OFF" : "Switch Auto Switch to ON";
            else
                item.Text = "Toggle Auto Switch";
        }

        private void ApplyControlConfig(bool giveControl)
        {
            if (!EnsureConnected())
                return;

            try
            {
                PluginConfig config = LoadConfig();
                Dictionary<string, float> targetParams = giveControl ? config.GiveControl : config.TakeControl;

                if (targetParams == null || targetParams.Count == 0)
                {
                    ShowAutoCloseMessage("No parameters to apply");
                    return;
                }

                ShowAutoCloseMessage(BuildParamUpdateLines(targetParams));
            }
            catch (Exception ex)
            {
                ShowAutoCloseMessage($"Failed to apply config: {ex.Message}");
            }
        }

        private IReadOnlyList<NotificationLine> BuildParamUpdateLines(Dictionary<string, float> targetParams)
        {
            List<string> successMessages = new List<string>();
            List<string> failureMessages = new List<string>();

            foreach (var kvp in targetParams)
            {
                bool success = SetSingleParam(kvp.Key, kvp.Value, false, false);
                List<string> targetMessages = success ? successMessages : failureMessages;
                targetMessages.Add(GetSetParamMessage(kvp.Key, kvp.Value, success));
            }

            return CreateNotificationLines(successMessages, failureMessages);
        }

        private IReadOnlyList<NotificationLine> CreateNotificationLines(IReadOnlyCollection<string> successMessages, IReadOnlyCollection<string> failureMessages)
        {
            List<NotificationLine> lines = new List<NotificationLine>(2);
            AddNotificationLine(lines, successMessages, SuccessNotificationColor);
            AddNotificationLine(lines, failureMessages, FailureNotificationColor);
            return lines;
        }

        private static void AddNotificationLine(ICollection<NotificationLine> lines, IReadOnlyCollection<string> messages, Color color)
        {
            if (messages.Count == 0)
                return;

            lines.Add(new NotificationLine(string.Join(", ", messages), color));
        }

        private static string GetSetParamMessage(string name, float value, bool success)
        {
            return success ? $"Set {name} to {value}" : $"Failed to set {name} to {value}";
        }

        private static string GetSetParamExceptionMessage(string name, Exception ex)
        {
            return $"Failed to set {name}: {ex.Message}";
        }

        private PluginConfig LoadConfig()
        {
            if (string.IsNullOrWhiteSpace(configFilePath))
                throw new InvalidOperationException("Config file path is not initialized.");

            if (!File.Exists(configFilePath))
                CreateDefaultConfig();

            string json = File.ReadAllText(configFilePath);
            var config = JsonConvert.DeserializeObject<PluginConfig>(json);

            if (config == null)
                throw new InvalidOperationException("Config file is empty or invalid.");

            return EnsureConfigCollections(config);
        }

        private void CreateDefaultConfig()
        {
            if (string.IsNullOrWhiteSpace(configFilePath) || File.Exists(configFilePath))
                return;

            var defaultConfig = new PluginConfig
            {
                GiveControl = new Dictionary<string, float> { { "FS_THR_ENABLE", 0 } },
                TakeControl = new Dictionary<string, float> { { "FS_THR_ENABLE", 1 } }
            };

            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
        }

        private static PluginConfig EnsureConfigCollections(PluginConfig config)
        {
            if (config.GiveControl == null)
                config.GiveControl = new Dictionary<string, float>();

            if (config.TakeControl == null)
                config.TakeControl = new Dictionary<string, float>();

            return config;
        }

        private void ShowAutoCloseMessage(string message)
        {
            ShowAutoCloseMessage(message, SystemColors.ControlText);
        }

        private void ShowAutoCloseMessage(string message, Color textColor)
        {
            ShowAutoCloseMessage(new List<NotificationLine>
            {
                new NotificationLine(message, textColor)
            });
        }

        private void ShowAutoCloseMessage(IReadOnlyList<NotificationLine> lines)
        {
            if (lines == null || lines.Count == 0)
                return;

            Form form = new Form
            {
                Text = "SoftwareLab Info",
                Size = new Size(NotificationWidth, NotificationHeight),
                StartPosition = FormStartPosition.Manual,
                TopMost = true,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                ShowInTaskbar = false
            };

            TableLayoutPanel layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = lines.Count,
                Padding = new Padding(6)
            };

            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            for (int i = 0; i < lines.Count; i++)
            {
                NotificationLine line = lines[i];
                layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F / lines.Count));
                layout.Controls.Add(new Label
                {
                    Text = line.Text,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Arial", 12, FontStyle.Bold),
                    ForeColor = line.Color,
                    Margin = new Padding(0)
                }, 0, i);
            }

            form.Controls.Add(layout);

            Rectangle workingArea = Screen.PrimaryScreen.WorkingArea;

            lock (activeNotifications)
            {
                int index = activeNotifications.Count;
                form.Location = GetNotificationLocation(workingArea, index);
                activeNotifications.Add(form);
            }

            Timer timer = new Timer { Interval = 2500 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                form.Close();
            };

            form.FormClosed += (s, e) =>
            {
                timer.Dispose();

                lock (activeNotifications)
                {
                    activeNotifications.Remove(form);
                    RepositionActiveNotifications(Screen.PrimaryScreen.WorkingArea);
                }

                form.Dispose();
            };

            timer.Start();
            form.Show();
        }

        private Point GetNotificationLocation(Rectangle workingArea, int index)
        {
            int x = workingArea.Left + ((workingArea.Width - NotificationWidth) / 2);
            int y = workingArea.Top + ((workingArea.Height - NotificationHeight) / 2) + (index * NotificationSpacing);
            return new Point(x, y);
        }

        private void RepositionActiveNotifications(Rectangle workingArea)
        {
            for (int i = 0; i < activeNotifications.Count; i++)
                activeNotifications[i].Location = GetNotificationLocation(workingArea, i);
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

    public class PluginConfig
    {
        public Dictionary<string, float> GiveControl { get; set; }
        public Dictionary<string, float> TakeControl { get; set; }
    }
}
