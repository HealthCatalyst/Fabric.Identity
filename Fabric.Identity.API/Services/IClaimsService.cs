using System.Collections.Generic;
using System.Security.Claims;
using Fabric.Identity.API.Models;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Http.Authentication;

namespace Fabric.Identity.API.Services
{
    public interface IClaimsService
    {
        ClaimsResult GenerateClaimsForIdentity(AuthenticateInfo info, AuthorizationRequest context);
        string GetEffectiveSubjectId(ClaimsResult ClaimInformation, User user);
        string GetEffectiveUserId(ClaimsResult claimInformation);
    }
}