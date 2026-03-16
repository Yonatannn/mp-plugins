using System.Collections.Generic;

namespace MissionPlanner.SoftwareLab
{
    public class FrSkyPluginConfig
    {
        public Dictionary<string, float> GiveControl { get; set; }
        public Dictionary<string, float> TakeControl { get; set; }
    }
}
