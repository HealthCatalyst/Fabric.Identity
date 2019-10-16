using System;
using Fabric.Identity.API.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Fabric.Identity.API.Persistence.SqlServer.Configuration;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace Fabric.Identity.API.Logging
{
    public class LogFactory
    {
        public static LoggingLevelSwitch LoggingLevelSwitch { get; } = new LoggingLevelSwitch();

        public static LoggerConfiguration ConfigureTraceLogger(LoggerConfiguration loggerConfig, ApplicationInsights appInsightsConfig,
            LoggingLevelSwitch levelSwitch = null)
        {
            ConfigureLoggerConfiguration(loggerConfig, levelSwitch ?? LoggingLevelSwitch);

            if (appInsightsConfig != null && appInsightsConfig.Enabled &&
                !string.IsNullOrEmpty(appInsightsConfig.InstrumentationKey))
            {
                loggerConfig.WriteTo.ApplicationInsights(TelemetryConverter.Traces);
            }

            return loggerConfig;
        }

        public static ILogger CreateEventLogger(HostingOptions hostingOptions, IConnectionStrings connectionStrings, LoggingLevelSwitch levelSwitch = null)
        {

            if (hostingOptions.StorageProvider.Equals(FabricIdentityConstants.StorageProviders.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                var columnOptions = new ColumnOptions();
                columnOptions.Store.Add(StandardColumn.LogEvent);

                return new LoggerConfiguration().Enrich.FromLogContext()
                    .WriteTo.MSSqlServer(
                        connectionStrings.IdentityDatabase,
                        "EventLogs",
                        columnOptions: columnOptions)
                    .CreateLogger();
            }
            
            var loggerConfiguration = ConfigureLoggerConfiguration(new LoggerConfiguration(), levelSwitch ?? LoggingLevelSwitch);
            return loggerConfiguration.CreateLogger();
        }

        private static LoggerConfiguration ConfigureLoggerConfiguration(LoggerConfiguration loggerConfig, LoggingLevelSwitch levelSwitch)
        {
            return loggerConfig
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext()        
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .WriteTo.ColoredConsole();
        }
    }
}
