using System;
using System.Net.Http;
using Novell.Directory.Ldap;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Wrap;

namespace Fabric.Identity.API.Infrastructure
{
    public class PolicyProvider
    {
        public readonly CircuitBreakerPolicy LdapErrorPolicy = Policy.Handle<LdapException>()
            .CircuitBreaker(5, TimeSpan.FromMinutes(5));

        public readonly RetryPolicy IdPSearchServiceRetryPolicy = Policy.Handle<HttpRequestException>()
            .RetryAsync(3);

        public readonly CircuitBreakerPolicy IdPSearchServiceErrorPolicy = Policy.Handle<HttpRequestException>()
            .CircuitBreakerAsync(3, TimeSpan.FromMinutes(1));

        public readonly PolicyWrap IdPSearchServicePolicy;

        public PolicyProvider()
        {
            IdPSearchServicePolicy = IdPSearchServiceRetryPolicy.WrapAsync(IdPSearchServiceErrorPolicy);
        }
    }
}
