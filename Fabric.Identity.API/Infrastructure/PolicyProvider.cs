using System;
using System.Net.Http;
using System.Threading.Tasks;
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

        private readonly PolicyBuilder _idPSearchServicePolicyBuilder = Policy.Handle<HttpRequestException>()
            .Or<TaskCanceledException>();

        public readonly RetryPolicy IdPSearchServiceRetryPolicy;
        public readonly CircuitBreakerPolicy IdPSearchServiceErrorPolicy;
            

        public readonly PolicyWrap IdPSearchServicePolicy;

        public PolicyProvider()
        {
            IdPSearchServiceRetryPolicy = _idPSearchServicePolicyBuilder.RetryAsync(3);
            IdPSearchServiceErrorPolicy = _idPSearchServicePolicyBuilder.CircuitBreakerAsync(3, TimeSpan.FromMinutes(5));
            IdPSearchServicePolicy = IdPSearchServiceRetryPolicy.WrapAsync(IdPSearchServiceErrorPolicy);
        }
    }
}
