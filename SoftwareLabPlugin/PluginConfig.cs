using System.Collections.Generic;

namespace MissionPlanner.SoftwareLab
{
    public class PluginConfig
    {
        public Dictionary<string, float> GiveControl { get; set; }
        public Dictionary<string, float> TakeControl { get; set; }
    }
}
