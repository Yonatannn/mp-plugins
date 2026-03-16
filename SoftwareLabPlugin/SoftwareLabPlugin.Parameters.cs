using MissionPlanner.SoftwareLab.Notifications;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public partial class SoftwareLabPlugin
    {
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
            AddNotificationLine(lines, successMessages, NotificationSeverity.Success);
            AddNotificationLine(lines, failureMessages, NotificationSeverity.Error);
            return lines;
        }

        private static void AddNotificationLine(ICollection<NotificationLine> lines, IReadOnlyCollection<string> messages, NotificationSeverity severity)
        {
            if (messages.Count == 0)
                return;

            lines.Add(new NotificationLine(string.Join(", ", messages), severity));
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
    }
}
