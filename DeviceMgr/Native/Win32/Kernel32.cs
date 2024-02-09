namespace RJCP.Native.Win32
{
    using System.Runtime.InteropServices;
    using System.Security;

    [SuppressUnmanagedCodeSecurity]
    internal static partial class Kernel32
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "GetVersionExW")]
        public static extern bool GetVersionEx([In, Out] OSVERSIONINFO osVersionInfo);
    }
}
