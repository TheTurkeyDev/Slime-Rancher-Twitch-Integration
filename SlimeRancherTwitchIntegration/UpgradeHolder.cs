using MonomiPark.SlimeRancher.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SlimeRancherTwitchIntegration
{
    public class UpgradeHolder
    {
        public DateTime Spawned { get; }
        public int Duration { get; }
        private LandPlot plot;
        private HashSet<LandPlot.Upgrade> upgrades;

        public UpgradeHolder(LandPlot plot, HashSet<LandPlot.Upgrade> upgrades, int duration)
        {
            this.plot = plot;
            this.Spawned = DateTime.UtcNow;
            this.upgrades = new HashSet<LandPlot.Upgrade>(upgrades);
            this.Duration = duration;
        }

        public void Reset()
        {
            foreach (LandPlot.Upgrade upgrade in upgrades)
                plot.AddUpgrade(upgrade);
        }
    }
}
