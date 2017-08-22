using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using Serilog;

namespace Fabric.Identity.API.Extensions
{
    /// <summary>
    /// Extensions for ProfileDataRequestContext
    /// </summary>
    public static class ProfileDataRequestContextExtensions
    {
        /// <summary>
        /// Filters the claims based on requested claim types.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="claims">The claims.</param>
        /// <returns></returns>
        public static List<Claim> FilterClaims(this ProfileDataRequestContext context, IEnumerable<Claim> claims)
        {
            return claims.Where(x => context.RequestedClaimTypes.Contains(x.Type)).ToList();
        }

        /// <summary>
        /// Filters the claims based on the requested claim types and then adds them to the IssuedClaims collection.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="claims">The claims.</param>
        public static void AddRequestedClaims(this ProfileDataRequestContext context, IEnumerable<Claim> claims)
        {
            if (context.RequestedClaimTypes.Any())
            {
                context.IssuedClaims.AddRange(context.FilterClaims(claims));
            }
        }

        /// <summary>
        /// Logs the profile request.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        public static void LogProfileRequest(this ProfileDataRequestContext context, ILogger logger)
        {
            logger.Debug("Get profile called for subject {subject} from client {client} with claim types {claimTypes} via {caller}",
                context.Subject.GetSubjectId(),
                context.Client.ClientName ?? context.Client.ClientId,
                context.RequestedClaimTypes,
                context.Caller);
        }

        /// <summary>
        /// Logs the issued claims.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="logger">The logger.</param>
        public static void LogIssuedClaims(this ProfileDataRequestContext context, ILogger logger)
        {
            logger.Debug("Issued claims: {claims}", context.IssuedClaims.Select(c => c.Type));
        }
    }
}
