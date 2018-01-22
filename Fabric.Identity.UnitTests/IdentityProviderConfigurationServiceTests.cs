using System.Collections.Generic;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Moq;
using Xunit;

namespace Fabric.Identity.UnitTests
{
    public class IdentityProviderConfigurationServiceTests
    {
        [Fact]
        public void GetConfiguredIdentityProviders_ReturnsIdentityProviders()
        {
            var expectedProviders = new List<AuthenticationDescription>
            {
                new AuthenticationDescription
                {
                    AuthenticationScheme = "OpenId Connect",
                    DisplayName = "Azure Active Directory"
                },
                new AuthenticationDescription
                {
                    AuthenticationScheme = "Windows",
                    DisplayName = "Windows"
                }
            };

            var authenticationManagerMock = new Mock<AuthenticationManager>();
            authenticationManagerMock.Setup(mock => mock.GetAuthenticationSchemes())
                .Returns(expectedProviders);

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(mock => mock.Authentication).Returns(authenticationManagerMock.Object);

            var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(mock => mock.HttpContext).Returns(httpContextMock.Object);

            var appConfig = new AppConfiguration {WindowsAuthenticationEnabled = true};

            var identityProviderConfigurationService = new IdentityProviderConfigurationService(httpContextAccessorMock.Object, appConfig);
            var providers = identityProviderConfigurationService.GetConfiguredIdentityProviders();
            Assert.NotNull(providers);
            Assert.Equal(expectedProviders.Count, providers.Count);
        }
    }
}
