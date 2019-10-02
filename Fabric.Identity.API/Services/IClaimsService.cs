using System.Threading.Tasks;
using Fabric.Identity.API.Models;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication;

namespace Fabric.Identity.API.Services
{
    public interface IClaimsService
    {
        Task<ClaimsResult> GenerateClaimsForIdentity(AuthenticateResult info, AuthorizationRequest context);
        string GetEffectiveSubjectId(ClaimsResult claimsResult, User user);
        string GetEffectiveUserId(ClaimsResult claimInformation);
    }
}