namespace RJCP.IO.DeviceMgr
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.Versioning;
    using System.Text;
    using System.Windows.Forms;
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

            // After refreshing, there shouldn't be any phantom devices.
            CheckDevicesPresent(devices);
        }

        private static int CheckDeviceTree(DeviceInstance devices)
        {
            if (devices is null) return 0;

            int nodes = 1;

            // Check the parent / child relationship
            Queue<DeviceInstance> queue = new();
            queue.Enqueue(devices);
            while (queue.Count > 0) {
                DeviceInstance parent = queue.Dequeue();
                foreach (DeviceInstance child in parent.Children) {
                    nodes++;
                    Assert.That(parent, Is.EqualTo(child.Parent));
                    if (child.Children.Count > 0) queue.Enqueue(child);
                }
            }

            return nodes;
        }

        private static void CheckDevicesPresent(DeviceInstance devices)
        {
            int devicesMissing = 0;

            // Check that every element in the tree is actually present.
            Queue<DeviceInstance> queue = new();
            queue.Enqueue(devices);
            while (queue.Count > 0) {
                DeviceInstance node = queue.Dequeue();
                if (node.ProblemCode == DeviceProblem.DeviceNotThere) {
                    Console.WriteLine($"{node} in list - {node.ProblemCode}");
                    devicesMissing++;
                }
                foreach (DeviceInstance child in node.Children) {
                    queue.Enqueue(child);
                }
            }
            Assert.That(devicesMissing, Is.Zero);
        }

        [Test]
        public void GetDeviceTreeTwice()
        {
            GetDeviceTree();
            GetDeviceTree();
        }

        private static HashSet<string> GetDevices()
        {
            return GetDevices(null);
        }

        private static HashSet<string> GetDevices(DeviceInstance root)
        {
            DeviceInstance devices = root ?? DeviceInstance.GetRoot();
            devices.Refresh();
            DumpDeviceTree(devices, true);
            Assert.That(devices, Is.Not.Null);
            Assert.That(devices.Children, Is.Not.Empty);

            HashSet<string> deviceList = new();

            Queue<DeviceInstance> queue = new();
            queue.Enqueue(devices);
            while (queue.Count > 0) {
                DeviceInstance node = queue.Dequeue();
                deviceList.Add(node.ToString());
                foreach (DeviceInstance child in node.Children) {
                    queue.Enqueue(child);
                }
            }

            return deviceList;
        }

        private static void Compare(HashSet<string> before, HashSet<string> after, out int removed, out int inserted)
        {
            removed = 0;
            inserted = 0;

            foreach (string node in before) {
                if (!after.Contains(node)) {
                    removed++;
                    Console.WriteLine($"Node Removed: {node}");
                }
            }
            foreach (string node in after) {
                if (!before.Contains(node)) {
                    inserted++;
                    Console.WriteLine($"Node Inserted: {node}");
                }
            }
        }

        [Test]
        public void GetDeviceTreeRefresh()
        {
            HashSet<string> foundBefore = GetDevices();
            HashSet<string> foundAfter = GetDevices();
            Compare(foundBefore, foundAfter, out int removals, out int additions);

            // Compare before and after. There should only be a difference if a device was added or removed.
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

            DumpDeviceTree(devices, true);
        }

        private static void DumpDeviceTree(DeviceInstance device, bool extended)
        {
            DumpDeviceTree(device, 0, extended);
        }

        private static void DumpDeviceTree(DeviceInstance device, int depth, bool extended)
        {
            DumpDeviceNode(device, depth, extended);
            foreach (DeviceInstance child in device.Children) {
                DumpDeviceTree(child, depth + 1, extended);
            }
        }

        private static void DumpDeviceNode(DeviceInstance device, bool extended)
        {
            DumpDeviceNode(device, 0, extended);
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

        [TestCase(LocateMode.Phantom)]
        [TestCase(LocateMode.Normal)]
        public void GetDeviceList(LocateMode mode)
        {
            IList<DeviceInstance> list = DeviceInstance.GetList(mode);
            Assert.That(list, Is.Not.Empty);

            foreach (DeviceInstance entry in list) {
                Assert.That(CheckDeviceTree(entry), Is.Not.Zero);
                DumpDeviceNode(entry, false);
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
            DumpDeviceTree(root, false);
        }

        [TestCase(LocateMode.Phantom)]
        [TestCase(LocateMode.Normal)]
        public void GetDeviceListRefreshNoMissing(LocateMode mode)
        {
            DeviceInstance.GetList(mode);
            DeviceInstance root = DeviceInstance.GetRoot();
            root.Refresh();
            DumpDeviceTree(root, false);
            CheckDevicesPresent(root);
        }

        [Test]
        [Explicit("Performance Test")]
        [Category("ManualTest")]
        public void RefreshFrequent()
        {
            // Useful for testing garbage collection with profilers.

            for (int count = 0; count < 10000; count++) {
                DeviceInstance.GetRoot().Refresh();
            }
        }

        [Test]
        [Explicit("Performance Test")]
        [Category("ManualTest")]
        public void RefreshFrequent2()
        {
            // Useful for testing garbage collection with profilers.

            List<DeviceInstance> instances = new();
            foreach (var device in DeviceInstance.GetList()) {
                instances.Add(device);
            }

            for (int count = 0; count < 10000; count++) {
                foreach (var instance in instances) {
                    instance.Refresh();
                }
            }
        }

        [TestCase(LocateMode.Phantom)]
        [TestCase(LocateMode.Normal)]
#if !DEBUG
        [Explicit("Interactive Manual Test")]
        [Category("ManualTest")]
#endif
        public void RefreshCount(LocateMode mode)
        {
            // This test case is set for DEBUG only. On some systems it's seen that the call to CM_Get_Device_ID_List()
            // called from GetList() returns only a partial list, and then refreshing that with GetRoot().Refresh() will
            // return additional devices (which uses CM_Get_Child() and CM_Get_Sibling()). However, the Refresh()
            // doesn't have a mechanism to return the phantom elements. Running as Administrator doesn't help.
            //
            // Tested on Windows 11 (after migrating from Windows 10). Assuming a bug in Windows 11.

            int list = DeviceInstance.GetList(mode).Count;
            int treecount = CheckDeviceTree(DeviceInstance.GetRoot());
            DeviceInstance.GetRoot().Refresh();
            int treecount_refreshed = CheckDeviceTree(DeviceInstance.GetRoot());

            Console.WriteLine("Found {0} devices", list);
            Assert.That(treecount, Is.EqualTo(list), $"Complete list {list} isn't the same as counting the tree {treecount}");
            switch (mode) {
            case LocateMode.Normal:
                Assert.That(treecount_refreshed, Is.EqualTo(list), $"Complete list {list} isn't the same as counting the tree {treecount_refreshed} after refresh");
                break;
            case LocateMode.Phantom:
                Assert.That(treecount_refreshed, Is.LessThanOrEqualTo(list), $"Complete list {list} smaller than counting the tree {treecount_refreshed} after refresh");
                break;
            default:
                Assert.Fail("Unknown case - test case error");
                break;
            }
        }

        [Test]
        [Explicit("Interactive Manual Test")]
        [Category("ManualTest")]
        public void RefreshDeviceChanged()
        {
            DeviceInstance.GetList(LocateMode.Normal);
            HashSet<string> orig_set = GetDevices();
            DeviceInstance root = DeviceInstance.GetRoot();
            int treecount = CheckDeviceTree(root);
            DumpDeviceTree(root, false);

            // To test, run manually. Then it pauses for 30 seconds. During this time, insert or remove a device (e.g. a
            // USB device) and check that the results show the count as increased or decreased depending on if it was
            // added or removed.
            Console.WriteLine("Please remove a device (e.g. a USB device)");
            MessageBox.Show("Please remove a device (e.g. a USB device)\nThen press OK.", nameof(RefreshDeviceChanged));
            HashSet<string> remove_set = GetDevices();
            int treecount_removed = CheckDeviceTree(DeviceInstance.GetRoot());
            DumpDeviceTree(DeviceInstance.GetRoot(), false);
            Compare(orig_set, remove_set, out int _, out int _);

            Console.WriteLine("Please insert a device (e.g. a USB device)");
            MessageBox.Show("Please insert a device (e.g. a USB device)\nThen press OK.", nameof(RefreshDeviceChanged));
            HashSet<string> insert_set = GetDevices();
            int treecount_inserted = CheckDeviceTree(DeviceInstance.GetRoot());
            DumpDeviceTree(DeviceInstance.GetRoot(), false);
            Compare(remove_set, insert_set, out int _, out int _);

            Console.WriteLine("before {0} after remove {1} then after insert {2}", treecount, treecount_removed, treecount_inserted);

            Assert.That(treecount_removed, Is.LessThan(treecount));
            Assert.That(treecount_inserted, Is.GreaterThan(treecount_removed));
        }
    }
}
