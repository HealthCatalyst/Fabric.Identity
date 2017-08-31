using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Events;
using Fabric.Identity.API.Infrastructure;
using IdentityServer4.Services;

namespace Fabric.Identity.API.Services
{
    public class AuditingDocumentDbService : IDocumentDbService
    {
        private readonly ISerializationSettings _serializationSettings;
        private readonly IEventService _eventService;
        private readonly IDocumentDbService _innerDocumentDbService;
        private readonly IUserResolveService _userResolveService;

        public AuditingDocumentDbService(IUserResolveService userResolverService, IEventService eventService, Decorator<IDocumentDbService> decorator, ISerializationSettings serializationSettings)
        {
            _serializationSettings = serializationSettings;
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
            _innerDocumentDbService = decorator.Instance ?? throw new ArgumentNullException(nameof(decorator));
            _userResolveService = userResolverService ?? throw new ArgumentNullException(nameof(userResolverService));
        }
        public Task<T> GetDocument<T>(string documentId)
        {
            return _innerDocumentDbService.GetDocument<T>(documentId);
        }

        public Task<IEnumerable<T>> GetDocuments<T>(string documentType)
        {
            return _innerDocumentDbService.GetDocuments<T>(documentType);
        }

        public Task<int> GetDocumentCount(string documentType)
        {
            return _innerDocumentDbService.GetDocumentCount(documentType);
        }

        public void AddDocument<T>(string documentId, T documentObject)
        {
            _innerDocumentDbService.AddDocument(documentId, documentObject);
            _eventService.RaiseAsync(new EntityCreatedAuditEvent<T>(_userResolveService.Username,
                    _userResolveService.ClientId, _userResolveService.Subject, documentId, documentObject, _serializationSettings))
                .ConfigureAwait(false);
        }

        public void UpdateDocument<T>(string documentId, T documentObject)
        {
            _innerDocumentDbService.UpdateDocument(documentId, documentObject);
            _eventService.RaiseAsync(new EntityUpdatedAuditEvent<T>(_userResolveService.Username,
                    _userResolveService.ClientId, _userResolveService.Subject, documentId, documentObject, _serializationSettings))
                .ConfigureAwait(false);
        }

        public void DeleteDocument<T>(string documentId)
        {
            _innerDocumentDbService.DeleteDocument<T>(documentId);
            _eventService.RaiseAsync(new EntityDeletedAuditEvent<T>(_userResolveService.Username,
                    _userResolveService.ClientId, _userResolveService.Subject, documentId, _serializationSettings))
                .ConfigureAwait(false);
        }

    }
}
