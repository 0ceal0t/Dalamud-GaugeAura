using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Command;
using Dalamud.Plugin;

namespace GaugeAura {
    public class Plugin : IDalamudPlugin {
        public string Name => "GaugeAura";
        private const string CommandName = "/ga";

        public DalamudPluginInterface PluginInterface;
        public ResourceLoader ResourceLoader;
        public GaugeManager Manager;

        public void Initialize( DalamudPluginInterface pluginInterface ) {
            PluginInterface = pluginInterface;
            ResourceLoader = new ResourceLoader( this );
            Manager = new GaugeManager( this );

            PluginInterface.CommandManager.AddHandler( CommandName, new CommandInfo( OnCommand )
            {
                HelpMessage = "/ga - test"
            } );

            ResourceLoader.Init();
            ResourceLoader.Enable();
            PluginInterface.Framework.OnUpdateEvent += Manager.Update;
        }

        public void Dispose() {
            PluginInterface.Framework.OnUpdateEvent -= Manager.Update;
            Manager.Dispose();

            PluginInterface.CommandManager.RemoveHandler( CommandName );
            PluginInterface.Dispose();
            ResourceLoader.Dispose();
        }
        private void OnCommand( string command, string rawArgs ) {
            var args = rawArgs.Split( ' ' );
            if( args.Length > 0 && args[0].Length > 0 ) {
                switch( args[0] ) {
                }

                return;
            }
        }
    }
}