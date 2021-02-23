using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState;
using GaugeAura.Vfx;
using Dalamud.Game.ClientState.Structs.JobGauge;

namespace GaugeAura.Gauges {
    public class DRK : BaseGauge {
        public BaseVfx BloodVfx = null;
        public byte LastBlood = 0;

        public DRK(GaugeManager manager) : base(manager) {

        }

        public override void Update(ClientState state) {
            var blood = state.JobGauges.Get<DRKGauge>().Blood;
            if(blood > 0 && LastBlood == 0 ) {
                BloodVfx = new ActorVfx( Manager._Plugin, state.LocalPlayer, state.LocalPlayer, "vfx/common/eff/c601_blue_c0v.avfx" );
            }
            else if(blood == 0 && LastBlood > 0 ) {
                BloodVfx?.Remove();
            }
            LastBlood = blood;
        }

        public override void Remove() {
            BloodVfx?.Remove();
        }
    }
}
