using System;
using System.Collections.Generic;
using Fabric.Identity.API.Exceptions;
using IdentityServer4.Models;
using Serilog;

namespace Fabric.Identity.API.Persistence.InMemory.Services
{
    public class InMemoryDbBootstrapper : IDbBootstrapper
    {
        private readonly IDocumentDbService _documentDbService;
        private readonly ILogger _logger;

        public InMemoryDbBootstrapper(IDocumentDbService documentDbService, ILogger logger)
        {
            _documentDbService = documentDbService;
            _logger = logger;
        }

        public bool Setup()
        {
            return true;
        }

        public void AddResources(IEnumerable<IdentityResource> resources)
        {
            foreach (var identityResource in resources)
            {
                try
                {
                    _documentDbService.AddDocument(identityResource.Name, identityResource);
                }
                catch (ResourceOperationException e)
                {
                    _logger.Warning(e, e.Message);
                }
                catch (ArgumentException e)
                {
                    _logger.Warning(e, e.Message);
                }
            }
        }
    }
}