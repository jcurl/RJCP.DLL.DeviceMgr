﻿namespace RJCP.Native.Win32
{
    using System.Runtime.InteropServices;

    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class OSVERSIONINFO
        {
            public int OSVersionInfoSize;
            public int MajorVersion;
            public int MinorVersion;
            public int BuildNumber;
            public WinPlatformId PlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x80)]
            public string CSDVersion;

            public OSVERSIONINFO()
            {
                OSVersionInfoSize = Marshal.SizeOf(this);
            }
        }
    }
}
