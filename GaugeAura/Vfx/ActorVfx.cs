using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors.Types;

namespace GaugeAura.Vfx {
    public class ActorVfx : BaseVfx {
        public ActorVfx( Plugin plugin, Actor caster, Actor target, string path, string realPath = "" ) : base( plugin, path, realPath ) {
            Vfx = _Plugin.ResourceLoader.StatusAdd( path, caster.Address, target.Address, -1, ( char )0, 0, ( char )0 );
        }

        public override void OnRemove() {
            _Plugin.ResourceLoader.StatusRemove( Vfx );
            _Plugin.ResourceLoader.StatusDealloc( Vfx, 0x1d0 );
        }
    }
}
