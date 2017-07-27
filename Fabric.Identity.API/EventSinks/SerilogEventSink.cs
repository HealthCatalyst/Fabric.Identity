using System;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Platform.Logging;
using IdentityServer4.Events;
using IdentityServer4.Services;
using Serilog;
using Serilog.Core;

namespace Fabric.Identity.API.EventSinks
{
    public class SerilogEventSink : IEventSink
    {
        private readonly ILogger _logger;

        public SerilogEventSink(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
