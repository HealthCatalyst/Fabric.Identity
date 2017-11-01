using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence;
using IdentityServer4.Models;
using IdentityServer4.Services;

namespace Fabric.Identity.API.Services
{
    public class CorsPolicyDocumentDbService : ICorsPolicyService
    {
        private readonly IDocumentDbService _documentDbService;

        public CorsPolicyDocumentDbService(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            var clients = _documentDbService
                .GetDocuments<Client>(FabricIdentityConstants.DocumentTypes.ClientDocumentType).Result.ToList();

            return Task.FromResult(clients != null && clients.SelectMany(c => c.AllowedCorsOrigins).Contains(origin));
        }
    }
}