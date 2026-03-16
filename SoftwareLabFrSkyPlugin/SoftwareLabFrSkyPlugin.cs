using MissionPlanner.SoftwareLab.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public class SoftwareLabFrSkyPlugin : SoftwareLabPluginBase
    {
        private const string ConfigFileName = "PluginConfig.json";
        private const string FrSkyMenuText = "FrSky Control";

        private string configFilePath;

        public override string Name => "SoftwareLab FrSky Plugin";

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
                ShowPluginError("initialize FrSky plugin", ex);
                return false;
            }
        }

        public override bool Loaded()
        {
            return LoadMenu(FrSkyMenuText, CreateFrSkyMenu, "load FrSky menu");
        }

        public override bool Exit()
        {
            return ExitMenu(FrSkyMenuText);
        }

        private ToolStripMenuItem CreateFrSkyMenu()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(FrSkyMenuText);
            menu.DropDownItems.AddRange(
                new ToolStripItem[]
                {
                    CreateMenuItem("Give Control to FrSky", (sender, args) => ApplyControlConfig(true)),
                    CreateMenuItem("Take Control from FrSky", (sender, args) => ApplyControlConfig(false))
                });

            return menu;
        }

        private void ApplyControlConfig(bool giveControl)
        {
            if (!EnsureConnected())
                return;

            try
            {
                FrSkyPluginConfig config = LoadConfig();
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

            foreach (KeyValuePair<string, float> kvp in targetParams)
            {
                bool success = SetSingleParam(kvp.Key, kvp.Value, false, false);
                List<string> targetMessages = success ? successMessages : failureMessages;
                targetMessages.Add(GetSetParamMessage(kvp.Key, kvp.Value, success));
            }

            return CreateNotificationLines(successMessages, failureMessages);
        }

        private FrSkyPluginConfig LoadConfig()
        {
            if (string.IsNullOrWhiteSpace(configFilePath))
                throw new InvalidOperationException("Config file path is not initialized.");

            if (!File.Exists(configFilePath))
                CreateDefaultConfig();

            string json = File.ReadAllText(configFilePath);
            FrSkyPluginConfig config = JsonConvert.DeserializeObject<FrSkyPluginConfig>(json);

            if (config == null)
                throw new InvalidOperationException("Config file is empty or invalid.");

            return EnsureConfigCollections(config);
        }

        private void CreateDefaultConfig()
        {
            if (string.IsNullOrWhiteSpace(configFilePath) || File.Exists(configFilePath))
                return;

            FrSkyPluginConfig defaultConfig = new FrSkyPluginConfig
            {
                GiveControl = new Dictionary<string, float> { { "FS_THR_ENABLE", 0 } },
                TakeControl = new Dictionary<string, float> { { "FS_THR_ENABLE", 1 } }
            };

            File.WriteAllText(configFilePath, JsonConvert.SerializeObject(defaultConfig, Formatting.Indented));
        }

        private static FrSkyPluginConfig EnsureConfigCollections(FrSkyPluginConfig config)
        {
            if (config.GiveControl == null)
                config.GiveControl = new Dictionary<string, float>();

            if (config.TakeControl == null)
                config.TakeControl = new Dictionary<string, float>();

            return config;
        }
    }
}
