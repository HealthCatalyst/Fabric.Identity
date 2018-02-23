using System;
using System.Net.Http;
using Novell.Directory.Ldap;
using Polly;
using Polly.CircuitBreaker;

namespace Fabric.Identity.API.Infrastructure
{
    public class PolicyProvider
    {
        public readonly CircuitBreakerPolicy LdapErrorPolicy = Policy.Handle<LdapException>()
            .CircuitBreaker(5, TimeSpan.FromMinutes(5));

        public readonly CircuitBreakerPolicy IdPSearchServiceErrorPolicy = Policy.Handle<HttpRequestException>()
            .CircuitBreaker(5, TimeSpan.FromMinutes(5));
    }
}
