# Logging <!-- omit in toc -->

- [1. Logging with .NET Framework (.NET 4.0 until .NET 4.8.x)](#1-logging-with-net-framework-net-40-until-net-48x)
- [2. Logging with .NET Standard](#2-logging-with-net-standard)

When using this library in your own projects, you can enable logging to capture
errors and warnings while devices are being enumerated. This can help debug and
identify potential issues.

The string to use for logging is `RJCP.IO.DeviceMgr`. The logging is done via my
[Trace](https://github.com/jcurl/RJCP.DLL.Trace) library. Further information
can be found on integrating and enabling logging.

## 1. Logging with .NET Framework (.NET 4.0 until .NET 4.8.x)

Apply to your application config, something similar to:

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <system.diagnostics>
    <trace autoflush="false" indentsize="4"/>
    <sources>
      <source name="RJCP.IO.Device" switchValue="Verbose">
        <listeners>
          <add name="nunitListener"/>
          <remove name="Default"/>
        </listeners>
      </source>
    </sources>

    <sharedListeners>
      <add name="nunitListener" type="RJCP.CodeQuality.NUnitExtensions.Trace.NUnitTraceListener, RJCP.CodeQuality" />
    </sharedListeners>
  </system.diagnostics>
</configuration>
```

Replace the shared listener with your logging provider (such as a console, or
writing to a file).

## 2. Logging with .NET Standard

.NET standard needs more work. Using the `LogSource` class implementation,
logging is abstracted to capture .NET Core providers. For this, you will need to
modify your code to enable logging. For example, you can enable logging in code
with the following snippet:

```csharp
namespace RJCP.DeviceInfoDump
{
#if NETCOREAPP
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using RJCP.Diagnostics.Trace;
#endif

    public static class GlobalLogger
    {
#if NETCOREAPP
        static GlobalLogger()
        {
            LogSource.SetLoggerFactory(GetLoggerFactory());
        }

        private static ILoggerFactory GetLoggerFactory()
        {
            // Should be something similar to dltdump.dll.json
            string file = typeof(Program).Assembly.Location;
            string app = Path.GetFileName(file);

            IConfigurationRoot config = new ConfigurationBuilder()
                .AddJsonFile($"{app}.json", true, false)
                .Build();

            return LoggerFactory.Create(builder => {
                builder
                    .AddConfiguration(config.GetSection("Logging"))
                    .AddConsole();
            });
        }
#endif

        // Just calling this method will result in the static constructor being executed.
        public static void Initialize()
        {
            /* Can be empty, reference will initialize static constructor */
        }
    }
}
```

Note, it is written in such a way that the same file can be used for .NET
Framework (.NET 4.x) and .NET Core, with the older .NET Framework effectively
doing nothing.

You would replace `.AddConsole()` with your own logging provider.

The instruction `.AddJsonFile($"{app}.json", true, false)` needs to have the
JSON file copied to the same location as the DLL. See the
`DeviceInfoDump.csproj` file on how this is done.

```xml
  <Target Name="CopyAppConfig" AfterTargets="AfterBuild" Condition="Exists('appsettings.json') and '$(TargetFrameworkIdentifier)' == '.NETCoreApp'">
    <Delete Files="$(OutDir)$(TargetFileName).json" />
    <Copy SourceFiles="$(ProjectDir)appsettings.json" DestinationFiles="$(OutDir)$(TargetFileName).json" />
  </Target>
```
