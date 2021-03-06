using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Plugin;
using GaugeAura.Structs;
using VFXEditor.Util;
using FileMode = GaugeAura.Structs.FileMode;
using Reloaded.Hooks;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace GaugeAura {
    public class ResourceLoader : IDisposable
    {
        public Plugin Plugin { get; set; }
        public Dictionary<string, string> PathMapping;

        public bool IsEnabled { get; set; }

        //====== STATIC ===========
        [UnmanagedFunctionPointer( CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
        public delegate IntPtr VfxCreateDelegate( string path, string pool );
        public VfxCreateDelegate VfxCreate;

        [UnmanagedFunctionPointer( CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
        public delegate IntPtr VfxRunDelegate( IntPtr vfx, float a1, uint a2 );
        public VfxRunDelegate VfxRun;

        [UnmanagedFunctionPointer( CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
        public delegate IntPtr VfxRemoveDelegate( IntPtr vfx );
        public VfxRemoveDelegate VfxRemove;

        // ======== ACTOR =============
        [UnmanagedFunctionPointer( CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
        public delegate IntPtr StatusAddDelegate( string a1, IntPtr a2, IntPtr a3, float a4, char a5, UInt16 a6, char a7 );
        public StatusAddDelegate StatusAdd;

        [UnmanagedFunctionPointer( CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
        public delegate IntPtr StatusRemoveDelegate( IntPtr a1 );
        public StatusRemoveDelegate StatusRemove;

        [UnmanagedFunctionPointer( CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
        public delegate IntPtr StatusDeallocDelegate( IntPtr a1, UInt64 a2 );
        public StatusDeallocDelegate StatusDealloc;

        // ==========================
        public Crc32 Crc32 { get; }
        [Function( CallingConventions.Microsoft )]
        public unsafe delegate byte ReadFilePrototype( IntPtr pFileHandler, SeFileDescriptor* pFileDesc, int priority, bool isSync );
        [Function( CallingConventions.Microsoft )]
        public unsafe delegate byte ReadSqpackPrototype( IntPtr pFileHandler, SeFileDescriptor* pFileDesc, int priority, bool isSync );
        [Function( CallingConventions.Microsoft )]
        public unsafe delegate void* GetResourceSyncPrototype( IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown );
        [Function( CallingConventions.Microsoft )]
        public unsafe delegate void* GetResourceAsyncPrototype( IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown );
        public IHook<GetResourceSyncPrototype> GetResourceSyncHook { get; private set; }
        public IHook<GetResourceAsyncPrototype> GetResourceAsyncHook { get; private set; }
        public IHook<ReadSqpackPrototype> ReadSqpackHook { get; private set; }
        public ReadFilePrototype ReadFile { get; private set; }

        public ResourceLoader( Plugin plugin )
        {
            Plugin = plugin;
            Crc32 = new Crc32();
            PathMapping = new Dictionary<string, string>();
        }

        public unsafe void Init()
        {
            var scanner = Plugin.PluginInterface.TargetModuleScanner;

            var vfxCreateAddress = scanner.ScanText( "E8 ?? ?? ?? ?? F3 0F 10 35 ?? ?? ?? ?? 48 89 43 08" );
            var vfxRunAddress = scanner.ScanText( "E8 ?? ?? ?? ?? 0F 28 B4 24 ?? ?? ?? ?? 48 8B 8C 24 ?? ?? ?? ?? 48 33 CC E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ?? 48 81 C4 ?? ?? ?? ?? 5F" );
            var vfxRemoveAddress = scanner.ScanText( "40 53 48 83 EC 20 48 8B D9 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 28 33 D2 E8 ?? ?? ?? ?? 48 8B 8B ?? ?? ?? ?? 48 85 C9" );
            var statusAddAddr = scanner.ScanText( "40 53 55 56 57 48 81 EC ?? ?? ?? ?? 0F 29 B4 24 ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 0F B6 AC 24 ?? ?? ?? ?? 0F 28 F3 49 8B F8" );
            var statusRemoveAddr = scanner.ScanText( "40 53 48 83 EC 20 48 8D 05 ?? ?? ?? ?? 48 8B D9 48 89 01 48 8D 05 ?? ?? ?? ?? 48 89 81 ?? ?? ?? ?? 48 8B 89 ?? ?? ?? ?? 48 85 C9 74 09 48 8B 01 48 8B D3" );
            var statusDeallocAddr = scanner.ScanText( "48 85 C9 74 63 53 48 83 EC 20 48 83 3D ?? ?? ?? ?? ?? 48 8B D9 75 0A 48 83 C4 20 5B" );

            StatusRemove = Marshal.GetDelegateForFunctionPointer<StatusRemoveDelegate>( statusRemoveAddr );
            StatusAdd = Marshal.GetDelegateForFunctionPointer<StatusAddDelegate>( statusAddAddr );
            VfxRemove = Marshal.GetDelegateForFunctionPointer<VfxRemoveDelegate>( vfxRemoveAddress );
            VfxRun = Marshal.GetDelegateForFunctionPointer<VfxRunDelegate>( vfxRunAddress );
            VfxCreate = Marshal.GetDelegateForFunctionPointer<VfxCreateDelegate>( vfxCreateAddress );
            StatusDealloc = Marshal.GetDelegateForFunctionPointer<StatusDeallocDelegate>( statusDeallocAddr );

            // =========================

            var readFileAddress = scanner.ScanText( "E8 ?? ?? ?? ?? 84 C0 0F 84 ?? 00 00 00 4C 8B C3 BA 05" );
            var readSqpackAddress = scanner.ScanText( "E8 ?? ?? ?? ?? EB 05 E8 ?? ?? ?? ?? 84 C0 0F 84 ?? 00 00 00 4C 8B C3" );
            var getResourceSyncAddress = scanner.ScanText( "E8 ?? ?? 00 00 48 8D 4F ?? 48 89 87 ?? ?? 00 00" );
            var getResourceAsyncAddress = scanner.ScanText( "E8 ?? ?? ?? 00 48 8B D8 EB ?? F0 FF 83 ?? ?? 00 00" );

            ReadSqpackHook = new Hook<ReadSqpackPrototype>( ReadSqpackHandler, ( long )readSqpackAddress );
            GetResourceSyncHook = new Hook<GetResourceSyncPrototype>( GetResourceSyncHandler, ( long )getResourceSyncAddress );
            GetResourceAsyncHook = new Hook<GetResourceAsyncPrototype>( GetResourceAsyncHandler, ( long )getResourceAsyncAddress );
            ReadFile = Marshal.GetDelegateForFunctionPointer<ReadFilePrototype>( readFileAddress );
        }

        // =====================

        private unsafe void* GetResourceSyncHandler(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown)
            => GetResourceHandler( true, pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, false );
        private unsafe void* GetResourceAsyncHandler(IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown)
            => GetResourceHandler( false, pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown );
        private unsafe void* CallOriginalHandler(bool isSync, IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown)
            => isSync
                ? GetResourceSyncHook.OriginalFunction( pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown )
                : GetResourceAsyncHook.OriginalFunction( pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown );

        private unsafe void* GetResourceHandler(bool isSync, IntPtr pFileManager, uint* pCategoryId, char* pResourceType, uint* pResourceHash, char* pPath, void* pUnknown, bool isUnknown) {
            var gameFsPath = Marshal.PtrToStringAnsi( new IntPtr( pPath ) );

            // ============ REPLACE WITH A LOCAL FILE, IF NECESSARY ============
            FileInfo replaceFile = null;
            if( PathMapping.ContainsKey( gameFsPath ) ) {
                replaceFile = new FileInfo( PathMapping[gameFsPath] );
            }

            var fsPath = replaceFile?.FullName;
            if( fsPath == null || fsPath.Length >= 260 ) {
                return CallOriginalHandler( isSync, pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown );
            }
            var cleanPath = fsPath.Replace( '\\', '/' );
            var path = Encoding.ASCII.GetBytes( cleanPath );
            var bPath = stackalloc byte[path.Length + 1];
            Marshal.Copy( path, 0, new IntPtr( bPath ), path.Length );
            pPath = ( char* )bPath;
            Crc32.Init();
            Crc32.Update( path );
            *pResourceHash = Crc32.Checksum;
            return CallOriginalHandler( isSync, pFileManager, pCategoryId, pResourceType, pResourceHash, pPath, pUnknown, isUnknown );
        }
        private unsafe byte ReadSqpackHandler( IntPtr pFileHandler, SeFileDescriptor* pFileDesc, int priority, bool isSync ) {
            var gameFsPath = Marshal.PtrToStringAnsi( new IntPtr( pFileDesc->ResourceHandle->FileName ) );
            var isRooted = Path.IsPathRooted( gameFsPath );
            if( gameFsPath == null || gameFsPath.Length >= 260 || !isRooted ) {
                return ReadSqpackHook.OriginalFunction( pFileHandler, pFileDesc, priority, isSync );
            }
            pFileDesc->FileMode = FileMode.LoadUnpackedResource;
            var utfPath = Encoding.Unicode.GetBytes( gameFsPath );
            Marshal.Copy( utfPath, 0, new IntPtr( &pFileDesc->UtfFileName ), utfPath.Length );
            var fd = stackalloc byte[0x20 + utfPath.Length + 0x16];
            Marshal.Copy( utfPath, 0, new IntPtr( fd + 0x21 ), utfPath.Length );
            pFileDesc->FileDescriptor = fd;
            return ReadFile( pFileHandler, pFileDesc, priority, isSync );
        }

        // ======================

        public void Enable()
        {
            IsEnabled = true;

            ReadSqpackHook.Activate();
            GetResourceSyncHook.Activate();
            GetResourceAsyncHook.Activate();

            ReadSqpackHook.Enable();
            GetResourceSyncHook.Enable();
            GetResourceAsyncHook.Enable();
        }

        public void Disable()
        {
            if( !IsEnabled )
                return;
            IsEnabled = false;

            ReadSqpackHook.Disable();
            GetResourceSyncHook.Disable();
            GetResourceAsyncHook.Disable();
        }

        public void Dispose()
        {
            if( IsEnabled )
                Disable();
        }
    }
}