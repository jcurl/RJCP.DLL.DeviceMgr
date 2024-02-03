namespace RJCP.DeviceInfoDump
{
#if NET6_0_OR_GREATER
    using System.IO;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using RJCP.Diagnostics.Trace;
#endif

    public static class GlobalLogger
    {
#if NET6_0_OR_GREATER
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
