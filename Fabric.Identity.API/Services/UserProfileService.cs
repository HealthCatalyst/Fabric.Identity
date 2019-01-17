using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Persistence;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using Serilog;

namespace Fabric.Identity.API.Services
{
    using Fabric.Identity.API.Configuration;

    using IdentityModel;

    public class UserProfileService : IProfileService
    {
        private readonly ILogger _logger;
        private readonly IUserStore _userStore;
        private readonly IAppConfiguration _appConfig;

        public UserProfileService(IUserStore userStore, ILogger logger, IAppConfiguration appConfig)
        {
            _userStore = userStore;
            _logger = logger;
            _appConfig = appConfig;
        }

        /// <summary>
        ///     This method is called whenever claims about the user are requested (e.g. during token creation or via the userinfo
        ///     endpoint)
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            context.LogProfileRequest(_logger);

            if (context.RequestedClaimTypes.Any())
            {
                var user = await _userStore.FindBySubjectIdAsync(context.Subject.GetSubjectId());
                if (user != null)
                {
                    context.AddRequestedClaims(user.Claims);
                }
            }

            context.LogIssuedClaims(_logger);
        }

        /// <summary>
        ///     This method gets called whenever identity server needs to determine if the user is valid or active (e.g. if the
        ///     user's account has been deactivated since they logged in).
        ///     (e.g. during token issuance or validation).
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public async Task IsActiveAsync(IsActiveContext context)
        {
            var sub = context.Subject.GetSubjectId();
            _logger.Debug($"found sub from IsActiveContext");
            var user = await _userStore.FindBySubjectIdAsync(sub);

            context.IsActive = user != null;

            if (user != null 
                    && _appConfig.AzureAuthenticationEnabled 
                    && user.ProviderName == FabricIdentityConstants.AuthenticationSchemes.Azure)
            {
                var issuerClaim = user.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.Issuer);
                _logger.Debug($"AzureAuthenticationIssuer =  {issuerClaim?.Value}");
                context.IsActive = _appConfig.AzureActiveDirectorySettings.IssuerWhiteList.Contains(issuerClaim?.Value);
            }
            
        }
    }
}