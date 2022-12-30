namespace RJCP.IO.DeviceMgr
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Text;
    using Microsoft.Win32;
    using Microsoft.Win32.SafeHandles;
    using Native;
    using Native.Win32;

    /// <summary>
    /// A Device Instance representing a device in the system.
    /// </summary>
    public class DeviceInstance : IDisposable
    {
        private readonly SafeDevInst m_DevInst;

        #region Getting the Device Tree
        /// <summary>
        /// Gets a tree of all devices, starting from the root, for devices that are available in the system.
        /// </summary>
        /// <returns>A tree of <see cref="DeviceInstance"/> objects.</returns>
        /// <exception cref="PlatformNotSupportedException">This is only supported on Windows NT platforms.</exception>
        public static DeviceInstance GetRoot()
        {
            return GetRoot(LocateMode.Normal);
        }

        /// <summary>
        /// Gets a tree of all devices, starting from the root.
        /// </summary>
        /// <param name="mode">
        /// The mode, defining what devices to query. A value of <see cref="LocateMode.Normal"/> returns devices that
        /// are present. A value of <see cref="LocateMode.Phantom"/> provides devices that might not be attached at the
        /// current time.
        /// </param>
        /// <returns>A tree of <see cref="DeviceInstance"/> objects.</returns>
        /// <exception cref="PlatformNotSupportedException">This is only supported on Windows NT platforms.</exception>
        /// <exception cref="ArgumentException">Invalid mode.</exception>
        public static DeviceInstance GetRoot(LocateMode mode)
        {
            if (!Platform.IsWinNT())
                throw new PlatformNotSupportedException();

            Log.CfgMgr.TraceEvent(TraceEventType.Verbose, $"Getting device tree for {mode}");

            SafeDevInst devInst;
            CfgMgr32.CONFIGRET ret;
            switch (mode) {
            case LocateMode.Normal:
                ret = CfgMgr32.CM_Locate_DevNode(out devInst, null, CfgMgr32.CM_LOCATE_DEVINST.NORMAL);
                break;
            case LocateMode.Phantom:
                ret = CfgMgr32.CM_Locate_DevNode(out devInst, null, CfgMgr32.CM_LOCATE_DEVINST.PHANTOM);
                break;
            default:
                throw new ArgumentException("Invalid mode");
            }

            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                Log.CfgMgr.TraceEvent(TraceEventType.Error, $"Couldn't get root node for {mode}, return {ret}");
                return null;
            }

            DeviceInstance root = new DeviceInstance(devInst);
            root.PopulateChildren();
            return root;
        }

        private void PopulateChildren()
        {
            if (m_DevInst.IsInvalid)
                throw new ObjectDisposedException(nameof(DeviceInstance));
            if (m_IsPopulated || m_Children.Count > 0)
                throw new InvalidOperationException("Device Instance already populated with children");
            m_IsPopulated = true;

            CfgMgr32.CONFIGRET ret;

            ret = CfgMgr32.CM_Get_Child(out SafeDevInst child, m_DevInst, 0);
            if (ret == CfgMgr32.CONFIGRET.CR_NO_SUCH_DEVINST) return;
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                Log.CfgMgr.TraceEvent(TraceEventType.Warning, $"Couldn't get child node, return {ret}");
                return;
            }
            m_Children.Add(new DeviceInstance(child) {
                Parent = this
            });

            bool finished = false;
            SafeDevInst node = child;
            while (!finished) {
                ret = CfgMgr32.CM_Get_Sibling(out SafeDevInst sibling, node, 0);
                switch (ret) {
                case CfgMgr32.CONFIGRET.CR_SUCCESS:
                    m_Children.Add(new DeviceInstance(sibling) {
                        Parent = this
                    });
                    node = sibling;
                    break;
                default:
                    if (ret != CfgMgr32.CONFIGRET.CR_NO_SUCH_DEVINST)
                        Log.CfgMgr.TraceEvent(TraceEventType.Warning, $"Couldn't get sibling node, return {ret}");
                    finished = true;
                    break;
                }
            }

            // Now recurse into the new nodes and populate them also.
            foreach (DeviceInstance dev in m_Children) {
                dev.PopulateChildren();
            }
        }
        #endregion

        private DeviceInstance(SafeDevInst handle)
        {
            if (handle.IsInvalid || handle.IsClosed)
                throw new InvalidOperationException();

            m_DevInst = handle;
            m_Name = GetDeviceId(handle);
            GetStatus();

            m_DevDesc = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.DEVICEDESC);
            m_Service = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.SERVICE);
            m_Class = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.CLASS);
            m_ClassGuid = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.CLASSGUID);
            m_Driver = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.DRIVER);
            m_Manufacturer = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.MFG);
            m_FriendlyName = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.FRIENDLYNAME);
            m_Location = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.LOCATION_INFORMATION);
            m_PhysicalDevice = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.PHYSICAL_DEVICE_OBJECT_NAME);
            m_ConfigFlags = new DeviceProperty<int>(handle, CfgMgr32.CM_DRP.CONFIGFLAGS);
            m_Capabilities = new DeviceProperty<int>(handle, CfgMgr32.CM_DRP.CAPABILITIES);
            m_HardwareIds = new DeviceProperty<string[]>(handle, CfgMgr32.CM_DRP.HARDWAREID);
            m_CompatibleIds = new DeviceProperty<string[]>(handle, CfgMgr32.CM_DRP.COMPATIBLEIDS);
            m_UpperFilters = new DeviceProperty<string[]>(handle, CfgMgr32.CM_DRP.UPPERFILTERS);
            m_LowerFilters = new DeviceProperty<string[]>(handle, CfgMgr32.CM_DRP.LOWERFILTERS);
            m_LocationPaths = new DeviceProperty<string[]>(handle, CfgMgr32.CM_DRP.LOCATION_PATHS);
            m_BaseContainerId = new DeviceProperty<string>(handle, CfgMgr32.CM_DRP.BASE_CONTAINERID);
        }

        private static string GetDeviceId(SafeDevInst devInst)
        {
            CfgMgr32.CONFIGRET ret = CfgMgr32.CM_Get_Device_ID_Size(out int length, devInst, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                Log.CfgMgr.TraceEvent(TraceEventType.Error, $"Couldn't get device identifier length, return {ret}");
                return null;
            }

            StringBuilder buffer = new StringBuilder(length + 1);
            ret = CfgMgr32.CM_Get_Device_ID(devInst, buffer, length, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                Log.CfgMgr.TraceEvent(TraceEventType.Error, $"Couldn't get device identifier for length {length}, return {ret}");
                return null;
            }

            return buffer.ToString(0, length);
        }

        private void GetStatus()
        {
            CfgMgr32.CONFIGRET ret = CfgMgr32.CM_Get_DevNode_Status(out CfgMgr32.DN_STATUS status, out int problem, m_DevInst, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                Log.CfgMgr.TraceEvent(TraceEventType.Warning, $"Couldn't get status for {m_Name}, return {ret}");
                return;
            }

            Status = DeviceStatusConvert.Get(status);
            HasProblem = (status & CfgMgr32.DN_STATUS.HAS_PROBLEM) != 0;
            ProblemCode = (DeviceProblem)problem;
        }

        /// <summary>
        /// Gets the parent device instance.
        /// </summary>
        /// <value>The parent device instance.</value>
        public DeviceInstance Parent { get; private set; }

        private bool m_IsPopulated;
        private readonly List<DeviceInstance> m_Children = new List<DeviceInstance>();

        /// <summary>
        /// Get the children device instances from this node.
        /// </summary>
#if NET45_OR_GREATER || NETSTANDARD
        public IReadOnlyList<DeviceInstance> Children
        {
            get { return m_Children; }
        }
#else
        public IList<DeviceInstance> Children
        {
            get { return new ReadOnlyList<DeviceInstance>(m_Children); }
        }
#endif

        /// <summary>
        /// Gets the status of the device at the time it was enumerated.
        /// </summary>
        /// <value>The status of the device at the time it was enumerated.</value>
        public DeviceStatus Status { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this instance has problem.
        /// </summary>
        /// <value>
        /// Returns <see langword="true"/> if this instance has problem; otherwise, <see langword="false"/>.
        /// </value>
        public bool HasProblem { get; private set; }

        /// <summary>
        /// Gets the problem code for this driver if it has a problem.
        /// </summary>
        /// <value>The problem code for this driver.</value>
        public DeviceProblem ProblemCode { get; private set; }

        private readonly DeviceProperty<string> m_DevDesc;

        /// <summary>
        /// Gets the device description.
        /// </summary>
        /// <value>The device description.</value>
        public string DeviceDescription { get { return m_DevDesc.Value; } }

        private readonly DeviceProperty<string> m_Service;

        /// <summary>
        /// Gets the name of the service.
        /// </summary>
        /// <value>The name of the service.</value>
        public string Service { get { return m_Service.Value; } }

        private readonly DeviceProperty<string> m_Class;

        /// <summary>
        /// Gets the class name of the device.
        /// </summary>
        /// <value>The class name of the device.</value>
        public string Class { get { return m_Class.Value; } }

        private readonly DeviceProperty<string> m_ClassGuid;

        /// <summary>
        /// Gets the class unique identifier of the device.
        /// </summary>
        /// <value>The class unique identifier of the device.</value>
        public string ClassGuid { get { return m_ClassGuid.Value; } }

        private readonly DeviceProperty<string> m_Driver;

        /// <summary>
        /// Gets the driver.
        /// </summary>
        /// <value>The driver.</value>
        public string Driver { get { return m_Driver.Value; } }

        private readonly DeviceProperty<string> m_Manufacturer;

        /// <summary>
        /// Gets the manufacturer of the device or driver.
        /// </summary>
        /// <value>The manufacturer of the device or driver.</value>
        public string Manufacturer { get { return m_Manufacturer.Value; } }

        private readonly DeviceProperty<string> m_FriendlyName;

        /// <summary>
        /// Gets the friendly name of the device.
        /// </summary>
        /// <value>The friendly name of the device.</value>
        public string FriendlyName { get { return m_FriendlyName.Value; } }

        private readonly DeviceProperty<string> m_Location;

        /// <summary>
        /// Gets the location of the device.
        /// </summary>
        /// <value>The location of the device.</value>
        public string Location { get { return m_Location.Value; } }

        private readonly DeviceProperty<string> m_PhysicalDevice;

        /// <summary>
        /// Gets the physical device name.
        /// </summary>
        /// <value>The physical device name.</value>
        public string PhysicalDevice { get { return m_PhysicalDevice.Value; } }

        private readonly DeviceProperty<int> m_ConfigFlags;

        /// <summary>
        /// Gets the configuration flags for the device.
        /// </summary>
        /// <value>The configuration flags for the device.</value>
        public int ConfigFlags { get { return m_ConfigFlags.Value; } }

        private readonly DeviceProperty<int> m_Capabilities;

        /// <summary>
        /// Gets the capability flags of the device.
        /// </summary>
        /// <value>The capabilities of the device.</value>
        public DeviceCapabilities Capabilities { get { return (DeviceCapabilities)m_Capabilities.Value; } }

        private readonly DeviceProperty<string[]> m_HardwareIds;

        /// <summary>
        /// Gets the hardware ids.
        /// </summary>
        /// <value>The hardware ids.</value>
#if NET45_OR_GREATER || NETSTANDARD
        public IReadOnlyList<string> HardwareIds { get { return m_HardwareIds.Value; } }
#else
        public IList<string> HardwareIds { get { return m_HardwareIds.Value; } }
#endif

        private readonly DeviceProperty<string[]> m_CompatibleIds;

        /// <summary>
        /// Gets the compatible ids.
        /// </summary>
        /// <value>The compatible ids.</value>
#if NET45_OR_GREATER || NETSTANDARD
        public IReadOnlyList<string> CompatibleIds { get { return m_CompatibleIds.Value; } }
#else
        public IList<string> CompatibleIds { get { return m_CompatibleIds.Value; } }
#endif

        private readonly DeviceProperty<string[]> m_UpperFilters;

        /// <summary>
        /// Gets the compatible ids.
        /// </summary>
        /// <value>The compatible ids.</value>
#if NET45_OR_GREATER || NETSTANDARD
        public IReadOnlyList<string> UpperFilters { get { return m_UpperFilters.Value; } }
#else
        public IList<string> UpperFilters { get { return m_UpperFilters.Value; } }
#endif

        private readonly DeviceProperty<string[]> m_LowerFilters;

        /// <summary>
        /// Gets the compatible ids.
        /// </summary>
        /// <value>The compatible ids.</value>
#if NET45_OR_GREATER || NETSTANDARD
        public IReadOnlyList<string> LowerFilters { get { return m_LowerFilters.Value; } }
#else
        public IList<string> LowerFilters { get { return m_LowerFilters.Value; } }
#endif

        private readonly DeviceProperty<string[]> m_LocationPaths;

        /// <summary>
        /// Gets the compatible ids (Windows 2003 and later).
        /// </summary>
        /// <value>The compatible ids.</value>
#if NET45_OR_GREATER || NETSTANDARD
        public IReadOnlyList<string> LocationPaths { get { return m_LocationPaths.Value; } }
#else
        public IList<string> LocationPaths { get { return m_LocationPaths.Value; } }
#endif

        private readonly DeviceProperty<string> m_BaseContainerId;

        /// <summary>
        /// Gets the base container identifier (Windows 7).
        /// </summary>
        /// <value>The location of the device.</value>
        public string BaseContainerId { get { return m_BaseContainerId.Value; } }

        /// <summary>
        /// Gets the names of the keys for the device from the registry.
        /// </summary>
        /// <returns>
        /// An array of all the keys for the device from the registry. This can be then used with
        /// <see cref="GetDeviceProperty{T}(string, T)"/>.
        /// </returns>
        public string[] GetDeviceProperties()
        {
            using (RegistryKey driverKey = GetDeviceKey()) {
                if (driverKey == null) {
#if NETSTANDARD
                    return Array.Empty<string>();
#else
                    return new string[0];
#endif
                }
                return driverKey.GetValueNames();
            }
        }

        /// <summary>
        /// Gets the device property.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>The result. If the key doesn't exist, <see langword="null"/> is returned.</returns>
        public object GetDeviceProperty(string keyName)
        {
            return GetDeviceProperty(keyName, null);
        }

        /// <summary>
        /// Gets the device property.
        /// </summary>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defValue">The value to return if the key doesn't exist.</param>
        /// <returns>The result. If the key doesn't exist, the default value is returned.</returns>
        public object GetDeviceProperty(string keyName, object defValue)
        {
            using (RegistryKey driverKey = GetDeviceKey()) {
                return driverKey == null ?
                    defValue :
                    driverKey.GetValue(keyName);
            }
        }

        /// <summary>
        /// Gets the device property.
        /// </summary>
        /// <typeparam name="T">The type to convert the result to.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <returns>The result. If the key doesn't exist, the default value is returned.</returns>
        public T GetDeviceProperty<T>(string keyName)
        {
            return GetDeviceProperty<T>(keyName, default);
        }

        /// <summary>
        /// Gets the device property.
        /// </summary>
        /// <typeparam name="T">The type to convert the result to.</typeparam>
        /// <param name="keyName">Name of the key.</param>
        /// <param name="defValue">The value to return if the key doesn't exist.</param>
        /// <returns>The result. If the key doesn't exist, the default value is returned.</returns>
        public T GetDeviceProperty<T>(string keyName, T defValue)
        {
            using (RegistryKey driverKey = GetDeviceKey()) {
                return driverKey == null ?
                    defValue :
                    (T)driverKey.GetValue(keyName);
            }
        }

        private RegistryKey GetDeviceKey()
        {
            CfgMgr32.CONFIGRET ret = CfgMgr32.CM_Open_DevNode_Key(
                m_DevInst, Kernel32.REGSAM.KEY_READ, 0, CfgMgr32.RegDisposition.OpenExisting,
                out SafeRegistryHandle key, 0);
            if (ret != CfgMgr32.CONFIGRET.CR_SUCCESS) {
                if (ret != CfgMgr32.CONFIGRET.CR_NO_SUCH_REGISTRY_KEY) {
                    Log.CfgMgr.TraceEvent(TraceEventType.Warning,
                        $"Couldn't get device key, return {ret}");
                }
                return null;
            }
            if (key.IsInvalid || key.IsClosed) {
                Log.CfgMgr.TraceEvent(TraceEventType.Error,
                    $"Couldn't get device key, registry is invalid or closed");
                return null;
            }

            return RegistryKey.FromHandle(key);
        }

        private readonly string m_Name;

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="string" /> that represents this instance.</returns>
        public override string ToString()
        {
            return m_Name;
        }

        // Properties we will record
        // * Properties, these will be fixed types as per the CfgMgr32.h file
        // * Properties, which we get from the registry

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting managed and unmanaged
        /// resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release
        /// only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                m_DevInst.Close();

                // Iterate through the tree and dispose them too.
                foreach (DeviceInstance devInst in m_Children) {
                    devInst.Dispose();
                }
            }
        }
    }
}
