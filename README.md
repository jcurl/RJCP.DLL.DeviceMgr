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
- [3. Release History](#3-release-history)
  - [Version 0.8.3](#version-083)
  - [Version 0.8.2](#version-082)
  - [Version 0.8.1](#version-081)
  - [Version 0.8.0](#version-080)

## 1. Testing

To quickly test the usage of the library, run the executable `DeviceInfoDump`.
This gets the root tree node and dumps all information about the device tree to
the console. This gives you an idea of the information that can be obtained.

## 2. Using in Your Own Software

Import the library into your project.

To set up logging, to see any errors or warnings, see
[Logging.md](docs/Logging.md), and the example program `DeviceInfoDump` which is
a minimal program for dumping information.

## 3. Release History

### Version 0.8.3

Bug Fix:

- Don't refresh multiple times (DOTNET-1031)
- Fix refresh on device insertion (DOTNET-1033)

Quality:

- Clear cache on getting the list (DOTNET-1032)
- Don't use `SafeDevInst` and use `IntPtr` as there's nothing to free
  (DOTNET-1036)

### Version 0.8.2

Do not use. Refresh doesn't work properly.

Quality:

- Reduce the amount of allocations on the heap (DOTNET-1021,
  [#1](https://github.com/jcurl/RJCP.DLL.DeviceMgr/issues/1))

### Version 0.8.1

Quality:

- Add README.md to NuGet Package (DOTNET-813)
- Update from .NET 4.5 to .NET 4.6.2 (DOTNET-827)
- Update from .NET Standard 2.1 to .NET 6.0 (DOTNET-936, DOTNET-937, DOTNET-938,
  DOTNET-942, DOTNET-945)
- Update to .NET 8.0 (DOTNET-982, DOTNET-983, DOTNET-989, DOTNET-990)

### Version 0.8.0

- Initial Release
