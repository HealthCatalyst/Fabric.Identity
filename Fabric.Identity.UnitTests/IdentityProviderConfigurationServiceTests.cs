using System.Collections.Generic;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class IdentityProviderConfigurationServiceTests
    {
        [Fact]
        public async Task GetConfiguredIdentityProviders_ReturnsIdentityProvidersAsync()
        {
            var expectedProviders = new List<AuthenticationScheme>
            {
                new AuthenticationScheme("OpenID Connect", "Azure Active Directory", typeof(JwtBearerHandler)),
                new AuthenticationScheme("Windows", "Windows", typeof(JwtBearerHandler))
            };

            var httpContextMock = new Mock<HttpContext>();

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(mock => mock.HttpContext).Returns(httpContextMock.Object);

            var authenticationSchemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
            authenticationSchemeProviderMock.Setup(mock => mock.GetAllSchemesAsync()).ReturnsAsync(expectedProviders);

            var appConfig = new AppConfiguration { WindowsAuthenticationEnabled = true };

            var identityProviderConfigurationService = new IdentityProviderConfigurationService(httpContextAccessorMock.Object, authenticationSchemeProviderMock.Object, appConfig);
            var providers = await identityProviderConfigurationService.GetConfiguredIdentityProviders();
            Assert.NotNull(providers);
            Assert.Equal(expectedProviders.Count, providers.Count);
        }
    }
}