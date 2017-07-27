using System;
using System.Threading.Tasks;
using Fabric.Identity.API.Infrastructure;
using Fabric.Identity.API.Services;
using IdentityServer4.Events;
using IdentityServer4.Services;

namespace Fabric.Identity.API.EventSinks
{
    public class CouchDbEventSink : IEventSink
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly IEventSink _innerEventSink;

        public CouchDbEventSink(Decorator<IDocumentDbService> documentDbService, Decorator<IEventSink> innerEventSink)
        {
            _documentDbService = documentDbService.Instance;
            _innerEventSink = innerEventSink.Instance;
        }
        public Task PersistAsync(Event evt)
        {
            _innerEventSink.PersistAsync(evt);
            _documentDbService.AddDocument(Guid.NewGuid().ToString(), evt);
            return Task.CompletedTask;
        }
    }
}
