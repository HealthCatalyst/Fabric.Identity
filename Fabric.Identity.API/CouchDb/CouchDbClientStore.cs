using System.Collections.Generic;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Models;
using IdentityServer4.Stores;

namespace Fabric.Identity.API.CouchDb
{
    public class CouchDbClientStore : IClientStore
    {
        private readonly IDocumentDbService _documentDbService;

        public CouchDbClientStore(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            return _documentDbService.GetDocument<Client>(clientId);
        }

        //This is temporary
        public void AddClients()
        {
            var clientList = new List<Client>
            {
                new Client
                {
                    ClientId = "fabric-mvcsample",
                    ClientName = "Sample Fabric MVC Client",
                    AllowedGrantTypes = GrantTypes.Hybrid,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    RedirectUris = {"http://localhost:5002/signin-oidc"},
                    PostLogoutRedirectUris = {"http://localhost:5002/signout-callback-oidc"},

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "fabric.profile",
                        "patientapi",
                    },
                    AllowOfflineAccess = true,
                    RequireConsent = false
                },
                new Client
                {
                    ClientId = "fabric-angularsample",
                    ClientName = "Sample Fabric Angular Client",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    AllowAccessTokensViaBrowser = true,

                    RedirectUris = {"http://localhost:4200/oidc-callback.html"},
                    PostLogoutRedirectUris = {"http://localhost:4200"},
                    AllowedCorsOrigins = {"http://localhost:4200"},

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "fabric.profile",
                        "patientapi",
                    },
                    AllowOfflineAccess = true,
                    RequireConsent = false
                },
                new Client
                {
                    ClientId = "fabric-sampleapi-client",
                    ClientName = "Sample API Client",
                    AllowedGrantTypes = GrantTypes.ClientCredentials,

                    ClientSecrets =
                    {
                        new Secret("secret".Sha256())
                    },

                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "fabric.profile",
                        "patientapi",
                    },
                    RequireConsent = false
                }
            };

            foreach (var client in clientList)
            {
                AddToCouchDb(client);

            }
        }

        private void AddToCouchDb(Client client)
        {
            _documentDbService.AddDocument(client.ClientId, client);
        }
    }
}
