namespace RJCP.IO.DeviceMgr
{
    using System;
    using System.Diagnostics;
    using Native;
    using Native.Win32;

#if NETSTANDARD
    using System.Buffers;
#endif

    internal class DeviceProperty<T>
    {
        private readonly DeviceInstance m_DevInst;
        private readonly CfgMgr32.CM_DRP m_DeviceProperty;
        private bool m_Retrieved;
        private object m_Value;

        public DeviceProperty(DeviceInstance devInst, CfgMgr32.CM_DRP deviceProperty)
        {
            m_DevInst = devInst;
            m_DeviceProperty = deviceProperty;

            if (typeof(T) != typeof(string) && typeof(T) != typeof(int) && typeof(T) != typeof(string[])) {
                string message = string.Format("Type {0} is not supported", typeof(T).Name);
                throw new NotSupportedException(message);
            }
        }

        public T Value
        {
            get
            {
                if (!m_Retrieved) {
                    m_Retrieved = true;
                    if (typeof(T) == typeof(string)) {
                        m_Value = GetPropertyString();
                    } else if (typeof(T) == typeof(int)) {
                        m_Value = GetPropertyInt();
                    } else if (typeof(T) == typeof(string[])) {
                        m_Value = GetPropertyStringArray();
                    }
                }
                return (T)m_Value;
            }
        }

        private string GetPropertyString()
        {
            CfgMgr32.CONFIGRET ret;
            int length = 0;
            ret = CfgMgr32.CM_Get_DevNode_Registry_Property(
                m_DevInst.InternalHandle, m_DeviceProperty, out int dataType,
                IntPtr.Zero, ref length, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS && ret != CfgMgr32.CONFIGRET.CR_BUFFER_SMALL) {
                if (ret != CfgMgr32.CONFIGRET.CR_NO_SUCH_VALUE &&
                    ret != CfgMgr32.CONFIGRET.CR_INVALID_PROPERTY) {
                    Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                        $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, return {ret}");
                }
                return string.Empty;
            }

            if (length <= 0) {
                if (length < 0)
                    Log.CfgMgr.TraceEvent(TraceEventType.Error,
                        $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, length is negative ({length})");
                return string.Empty;
            }

            Kernel32.REG_DATATYPE regDataType = (Kernel32.REG_DATATYPE)dataType;
            if (regDataType != Kernel32.REG_DATATYPE.REG_SZ) {
                Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                    $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, data type {regDataType} is not expected REG_SZ");
                return string.Empty;
            }

            if (length % 2 == 1) length++;

#if NETSTANDARD
            char[] blob = ArrayPool<char>.Shared.Rent(length / 2);
            try {
#else
            char[] blob = new char[length / 2];
#endif
                int bloblen = length / 2;

                ret = CfgMgr32.CM_Get_DevNode_Registry_Property(
                    m_DevInst.InternalHandle, m_DeviceProperty, out _, blob, ref length, 0);
                if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                    Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                        $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, return {ret} (length {length})");
                    return string.Empty;
                }

                // Subtract one for the NUL at the end.
                if (blob[bloblen - 1] == (char)0) bloblen--;
                return new string(blob, 0, bloblen);

#if NETSTANDARD
            } finally {
                ArrayPool<char>.Shared.Return(blob);
            }
#endif
        }

        private string[] GetPropertyStringArray()
        {
            CfgMgr32.CONFIGRET ret;
            int length = 0;
            ret = CfgMgr32.CM_Get_DevNode_Registry_Property(
                m_DevInst.InternalHandle, m_DeviceProperty, out int dataType,
                IntPtr.Zero, ref length, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS && ret != CfgMgr32.CONFIGRET.CR_BUFFER_SMALL) {
                if (ret != CfgMgr32.CONFIGRET.CR_NO_SUCH_VALUE &&
                    ret != CfgMgr32.CONFIGRET.CR_INVALID_PROPERTY) {
                    Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                        $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, return {ret}");
                }
#if NETSTANDARD
                return Array.Empty<string>();
#else
                return new string[0];
#endif
            }

            if (length <= 0) {
                if (length < 0)
                    Log.CfgMgr.TraceEvent(TraceEventType.Error,
                        $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, length is negative ({length})");
#if NETSTANDARD
                return Array.Empty<string>();
#else
                return new string[0];
#endif
            }

            Kernel32.REG_DATATYPE regDataType = (Kernel32.REG_DATATYPE)dataType;
            if (regDataType != Kernel32.REG_DATATYPE.REG_MULTI_SZ) {
                Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                    $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, data type {regDataType} is not expected REG_MULTI_SZ");
#if NETSTANDARD
                return Array.Empty<string>();
#else
                return new string[0];
#endif
            }

            if (length % 2 == 1) length++;

#if NETSTANDARD
            char[] blob = ArrayPool<char>.Shared.Rent(length / 2);
            try {
                int bloblen = length / 2;

                ret = CfgMgr32.CM_Get_DevNode_Registry_Property(
                    m_DevInst.InternalHandle, m_DeviceProperty, out dataType, blob, ref length, 0);
                if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                    Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                        $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, return {ret} (length {length})");
                    return Array.Empty<string>();
                }
                return Marshalling.GetMultiSz(blob.AsSpan(0, bloblen)).ToArray();
            } finally {
                ArrayPool<char>.Shared.Return(blob);
            }
#else
            char[] blob = new char[length / 2];
            ret = CfgMgr32.CM_Get_DevNode_Registry_Property(
                    m_DevInst.InternalHandle, m_DeviceProperty, out dataType, blob, ref length, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                    $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, return {ret} (length {length})");
                return new string[0];
            }
            return Marshalling.GetMultiSz(blob).ToArray();
#endif
        }

        private int GetPropertyInt()
        {
            int length = 4;
            CfgMgr32.CONFIGRET ret = CfgMgr32.CM_Get_DevNode_Registry_Property(
                m_DevInst.InternalHandle, m_DeviceProperty, out int dataType,
                out int value, ref length, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                if (ret != CfgMgr32.CONFIGRET.CR_NO_SUCH_VALUE &&
                    ret != CfgMgr32.CONFIGRET.CR_INVALID_PROPERTY) {
                    Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                        $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, return {ret}");
                }
                return 0;
            }

            Kernel32.REG_DATATYPE regDataType = (Kernel32.REG_DATATYPE)dataType;
            if (regDataType != Kernel32.REG_DATATYPE.REG_DWORD) {
                Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                    $"{m_DevInst}: Couldn't get property for {m_DeviceProperty}, data type {regDataType} is not expected REG_DWORD");
                return 0;
            }
            return value;
        }
    }
}
