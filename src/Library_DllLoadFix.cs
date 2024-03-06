using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ic4;

namespace VL.Devices.TheImagingSource
{
    internal class Library_DllLoadFix
    {
        private static object _handleLock = new object();

        private static IntPtr _handle = IntPtr.Zero;

        //
        // Summary:
        //     Checks whether the library was initialized by a successful call to ic4.Library.Init(ic4.LogLevel,ic4.LogLevel,ic4.LogTarget,System.String).
        public static bool IsInitialized => _handle != IntPtr.Zero;

        private static void CheckPlatformSupported()
        {
            if (OperatingSystem.IsWindows() && IntPtr.Size == 4)
            {
                throw new NotSupportedException("IC Imaging Control 4 .NET does not support x86 Windows applications.\nChange your project settings to x64, or, when the project is set to 'Any CPU' disable 'Prefer 32-bit' in the build options.");
            }
        }

        //
        // Summary:
        //     Initializes the IC Imaging Control 4 .NET library.
        //
        // Parameters:
        //   apiLogLevel:
        //     Configures the API log level for the library.
        //
        //   internalLogLevel:
        //     Configures the internal log level for the library.
        //
        //   logTargets:
        //     Configures the log targets.
        //
        //   logFilePath:
        //     If logTargets includes ic4.LogTarget.File, specifies the log file to use.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The platform is not supported
        public static void Init(LogLevel apiLogLevel = LogLevel.Off, LogLevel internalLogLevel = LogLevel.Off, LogTarget logTargets = LogTarget.None, string? logFilePath = null)
        {
            lock (_handleLock)
            {
                if (!(_handle == IntPtr.Zero))
                {
                    return;
                }

                CheckPlatformSupported();

                _handle = NativeLibrary.Load("ic4core", typeof(Library_DllLoadFix).Assembly, searchPath: default);

                IC4_INIT_CONFIG iC4_INIT_CONFIG = default(IC4_INIT_CONFIG);
                iC4_INIT_CONFIG.api_log_level = (IC4_LOG_LEVEL)apiLogLevel;
                iC4_INIT_CONFIG.internal_log_level = (IC4_LOG_LEVEL)internalLogLevel;
                iC4_INIT_CONFIG.log_targets = (IC4_LOG_TARGET_FLAGS)logTargets;
                iC4_INIT_CONFIG.log_file = logFilePath;
                IC4_INIT_CONFIG init_config = iC4_INIT_CONFIG;
                if (!InitLibrary(ref init_config))
                {
                    ThrowLastError();
                    //IC4Exception.ThrowLastError();
                }
            }
        }

        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
        private static extern void ThrowLastError(IC4Exception? @this = null, int extraStackFrames = 0, bool throwIfNoError = true, IEnumerable<ErrorCode>? ignoreErrors = null);

        [DllImport("ic4core", EntryPoint = "ic4_init_library")]
        [return: MarshalAs(UnmanagedType.U1)]
        private static extern bool InitLibrary(ref IC4_INIT_CONFIG init_config);

        internal struct IC4_INIT_CONFIG
        {
            public IC4_LOG_LEVEL api_log_level;

            public IC4_LOG_LEVEL internal_log_level;

            public IC4_LOG_TARGET_FLAGS log_targets;

            [MarshalAs(UnmanagedType.LPStr)]
            public string? log_file;
        }

        internal enum IC4_LOG_LEVEL
        {
            IC4_LOG_OFF,
            IC4_LOG_ERROR,
            IC4_LOG_WARN,
            IC4_LOG_INFO,
            IC4_LOG_DEBUG,
            IC4_LOG_TRACE
        }

        internal enum IC4_LOG_TARGET_FLAGS
        {
            IC4_LOGTARGET_DISABLE = 0,
            IC4_LOGTARGET_STDOUT = 1,
            IC4_LOGTARGET_STDERR = 2,
            IC4_LOGTARGET_FILE = 4,
            IC4_LOGTARGET_WINDEBUG = 8
        }
    }
}
