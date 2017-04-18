using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;

namespace Fabric.Identity.API
{
    public class Config
    {
        public static IEnumerable<IdentityResource> GetIdentityResources()
        {
            var fabricProfile = new IdentityResource(name: "fabric.profile", displayName: "Fabric Profile", claimTypes: new[] { "location", "allowedresource" });
            return new List<IdentityResource>
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile(),
                new IdentityResources.Email(),
                new IdentityResources.Address(),
                fabricProfile
            };
        }

        public static IEnumerable<ApiResource> GetApiResources()
        {
            return new List<ApiResource>
            {
                new ApiResource
                {
                    Name = "patientapi",
                    UserClaims = { JwtClaimTypes.Name, JwtClaimTypes.Email, "allowedresource"},
                    Scopes = { new Scope("patientapi", "Patient API") }
                }
            };
        }

        public static IEnumerable<Client> GetClients()
        {
            return new List<Client>
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

                    RedirectUris = { "http://localhost:5002/signin-oidc"},
                    PostLogoutRedirectUris = { "http://localhost:5002/signout-callback-oidc"},

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

                    RedirectUris = { "http://localhost:4200/oidc-callback"},
                    PostLogoutRedirectUris = { "http://localhost:4200" },
                    AllowedCorsOrigins = { "http://localhost:4200" },

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
                    ClientId ="fabric-sampleapi-client",
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
        }
    }
}
