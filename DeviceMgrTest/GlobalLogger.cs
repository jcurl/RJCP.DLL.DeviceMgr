﻿namespace RJCP.IO.DeviceMgr
{
    // This file is only for .NET Core

    using Microsoft.Extensions.Logging;
    using RJCP.CodeQuality.NUnitExtensions.Trace;
    using RJCP.Diagnostics.Trace;

    internal static class GlobalLogger
    {
        static GlobalLogger()
        {
            ILoggerFactory factory = LoggerFactory.Create(builder => {
                builder
                    .AddFilter("Microsoft", LogLevel.Warning)
                    .AddFilter("System", LogLevel.Warning)
                    .AddFilter("RJCP.IO.DeviceMgr", LogLevel.Debug)
                    .AddNUnitLogger();
            });
            LogSource.SetLoggerFactory(factory);
        }

        // Just calling this method will result in the static constructor being executed.
        public static void Initialize()
        {
            /* Can be empty, reference will initialize static constructor */
        }
    }
}
