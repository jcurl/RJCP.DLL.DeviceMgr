namespace RJCP.Native.Win32
{
    using System;
    using System.Buffers;

    internal static partial class CfgMgr32
    {
        // P/Invoke methods specific for .NET Framework

        // Don't use StringBuilder, as it's slow [CA1838]. Allocate either on the stack, or using an ArrayPool for best
        // performance.
        //
        // [CA1838] https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1838

        public static unsafe CONFIGRET CM_Get_DevNode_Registry_Property(SafeDevInst devInst, CM_DRP property, out int dataType, out string buffer)
        {
            int length = 0;
            CONFIGRET ret = CM_Get_DevNode_Registry_Property(devInst, property, out dataType, IntPtr.Zero, ref length, 0);
            if (ret != CONFIGRET.CR_SUCCESS && ret != CONFIGRET.CR_BUFFER_SMALL) {
                buffer = string.Empty;
                return ret;
            }

            if (length <= 0) {
                buffer = string.Empty;
                return CONFIGRET.CR_UNEXPECTED_LENGTH;
            }

            Kernel32.REG_DATATYPE regDataType = (Kernel32.REG_DATATYPE)dataType;
            if (regDataType != Kernel32.REG_DATATYPE.REG_SZ) {
                buffer = string.Empty;
                return CONFIGRET.CR_UNEXPECTED_TYPE;
            }

            if (length % 2 == 1) length++;
            int bloblen = length / 2;

            if (length <= MaxLengthStack) {
                Span<char> blob = stackalloc char[bloblen];
                fixed (char* blobptr = blob) {
                    ret = CM_Get_DevNode_Registry_Property(devInst, property, out _, blobptr, ref length, 0);
                    if (ret != CONFIGRET.CR_SUCCESS) {
                        buffer = string.Empty;
                        return ret;
                    }
                }

                // Subtract one for the NUL at the end.
                if (blob[bloblen - 1] == (char)0) bloblen--;
                buffer = new string(blob[..bloblen]);
            } else {
                char[] blob = ArrayPool<char>.Shared.Rent(length / 2);
                try {
                    ret = CM_Get_DevNode_Registry_Property(devInst, property, out _, blob, ref length, 0);
                    if (ret != CONFIGRET.CR_SUCCESS) {
                        buffer = string.Empty;
                        return ret;
                    }

                    // Subtract one for the NUL at the end.
                    if (blob[bloblen - 1] == (char)0) bloblen--;
                    buffer = new string(blob, 0, bloblen);
                } finally {
                    ArrayPool<char>.Shared.Return(blob);
                }
            }
            return ret;
        }

        public static unsafe CONFIGRET CM_Get_DevNode_Registry_Property(SafeDevInst devInst, CM_DRP property, out int dataType, out string[] buffer)
        {
            int length = 0;
            CONFIGRET ret = CM_Get_DevNode_Registry_Property(devInst, property, out dataType, IntPtr.Zero, ref length, 0);
            if (ret != CONFIGRET.CR_SUCCESS && ret != CONFIGRET.CR_BUFFER_SMALL) {
                buffer = Array.Empty<string>();
                return ret;
            }

            if (length <= 0) {
                buffer = Array.Empty<string>();
                return CONFIGRET.CR_UNEXPECTED_LENGTH;
            }

            Kernel32.REG_DATATYPE regDataType = (Kernel32.REG_DATATYPE)dataType;
            if (regDataType != Kernel32.REG_DATATYPE.REG_MULTI_SZ) {
                buffer = Array.Empty<string>();
                return CONFIGRET.CR_UNEXPECTED_TYPE;
            }

            if (length % 2 == 1) length++;
            int bloblen = length / 2;

            if (length < MaxLengthStack) {
                Span<char> blob = stackalloc char[bloblen];
                fixed (char* blobptr = blob) {
                    ret = CM_Get_DevNode_Registry_Property(devInst, property, out _, blobptr, ref length, 0);
                    if (ret != CONFIGRET.CR_SUCCESS) {
                        buffer = Array.Empty<string>();
                        return ret;
                    }
                }
                buffer = Marshalling.GetMultiSz(blob[..bloblen]).ToArray();
            } else {
                char[] blob = ArrayPool<char>.Shared.Rent(length / 2);
                try {
                    ret = CM_Get_DevNode_Registry_Property(devInst, property, out _, blob, ref length, 0);
                    if (ret != CONFIGRET.CR_SUCCESS) {
                        buffer = Array.Empty<string>();
                        return ret;
                    }

                    // Subtract one for the NUL at the end.
                    if (blob[bloblen - 1] == (char)0) bloblen--;
                    buffer = Marshalling.GetMultiSz(blob, bloblen).ToArray();
                } finally {
                    ArrayPool<char>.Shared.Return(blob);
                }
            }

            return ret;
        }

        public static unsafe CONFIGRET CM_Get_Device_ID_List(string filter, out string[] buffer)
        {
            // We don't use the stack, as the list is usually quite long.

            CONFIGRET ret = CM_Get_Device_ID_List_Size(out int length, filter, 0);
            if (ret != CONFIGRET.CR_SUCCESS) {
                buffer = Array.Empty<string>();
                return ret;
            }

            char[] blob = ArrayPool<char>.Shared.Rent(length);
            try {
                ret = CM_Get_Device_ID_List(filter, blob, length, 0);
                if (ret != CONFIGRET.CR_SUCCESS) {
                    buffer = Array.Empty<string>();
                    return ret;
                }
                buffer = Marshalling.GetMultiSz(blob, length).ToArray();
            } finally {
                ArrayPool<char>.Shared.Return(blob);
            }
            return ret;
        }
    }
}
