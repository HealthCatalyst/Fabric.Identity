using System;
using Novell.Directory.Ldap;
using Polly;
using Polly.CircuitBreaker;

namespace Fabric.Identity.API.Infrastructure
{
    public class PolicyProvider
    {
        public readonly CircuitBreakerPolicy LdapErrorPolicy = Policy.Handle<LdapException>()
            .CircuitBreaker(5, TimeSpan.FromMinutes(5));

        public readonly CircuitBreakerPolicy IdPSearchServiceErrorPolicy = Policy.Handle<Exception>()
            .CircuitBreaker(5, TimeSpan.FromMinutes(5));
    }
}
