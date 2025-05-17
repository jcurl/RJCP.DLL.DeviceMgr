# RJCP.DeviceMgr <!-- omit in toc -->

This library implements mechanisms to enumerate over the devices that are
present in your Windows System. It uses Win32 API from `CfgMgr32.dll` which is
available since Windows 2000. This code is highly compatible and is tested with
Windows XP to Windows 11.

The current implementation provides *readonly* information. It is not designed
that you can eject or modify the devices in the system.

It is called `RJCP.IO.DeviceMgr`, as it provides information very similar to
when you manage your devices from the Windows Desktop and view the details of
individual devices.

- [1. Testing](#1-testing)
- [2. Using in Your Own Software](#2-using-in-your-own-software)
- [3. Implementation Details](#3-implementation-details)
  - [3.1. Win32 APIs used](#31-win32-apis-used)
    - [3.1.1. Getting a List of Devices](#311-getting-a-list-of-devices)
    - [3.1.2. Refreshing Devices](#312-refreshing-devices)
- [4. Known Issues](#4-known-issues)
  - [4.1. Incomplete Set of Devices for GetList()](#41-incomplete-set-of-devices-for-getlist)
  - [4.2. No Phantom Devices with GetRoot().Refresh()](#42-no-phantom-devices-with-getrootrefresh)
- [5. Release History](#5-release-history)
  - [5.1. Version 0.8.3](#51-version-083)
  - [5.2. Version 0.8.2](#52-version-082)
  - [5.3. Version 0.8.1](#53-version-081)
  - [5.4. Version 0.8.0](#54-version-080)

## 1. Testing

To quickly test the usage of the library, run the executable `DeviceInfoDump`.
This gets the root tree node and dumps all information about the device tree to
the console. This gives you an idea of the information that can be obtained.

## 2. Using in Your Own Software

Import the library into your project.

To set up logging, to see any errors or warnings, see
[Logging.md](docs/Logging.md), and the example program `DeviceInfoDump` which is
a minimal program for dumping information.

## 3. Implementation Details

### 3.1. Win32 APIs used

This implementation uses `CfgMgr32.dll` as described in the introduction. The
APIs used are expected to be available for Windows XP and later.

#### 3.1.1. Getting a List of Devices

Getting the list of all devices in the system uses the
[CM_Get_Device_ID_List](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_device_id_listw)
API, which returns a list of all devices in the system, including those that are
"phantom". No filter is applied when getting the list.

From the list of devices,
[CM_Locate_DevNode](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_locate_devnodew)
is used to get information about each device from the list returned, either
"Normal" or "Phantom" devices. A tree of all devices is built after querying
each device and getting information about the parent device in the tree.

The technique is described at
[MSDN](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/enumerating-installed-devices)

> Using configuration manager functions:
>
> 1. Use `CM_Get_Device_ID_List` to retrieve a list of unique device instance
>    identifier (ID) strings. To retrieve information only for devices that are
>    present in the system, set `CM_GETIDLIST_FILTER_PRESENT` in the `ulFlags`
>    parameter.
>
> 2. You can use the unique device instance ID with `CM_Locate_DevNode` to
>    retrieve a `DEVINST` that represents the device to use with other
>    configuration manager APIs.

#### 3.1.2. Refreshing Devices

When refreshing devices, a different set of APIs are used. The root node is
obtained with
[CM_Locate_DevNode](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_locate_devnodew),
and then the tree is directly queried with
[CM_Get_Child](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_child)
and
[CM_Get_Sibling](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_sibling).
These APIs only return devices that are active in the device tree.

## 4. Known Issues

### 4.1. Incomplete Set of Devices for GetList()

On some specific systems, the devices returned by `DeviceInstance.GetList()` is
incomplete. Debugging shows that the list of strings returned by
[CM_Get_Device_ID_List](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_device_id_listw)
is incomplete.

Calling a subsequent `DeviceInstance.GetRoot().Refresh()` removes any phantom
devices, and can include extra devices that were previously not present, despite
devices not being physically added by the user.

A workaround is to use `DeviceInstance.GetRoot()` instead of
`DeviceInstance.GetList()` if no phantom devices are needed.

It is not intended to try and workaround this problem in software (by calling
`CM_Get_Device_ID_List` and then enumerating the devices with `CM_Get_Child` and
`CM_Get_Sibling`, trying to remember the phantom devices) due to potential race
conditions.

This problem was observed in the case that an upgrade from Windows 10 to Windows
11 with `SWD\PRINTENUM\{0C67E0C9-90A0-40EB-B1A6-470E41AB1CB7}` and
`SWD\PRINTENUM\{C9730CC1-28FB-4572-A864-D9546A92F674}` which have the friendly
names `OneNote` and `OneNote for Windows 10`.

This was confirmed independently in a test program in C. See in
`docs/SampleCode`.

### 4.2. No Phantom Devices with GetRoot().Refresh()

When using the API
[CM_Locate_DevNode](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_locate_devnodew)
to get the root node (even specifying `CM_LOCATE_DEVNODE_PHANTOM` on the root
node), subsequent calls to
[CM_Get_Child](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_child)
and
[CM_Get_Sibling](https://learn.microsoft.com/en-us/windows/win32/api/cfgmgr32/nf-cfgmgr32-cm_get_sibling)
will not return phantom devices.

## 5. Release History

### 5.1. Version 0.8.3

Bug Fix:

- Don't refresh multiple times (DOTNET-1031)
- Fix refresh on device insertion (DOTNET-1033)

Quality:

- Clear cache on getting the list (DOTNET-1032)
- Don't use `SafeDevInst` and use `IntPtr` as there's nothing to free
  (DOTNET-1036)

### 5.2. Version 0.8.2

Do not use. Refresh doesn't work properly.

Quality:

- Reduce the amount of allocations on the heap (DOTNET-1021,
  [#1](https://github.com/jcurl/RJCP.DLL.DeviceMgr/issues/1))

### 5.3. Version 0.8.1

Quality:

- Add README.md to NuGet Package (DOTNET-813)
- Update from .NET 4.5 to .NET 4.6.2 (DOTNET-827)
- Update from .NET Standard 2.1 to .NET 6.0 (DOTNET-936, DOTNET-937, DOTNET-938,
  DOTNET-942, DOTNET-945)
- Update to .NET 8.0 (DOTNET-982, DOTNET-983, DOTNET-989, DOTNET-990)

### 5.4. Version 0.8.0

- Initial Release
