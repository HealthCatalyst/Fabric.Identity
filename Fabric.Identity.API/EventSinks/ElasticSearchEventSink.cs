using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Platform.Logging;
using Fabric.Platform.Shared.Configuration;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Serilog;
using Serilog.Core;

namespace Fabric.Identity.API.EventSinks
{
    public class ElasticSearchEventSink : IEventSink
    {
        private readonly ILogger _logger;

        public ElasticSearchEventSink(IAppConfiguration settings)
        {
            _logger = LogFactory.CreateLogger(new LoggingLevelSwitch(), settings.ElasticSearchSettings, "identityservice-events");
        }
        public Task PersistAsync(Event evt)
        {
            if (evt.EventType == EventTypes.Success ||
                evt.EventType == EventTypes.Information)
            {
                _logger.Information("{Name} ({Id}), Details: {@details}",
                    evt.Name,
                    evt.Id,
                    evt);
            }
            else
            {
                _logger.Error("{Name} ({Id}), Details: {@details}",
                    evt.Name,
                    evt.Id,
                    evt);
            }

            return Task.CompletedTask;
        }
    }
}
