using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.ClientState.Actors.Resolvers;
using GaugeAura.Gauges;
using Dalamud.Plugin;
using System.Diagnostics;
using Dalamud.Game.Internal;

/*
 * PLD: goring blade falling off of enemy?
 *  if on PLD, 
 * WAR: -
 * DRK: has gauge
 * GNB: 
 * 
 * AST
 * WHM
 * SCH
 * 
 * 
 * if on Pld, etc. -> hook status application
 *          check if goring blade, and from you
 *          if already has it, delete the old one
 */

namespace GaugeAura {
    public class GaugeManager {
        public Plugin _Plugin;
        public BaseGauge Gauge = null;
        private readonly Stopwatch updateTimer = new Stopwatch();

        // https://github.com/Caraxi/RemindMe

        public GaugeManager(Plugin plugin ) {
            _Plugin = plugin;
            updateTimer.Start();
        }

        public uint PlayerJobId = 0;

        public void Update( Framework framework ) {
            if( updateTimer.ElapsedMilliseconds >= 100 ) {
                updateTimer.Restart();

                var state = _Plugin.PluginInterface.ClientState;
                var player = state.LocalPlayer;
                if( player == null ) {
                    Reset();
                    return;
                }
                // PLAYER EXISTS
                if( player.ClassJob.Id != PlayerJobId ) {
                    // CHANGE JOBS
                    Gauge?.Remove();
                    switch( player.ClassJob.Id ) {
                        case 32:
                            Gauge = new DRK( this );
                            break;
                    }
                    PlayerJobId = player.ClassJob.Id;
                }
                Gauge?.Update( state );
            }
        }

        public void Reset() {
            Gauge?.Remove();
            PlayerJobId = 0;
        }

        public void Dispose() {
            Reset();
            updateTimer.Stop();
        }
    }
}
