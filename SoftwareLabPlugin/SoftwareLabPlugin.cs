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

        private string configFilePath;
        private ToolStripMenuItem gpsMenu;
        private ToolStripMenuItem frskyMenu;

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
                MessageBox.Show(
                    $"Failed to initialize plugin.\n{ex.Message}",
                    "SoftwareLab Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
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

                gpsMenu = new ToolStripMenuItem("GPS Control");
                var btnPrimary = new ToolStripMenuItem("Set Primary GPS (GPS 1)", null, (s, e) => SetSingleParam(GpsPrimaryParamName, 0));
                var btnSecondary = new ToolStripMenuItem("Set Secondary GPS (GPS 2)", null, (s, e) => SetSingleParam(GpsPrimaryParamName, 1));
                var btnAuto = new ToolStripMenuItem("Toggle Auto Switch");

                gpsMenu.DropDownOpening += (s, e) => UpdateGpsAutoText(btnAuto);
                btnAuto.Click += (s, e) => ToggleGpsAuto();

                gpsMenu.DropDownItems.Add(btnPrimary);
                gpsMenu.DropDownItems.Add(btnSecondary);
                gpsMenu.DropDownItems.Add(btnAuto);

                frskyMenu = new ToolStripMenuItem("FrSky Control");
                var btnGive = new ToolStripMenuItem("Give Control to FrSky", null, (s, e) => ApplyControlConfig(true));
                var btnTake = new ToolStripMenuItem("Take Control from FrSky", null, (s, e) => ApplyControlConfig(false));

                frskyMenu.DropDownItems.Add(btnGive);
                frskyMenu.DropDownItems.Add(btnTake);

                Host.FDMenuHud.Items.Add(gpsMenu);
                Host.FDMenuHud.Items.Add(frskyMenu);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load plugin menu.\n{ex.Message}",
                    "SoftwareLab Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        public override bool Loop()
        {
            return true;
        }

        private void RemoveExistingMenus()
        {
            RemoveMenuByText("GPS Control");
            RemoveMenuByText("FrSky Control");
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

        private bool IsConnected()
        {
            return Host?.comPort?.BaseStream != null && Host.comPort.BaseStream.IsOpen;
        }

        private bool TryGetCachedParam(string name, out float value)
        {
            value = 0;

            if (!IsConnected())
                return false;

            var paramTable = Host.comPort.MAV?.param;
            if (paramTable == null || !paramTable.ContainsKey(name))
                return false;

            try
            {
                value = Convert.ToSingle(paramTable[name]);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool SetSingleParam(string name, float value, bool showSuccessMessage = true)
        {
            if (!IsConnected())
            {
                ShowAutoCloseMessage("Vehicle not connected");
                return false;
            }

            try
            {
                bool success = Host.comPort.setParam(name, value);

                if (!success)
                {
                    MessageBox.Show(
                        $"Failed to set parameter {name} to {value}.",
                        "SoftwareLab Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                if (showSuccessMessage)
                    ShowAutoCloseMessage($"Set {name} to {value}");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to set parameter {name}.\n{ex.Message}",
                    "SoftwareLab Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return false;
            }
        }

        private void ToggleGpsAuto()
        {
            if (!IsConnected())
            {
                ShowAutoCloseMessage("Vehicle not connected");
                return;
            }

            try
            {
                float currentValue = Convert.ToSingle(Host.comPort.GetParam(GpsAutoSwitchParamName));
                float nextValue = Math.Abs(currentValue - 1f) < 0.001f ? 0f : 1f;
                SetSingleParam(GpsAutoSwitchParamName, nextValue);
            }
            catch
            {
                ShowAutoCloseMessage($"{GpsAutoSwitchParamName} not found");
            }
        }

        private void UpdateGpsAutoText(ToolStripMenuItem item)
        {
            if (item == null)
                return;

            if (TryGetCachedParam(GpsAutoSwitchParamName, out float value))
                item.Text = Math.Abs(value - 1f) < 0.001f ? "Disable Auto Switch (ON)" : "Enable Auto Switch (OFF)";
            else
                item.Text = "Toggle Auto Switch";
        }

        private void ApplyControlConfig(bool giveControl)
        {
            if (!IsConnected())
            {
                ShowAutoCloseMessage("Vehicle not connected");
                return;
            }

            try
            {
                PluginConfig config = LoadConfig();
                Dictionary<string, float> targetParams = giveControl ? config.GiveControl : config.TakeControl;

                if (targetParams == null || targetParams.Count == 0)
                {
                    ShowAutoCloseMessage("No parameters to apply");
                    return;
                }

                int successCount = 0;
                int failureCount = 0;

                foreach (var kvp in targetParams)
                {
                    if (SetSingleParam(kvp.Key, kvp.Value, false))
                        successCount++;
                    else
                        failureCount++;
                }

                if (failureCount == 0)
                {
                    ShowAutoCloseMessage(giveControl ? "Give Control applied" : "Take Control applied");
                    return;
                }

                MessageBox.Show(
                    $"Completed with partial success.\nSucceeded: {successCount}\nFailed: {failureCount}",
                    "SoftwareLab Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to apply config.\n{ex.Message}",
                    "SoftwareLab Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
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

            config.GiveControl ??= new Dictionary<string, float>();
            config.TakeControl ??= new Dictionary<string, float>();

            return config;
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

        private void ShowAutoCloseMessage(string message)
        {
            Form form = new Form
            {
                Text = "SoftwareLab Info",
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                BackColor = Color.LightGreen
            };

            Label label = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            form.Controls.Add(label);

            Timer timer = new Timer { Interval = 2500 };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                form.Close();
            };

            form.FormClosed += (s, e) =>
            {
                timer.Dispose();
                form.Dispose();
            };

            timer.Start();
            form.Show();
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