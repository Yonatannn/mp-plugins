using System;
using System.Windows.Forms;

namespace MissionPlanner.SoftwareLab.Notifications
{
    public class SoftwareLabNotificationsPlugin : MissionPlanner.Plugin.Plugin
    {
        private SoftwareLabNotificationService notificationService;

        public override string Name => "SoftwareLab Notifications Plugin";
        public override string Version => "1.0";
        public override string Author => "Software";

        public override bool Init()
        {
            notificationService = new SoftwareLabNotificationService(GetMissionPlannerForm);
            return true;
        }

        public override bool Loaded()
        {
            NotificationServiceRegistry.Register(notificationService);
            return true;
        }

        public override bool Loop()
        {
            return true;
        }

        public override bool Exit()
        {
            NotificationServiceRegistry.Unregister(notificationService);
            notificationService?.Dispose();
            notificationService = null;
            return true;
        }

        private Form GetMissionPlannerForm()
        {
            return Host?.FDMenuHud?.FindForm();
        }
    }
}
