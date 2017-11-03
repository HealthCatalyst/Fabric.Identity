using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Persistence.SqlServer.Models;
using Fabric.Identity.API.Persistence.SqlServer.Services;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;

namespace Fabric.Identity.API.Persistence.SqlServer.Stores
{
    public class SqlServerClientStore : IClientManagementStore
    {
        private readonly IIdentityDbContext _identityDbContext;

        public SqlServerClientStore(IIdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;            
        }

        public async Task<Client> FindClientByIdAsync(string clientId)
        {
            var client = await _identityDbContext.Clients
                .Include(x => x.AllowedGrantTypes)
                .Include(x => x.RedirectUris)
                .Include(x => x.PostLogoutRedirectUris)
                .Include(x => x.AllowedScopes)
                .Include(x => x.ClientSecrets)
                .Include(x => x.Claims)
                .Include(x => x.IdentityProviderRestrictions)
                .Include(x => x.AllowedCorsOrigins)
               // .Include(x => x.Properties)
                .FirstOrDefaultAsync(x => x.ClientId == clientId);
            var clientEntity = client?.ToModel();

            return clientEntity;
        }

        public IEnumerable<Client> GetAllClients()
        {
            var clients = _identityDbContext.Clients
                .Where(c => !c.IsDeleted)
                .Select(c => c.ToModel());

            return clients;
        }

        public int GetClientCount()
        {
            return GetAllClients().Count();
        }

        public void AddClient(Client client)
        {
            AddClientAsync(client).Wait();
        }

        public void UpdateClient(string clientId, Client client)
        {
            UpdateClientAsync(clientId, client).Wait();
        }

        public void DeleteClient(string id)
        {
            DeleteClientAsync(id).Wait();
        }

        public async Task AddClientAsync(Client client)
        {
            var domainModelClient = client.ToFabricEntity();

            //TODO: set domain model properties

            await _identityDbContext.Clients.AddAsync(domainModelClient);
        }

        public async Task UpdateClientAsync(string clientId, Client client)
        {
            var clientDomainModel = client.ToFabricEntity();

            //TODO: set domain model properties

            _identityDbContext.Clients.Update(clientDomainModel);
            await _identityDbContext.SaveChangesAsync();
        }

        public async Task DeleteClientAsync(string id)
        {
            var clientToDelete =
               await _identityDbContext.Clients.FirstOrDefaultAsync(c =>
                    c.ClientId.Equals(id, StringComparison.OrdinalIgnoreCase));

            //TODO: set other domain model properties

            clientToDelete.IsDeleted = true;

            await _identityDbContext.SaveChangesAsync();
        }
    }
}
