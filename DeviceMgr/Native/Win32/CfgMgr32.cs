﻿namespace RJCP.Native.Win32
{
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;
    using Microsoft.Win32.SafeHandles;

    [SuppressUnmanagedCodeSecurity]
    internal static partial class CfgMgr32
    {
        [DllImport("cfgmgr32.dll", SetLastError = false, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "CM_Locate_DevNodeW")]
        public static extern CONFIGRET CM_Locate_DevNode(out SafeDevInst devInst, string devInstId, CM_LOCATE_DEVINST flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern CONFIGRET CM_Get_Child(out SafeDevInst childDevInst, SafeDevInst devInst, int flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern CONFIGRET CM_Get_Sibling(out SafeDevInst childDevInst, SafeDevInst devInst, int flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern CONFIGRET CM_Get_Device_ID_Size(out int length, SafeDevInst devInst, int flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "CM_Get_Device_IDW")]
        public static extern CONFIGRET CM_Get_Device_ID(SafeDevInst devInst, StringBuilder buffer, int bufferLen, int flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, ExactSpelling = true)]
        public static extern CONFIGRET CM_Get_DevNode_Status(out DN_STATUS status, out int problem, SafeDevInst devInst, int flags);

        // We must set CharSet so when we query the length for strings, we get the correct value.
        [DllImport("cfgmgr32.dll", SetLastError = false, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "CM_Get_DevNode_Registry_PropertyW")]
        public static extern CONFIGRET CM_Get_DevNode_Registry_Property(SafeDevInst devInst, CM_DRP property, out int dataType, IntPtr buffer, ref int bufferLen, int flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "CM_Get_DevNode_Registry_PropertyW")]
        public static extern CONFIGRET CM_Get_DevNode_Registry_Property(SafeDevInst devInst, CM_DRP property, out int dataType, out int buffer, ref int bufferLen, int flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, CharSet = CharSet.Unicode, ExactSpelling = true, EntryPoint = "CM_Get_DevNode_Registry_PropertyW")]
        public static extern CONFIGRET CM_Get_DevNode_Registry_Property(SafeDevInst devInst, CM_DRP property, out int dataType, [Out] char[] buffer, ref int bufferLen, int flags);

        [DllImport("cfgmgr32.dll", SetLastError = false, ExactSpelling = true, EntryPoint = "CM_Open_DevNode_Key")]
        public static extern CONFIGRET CM_Open_DevNode_Key(SafeDevInst devInst, Kernel32.REGSAM samDesired, int hardwareProfile, RegDisposition disposition, out SafeRegistryHandle device, int flags);
    }
}
