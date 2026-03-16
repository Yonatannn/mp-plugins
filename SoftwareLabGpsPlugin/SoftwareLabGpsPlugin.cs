using System;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab
{
    public class SoftwareLabGpsPlugin : SoftwareLabPluginBase
    {
        private const string GpsPrimaryParamName = "GPS_PRIMARY";
        private const string GpsAutoSwitchParamName = "GPS_AUTO_SWITCH";
        private const string GpsMenuText = "GPS Control";

        public override string Name => "SoftwareLab GPS Plugin";

        public override bool Loaded()
        {
            return LoadMenu(GpsMenuText, CreateGpsMenu, "load GPS menu");
        }

        public override bool Exit()
        {
            return ExitMenu(GpsMenuText);
        }

        private ToolStripMenuItem CreateGpsMenu()
        {
            ToolStripMenuItem menu = new ToolStripMenuItem(GpsMenuText);
            ToolStripMenuItem autoItem = CreateMenuItem("Toggle Auto Switch", (sender, args) => ToggleGpsAuto((ToolStripMenuItem)sender));

            menu.DropDownOpening += (sender, args) => UpdateGpsAutoText(autoItem);
            menu.DropDownItems.AddRange(
                new ToolStripItem[]
                {
                    CreateMenuItem("Set Primary GPS (GPS 1)", (sender, args) => SetSingleParam(GpsPrimaryParamName, 0)),
                    CreateMenuItem("Set Secondary GPS (GPS 2)", (sender, args) => SetSingleParam(GpsPrimaryParamName, 1)),
                    autoItem
                });

            return menu;
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
                }

                float nextValue = Math.Abs(currentValue - 1f) < 0.001f ? 0f : 1f;

                if (SetSingleParam(GpsAutoSwitchParamName, nextValue, true))
                    UpdateGpsAutoText(item);
            }
            catch (Exception ex)
            {
                ShowAutoCloseMessage($"Failed to toggle switch: {ex.Message}", NotificationSeverity.Error);
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
    }
}
