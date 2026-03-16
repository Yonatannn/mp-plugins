using MissionPlanner;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public class SoftwareLabPlugin : MissionPlanner.Plugin.Plugin
    {
        public override string Name => "SoftwareLab Plugin";
        public override string Version => "1.0";
        public override string Author => "Software";

        private string configFilePath;

        public override bool Init()
        {
            string pluginFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            configFilePath = Path.Combine(pluginFolder, "PluginConfig.json");

            CreateDefaultConfig();

            return true;
        }

        public override bool Loaded()
        {
            ToolStripMenuItem gpsMenu = new ToolStripMenuItem("GPS Control");
            var btnPrimary = new ToolStripMenuItem("Set Primary GPS (GPS 1)", null, (s, e) => SetSingleParam("GPS_PRIMARY", 0));
            var btnSecondary = new ToolStripMenuItem("Set Secondary GPS (GPS 2)", null, (s, e) => SetSingleParam("GPS_PRIMARY", 1));
            var btnAuto = new ToolStripMenuItem("Toggle Auto Switch");

            gpsMenu.DropDownOpening += (s, e) => UpdateGpsAutoText(btnAuto);
            btnAuto.Click += (s, e) => ToggleGpsAuto();

            gpsMenu.DropDownItems.Add(btnPrimary);
            gpsMenu.DropDownItems.Add(btnSecondary);
            gpsMenu.DropDownItems.Add(btnAuto);

            ToolStripMenuItem frskyMenu = new ToolStripMenuItem("FrSky Control");
            var btnGive = new ToolStripMenuItem("Give Control to FrSky", null, (s, e) => ApplyJsonConfig("GiveControl"));
            var btnTake = new ToolStripMenuItem("Take Control from FrSky", null, (s, e) => ApplyJsonConfig("TakeControl"));
            var btnReload = new ToolStripMenuItem("Reload Configs", null, (s, e) => ShowAutoCloseMessage("Configs Reloaded!"));

            frskyMenu.DropDownItems.Add(btnGive);
            frskyMenu.DropDownItems.Add(btnTake);
            frskyMenu.DropDownItems.Add(new ToolStripSeparator());
            frskyMenu.DropDownItems.Add(btnReload);

            Host.FDMenuHud.Items.Add(gpsMenu);
            Host.FDMenuHud.Items.Add(frskyMenu);

            return true;
        }

        public override bool Loop()
        {
            return true;
        }

        private void SetSingleParam(string name, float val)
        {
            if (!Host.comPort.BaseStream.IsOpen) return;
            Host.comPort.setParam(name, val);
            ShowAutoCloseMessage($"Set {name} to {val}");
        }

        private void ToggleGpsAuto()
        {
            if (!Host.comPort.BaseStream.IsOpen) return;
            float current = (float)Host.comPort.MAV.param["GPS_AUTO_SWITCH"];
            float next = current == 1 ? 0 : 1;
            SetSingleParam("GPS_AUTO_SWITCH", next);
        }

        private void UpdateGpsAutoText(ToolStripMenuItem item)
        {
            if (Host.comPort.BaseStream.IsOpen && Host.comPort.MAV.param.ContainsKey("GPS_AUTO_SWITCH"))
            {
                float val = (float)Host.comPort.MAV.param["GPS_AUTO_SWITCH"];
                item.Text = val == 1 ? "Disable Auto Switch (ON)" : "Enable Auto Switch (OFF)";
            }
        }

        private void ApplyJsonConfig(string action)
        {
            if (!Host.comPort.BaseStream.IsOpen) return;

            try
            {
                string json = File.ReadAllText(configFilePath);
                var config = JsonConvert.DeserializeObject<PluginConfig>(json);
                var targetParams = action == "GiveControl" ? config.GiveControl : config.TakeControl;

                foreach (var kvp in targetParams)
                {
                    Host.comPort.setParam(kvp.Key, kvp.Value);
                }
                ShowAutoCloseMessage($"{action} Applied Successfully");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void CreateDefaultConfig()
        {
            if (!File.Exists(configFilePath))
            {
                var def = new PluginConfig
                {
                    GiveControl = new Dictionary<string, float> { { "FS_THR_ENABLE", 0 } },
                    TakeControl = new Dictionary<string, float> { { "FS_THR_ENABLE", 1 } }
                };
                File.WriteAllText(configFilePath, JsonConvert.SerializeObject(def, Formatting.Indented));
            }
        }

        private void ShowAutoCloseMessage(string message)
        {
            Form f = new Form
            {
                Text = "SoftwareLab Info",
                Size = new Size(300, 150),
                StartPosition = FormStartPosition.CenterScreen,
                TopMost = true,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                BackColor = Color.LightGreen
            };
            Label l = new Label
            {
                Text = message,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Arial", 9, FontStyle.Bold)
            };
            f.Controls.Add(l);
            Timer t = new Timer { Interval = 2500 };
            t.Tick += (s, e) => { t.Stop(); f.Close(); };
            t.Start();
            f.Show();
        }

        public override bool Exit() => true;
    }

    public class PluginConfig
    {
        public Dictionary<string, float> GiveControl { get; set; }
        public Dictionary<string, float> TakeControl { get; set; }
    }
}