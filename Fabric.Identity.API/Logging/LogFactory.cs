using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fabric.Identity.API.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.MSSqlServer;
using Fabric.Identity.API.Persistence.SqlServer.Configuration;
using IdentityServer4.Events;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Newtonsoft.Json;
using RestSharp.Extensions;

namespace Fabric.Identity.API.Logging
{
    public static class LogFactory
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

        public static ILogger CreateEventLogger(LoggingLevelSwitch levelSwitch, HostingOptions hostingOptions, IConnectionStrings connectionStrings, ApplicationInsights appInsightsConfig)
        {
            var loggerConfiguration = new LoggerConfiguration().Enrich.FromLogContext();
            if (hostingOptions.StorageProvider.Equals(FabricIdentityConstants.StorageProviders.SqlServer, StringComparison.OrdinalIgnoreCase))
            {
                var columnOptions = new ColumnOptions();
                columnOptions.Store.Add(StandardColumn.LogEvent);

                loggerConfiguration
                    .WriteTo.MSSqlServer(
                        connectionStrings.IdentityDatabase,
                        "EventLogs",
                        columnOptions: columnOptions);
            }

            if (appInsightsConfig != null && appInsightsConfig.Enabled &&
                !string.IsNullOrEmpty(appInsightsConfig.InstrumentationKey))
            {
                loggerConfiguration.WriteTo.ApplicationInsights(appInsightsConfig.InstrumentationKey, SanitizeLogEvents);
            }
            
            return loggerConfiguration.CreateLogger();
        }

        private static LoggerConfiguration CreateLoggerConfiguration(LoggingLevelSwitch levelSwitch)
        {
            var logFilePath = Path.Combine("logs", "fabricidentitylog.txt");
            return new LoggerConfiguration()
                .MinimumLevel.ControlledBy(levelSwitch)
                .Enrich.FromLogContext()        
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .WriteTo.ColoredConsole()
                .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 40);
        }

        private static ITelemetry SanitizeLogEvents(LogEvent serilogLogEvent, IFormatProvider formatProvider)
        {
            var eventTelemetry = new EventTelemetry
            {
                Name = serilogLogEvent.Properties["Name"].ToString(),
                Timestamp = serilogLogEvent.Timestamp
            };
            foreach (var property in serilogLogEvent.Properties)
            {
                if (!property.Key.Equals("details", StringComparison.OrdinalIgnoreCase))
                {
                    eventTelemetry.Properties[property.Key] = property.Value.ToString();
                }
            }
            return eventTelemetry;
        }
    }
}
