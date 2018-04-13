using System;
using Fabric.Identity.API.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;

namespace Fabric.Identity.API.Logging
{
    public class LogFactory
    {
        public static ILogger CreateTraceLogger(LoggingLevelSwitch levelSwitch, ApplicationInsights appInsightsConfig)
        {
            var loggerConfiguration = CreateLoggerConfiguration(levelSwitch);

            if (appInsightsConfig != null && appInsightsConfig.Enabled &&
                !string.IsNullOrEmpty(appInsightsConfig.InstrumentationKey))
            {
                loggerConfiguration.WriteTo.ApplicationInsightsTraces(appInsightsConfig.InstrumentationKey);
            }

            return loggerConfiguration.CreateLogger();
        }

        public static ILogger CreateEventLogger(LoggingLevelSwitch levelSwitch, IAppConfiguration appConfiguration)
        {

            if (appConfiguration.HostingOptions.StorageProvider.Equals(FabricIdentityConstants.StorageProviders.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                var columnOptions = new ColumnOptions();
                columnOptions.Store.Add(StandardColumn.LogEvent);

                return new LoggerConfiguration().Enrich.FromLogContext()
                    .WriteTo.MSSqlServer(
                        appConfiguration.ConnectionStrings.IdentityDatabase,
                        "EventLogs",
                        columnOptions: columnOptions)
                    .CreateLogger();
            }
            
            var loggerConfiguration = CreateLoggerConfiguration(levelSwitch);
            return loggerConfiguration.CreateLogger();
        }

        private static LoggerConfiguration CreateLoggerConfiguration(LoggingLevelSwitch levelSwitch)
        {
            return new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext()        
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .WriteTo.ColoredConsole();
        }
    }
}
