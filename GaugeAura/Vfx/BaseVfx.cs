using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GaugeAura.Vfx {
    public abstract class BaseVfx {
        public Plugin _Plugin;
        public IntPtr Vfx;
        public string Path; // the ".avfx"
        public bool Redirect = false; // whether to redirect to a local file
        public string RedirectPath;

        public BaseVfx(Plugin plugin, string path, string realPath ) {
            _Plugin = plugin;
            Path = path;
            if(realPath != "" ) {
                Redirect = true;
                RedirectPath = realPath;
                _Plugin.ResourceLoader.PathMapping[Path] = RedirectPath;
            }
        }

        public void Remove() {
            if(Redirect && _Plugin.ResourceLoader.PathMapping.ContainsKey( Path ) ) {
                _Plugin.ResourceLoader.PathMapping.Remove( Path );
            }
            OnRemove();
        }

        public abstract void OnRemove();
    }
}
