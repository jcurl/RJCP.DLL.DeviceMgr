namespace RJCP.IO.DeviceMgr
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.Versioning;
    using System.Text;
    using NUnit.Framework;

    [TestFixture]
    [Platform("Win")]
    [SupportedOSPlatform("windows")]
    public class DeviceInstanceTest
    {
        [Test]
        public void GetDeviceTree()
        {
            DeviceInstance devices = DeviceInstance.GetRoot();
            devices.Refresh();
            Assert.That(devices, Is.Not.Null);
            Assert.That(devices.Children, Is.Not.Empty);
            CheckDeviceTree(devices);
            CheckDevicesPresent(devices);
        }

        private static void CheckDeviceTree(DeviceInstance devices)
        {
            // Check the parent / child relationship
            Queue<DeviceInstance> queue = new();
            queue.Enqueue(devices);
            while (queue.Count > 0) {
                DeviceInstance parent = queue.Dequeue();
                foreach (DeviceInstance child in parent.Children) {
                    Assert.That(ReferenceEquals(parent, child.Parent));
                    if (child.Children.Count > 0) queue.Enqueue(child);
                }
            }
        }

        private static void CheckDevicesPresent(DeviceInstance devices)
        {
            // Check the parent / child relationship
            Queue<DeviceInstance> queue = new();
            queue.Enqueue(devices);
            while (queue.Count > 0) {
                DeviceInstance node = queue.Dequeue();
                Assert.That(node.ProblemCode, Is.Not.EqualTo(DeviceProblem.DeviceNotThere),
                    $"{node} in list - {node.ProblemCode}");
                foreach (DeviceInstance child in node.Children) {
                    queue.Enqueue(child);
                }
            }
        }

        [Test]
        public void GetDeviceTreeTwice()
        {
            GetDeviceTree();
            GetDeviceTree();
        }

        [Test]
        public void GetDeviceTreeRefresh()
        {
            HashSet<string> foundBefore = new();
            HashSet<string> foundAfter = new();

            DeviceInstance devices = DeviceInstance.GetRoot();
            devices.Refresh();
            Assert.That(devices, Is.Not.Null);
            Assert.That(devices.Children, Is.Not.Empty);

            Queue<DeviceInstance> queue = new();
            queue.Enqueue(devices);
            while (queue.Count > 0) {
                DeviceInstance node = queue.Dequeue();
                foundBefore.Add(node.ToString());
                foreach (DeviceInstance child in node.Children) {
                    queue.Enqueue(child);
                }
            }

            queue.Clear();
            devices.Refresh();
            queue.Enqueue(devices);
            while (queue.Count > 0) {
                DeviceInstance node = queue.Dequeue();
                foundAfter.Add(node.ToString());
                foreach (DeviceInstance child in node.Children) {
                    queue.Enqueue(child);
                }
            }

            // Compare before and after. There should only be a difference if a device was added or removed.
            int additions = 0;
            int removals = 0;
            foreach (string node in foundBefore) {
                if (!foundAfter.Contains(node)) {
                    removals++;
                    Console.WriteLine($"Node Removed: {node}");
                }
            }
            foreach (string node in foundAfter) {
                if (!foundBefore.Contains(node)) {
                    additions++;
                    Console.WriteLine($"Node Added: {node}");
                }
            }

            Assert.Multiple(() => {
                Assert.That(removals, Is.EqualTo(0));
                Assert.That(additions, Is.EqualTo(0));
            });
        }

        [Test]
        public void DumpDeviceTree()
        {
            DeviceInstance devices = DeviceInstance.GetRoot();
            devices.Refresh();
            Assert.That(devices, Is.Not.Null);
            Assert.That(devices.Children, Is.Not.Empty);

            DumpDeviceTree(devices, 0, true);
        }

        private static void DumpDeviceTree(DeviceInstance device, int depth, bool extended)
        {
            DumpDeviceNode(device, depth, extended);
            foreach (DeviceInstance child in device.Children) {
                DumpDeviceTree(child, depth + 1, extended);
            }
        }

        private static void DumpDeviceNode(DeviceInstance device, int depth, bool extended)
        {
            PrintIndent(depth); Console.WriteLine($"+ {device}");
            PrintIndent(depth); Console.WriteLine($"  - Friendly Name: {device.FriendlyName}");
            PrintIndent(depth); Console.WriteLine($"  - ProbCode: {device.ProblemCode}");
            if (extended) {
                PrintIndent(depth); Console.WriteLine($"  - Status: {device.Status}");
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
                PrintIndent(depth); Console.WriteLine($"  - Keys: {List(device.GetDeviceProperties())}");

                string port = device.GetDeviceProperty<string>("PortName");
                if (port is not null) {
                    PrintIndent(depth); Console.WriteLine($"  - PortName: {port}");
                }
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
            if (list is null) { return string.Empty; }

            StringBuilder sb = new();
            bool first = true;
            foreach (string entry in list) {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(entry);
            }
            return sb.ToString();
        }

        [Test]
        public void GetDeviceList()
        {
            IList<DeviceInstance> list = DeviceInstance.GetList();
            Assert.That(list, Is.Not.Empty);

            foreach (DeviceInstance entry in list) {
                CheckDeviceTree(entry);
            }
        }

        [Test]
        public void GetDeviceListNormal()
        {
            IList<DeviceInstance> list = DeviceInstance.GetList(LocateMode.Normal);
            Assert.That(list, Is.Not.Empty);

            foreach (DeviceInstance entry in list) {
                CheckDeviceTree(entry);
                DumpDeviceNode(entry, 0, false);
            }
        }

        [Test]
        public void DumpDeviceList()
        {
            IList<DeviceInstance> list = DeviceInstance.GetList();
            foreach (DeviceInstance dev in list) {
                Assert.That(dev, Is.Not.Null);
                DumpDeviceNode(dev, 0, false);
            }
        }

        [Test]
        public void GetDeviceListDumpRoot()
        {
            // Getting the list returns all nodes, including those not connected. Then getting the root will just return
            // the tree build from GetList().
            //
            // If you don't want the cached list, you should call root.Refresh(), which then returns only the entries
            // that are actually connected.
            DeviceInstance.GetList();
            DeviceInstance root = DeviceInstance.GetRoot();
            DumpDeviceTree(root, 0, false);
        }

        [Test]
        public void GetDeviceListRefreshNoMissing()
        {
            DeviceInstance.GetList();
            DeviceInstance root = DeviceInstance.GetRoot();
            root.Refresh();
            DumpDeviceTree(root, 0, false);
            CheckDevicesPresent(root);
        }
    }
}
