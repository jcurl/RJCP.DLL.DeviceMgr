namespace RJCP.DeviceInfoDump
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using RJCP.IO.DeviceMgr;

    public static class Program
    {
        public static int Main()
        {
            GlobalLogger.Initialize();

            DeviceInstance.GetList();
            DeviceInstance devices = DeviceInstance.GetRoot();
            DumpDeviceTree(devices, 0);
            return 0;
        }

        private static void DumpDeviceTree(DeviceInstance device, int depth)
        {
            DumpDeviceNode(device, depth);
            foreach (DeviceInstance child in device.Children) {
                DumpDeviceTree(child, depth + 1);
            }
        }

        private static void DumpDeviceNode(DeviceInstance device, int depth)
        {
            PrintIndent(depth); Console.WriteLine($"+ {device}");
            PrintIndent(depth); Console.WriteLine($"  - Status: {device.Status}");
            PrintIndent(depth); Console.WriteLine($"  - ProbCode: {device.ProblemCode}");
            PrintIndent(depth); Console.WriteLine($"  - Friendly Name: {device.FriendlyName}");
            PrintIndent(depth); Console.WriteLine($"  - Description: {device.DeviceDescription}");
            PrintIndent(depth); Console.WriteLine($"  - Service: {device.Service}");
            PrintIndent(depth); Console.WriteLine($"  - Manufacturer: {device.Manufacturer}");
            PrintIndent(depth); Console.WriteLine($"  - Class: {device.Class}");
            PrintIndent(depth); Console.WriteLine($"  - Class GUID: {device.ClassGuid}");
            PrintIndent(depth); Console.WriteLine($"  - Driver: {device.Driver}");
            PrintIndent(depth); Console.WriteLine($"  - Location: {device.Location}");
            PrintIndent(depth); Console.WriteLine($"  - Location Paths: {List(device.LocationPaths)}");
            PrintIndent(depth); Console.WriteLine($"  - Device: {device.PhysicalDevice}");
            PrintIndent(depth); Console.WriteLine($"  - Config Flags: 0x{device.ConfigFlags:x8}");
            PrintIndent(depth); Console.WriteLine($"  - Capabilities: {device.Capabilities}");
            PrintIndent(depth); Console.WriteLine($"  - Hardware IDs: {List(device.HardwareIds)}");
            PrintIndent(depth); Console.WriteLine($"  - Compatible IDs: {List(device.CompatibleIds)}");
            PrintIndent(depth); Console.WriteLine($"  - Upper Filters: {List(device.UpperFilters)}");
            PrintIndent(depth); Console.WriteLine($"  - Lower Filters: {List(device.LowerFilters)}");
            PrintIndent(depth); Console.WriteLine($"  - Base Container ID: {device.BaseContainerId}");

            string[] keys = device.GetDeviceProperties();
            PrintIndent(depth); Console.WriteLine($"  - Keys: {List(keys)}");
            foreach (string key in keys) {
                PrintIndent(depth); Console.WriteLine($"  \\- Key: {key}={device.GetDeviceProperty(key)}");
            }
        }

        private static void PrintIndent(int depth)
        {
            if (depth > 0) {
                for (int i = 0; i < depth; i++) {
                    Console.Write("  ");
                }
            }
        }

        private static string List(IEnumerable<string> list)
        {
            if (list == null) { return string.Empty; }

            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (string entry in list) {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(entry);
            }
            return sb.ToString();
        }
    }
}
