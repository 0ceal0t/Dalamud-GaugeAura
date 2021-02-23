using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;

namespace GaugeAura.Gauges {

    public abstract class BaseGauge {
        public GaugeManager Manager;
        public BaseGauge(GaugeManager manager ) {
            Manager = manager;
        }

        public abstract void Update( ClientState state );
        public abstract void Remove();
    }
}
