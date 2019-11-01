using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using Moq;
using System.Threading.Tasks;
using Xunit;
using static Fabric.Identity.API.FabricIdentityEnums;

namespace Fabric.Identity.IntegrationTests
{
    public class ActiveDirectoryProviderServiceTests
    {
        private readonly AppConfiguration _appConfig;
        private readonly ActiveDirectoryProviderService _providerService;

        public ActiveDirectoryProviderServiceTests()
        {
            var activeDirectoryProxyMock = new Mock<IActiveDirectoryProxy>()
                .SetupActiveDirectoryProxy(new ActiveDirectoryDataHelper().GetPrincipals());

            _appConfig = new AppConfiguration
            {
                DomainName = "testing"
            };

            _providerService = new ActiveDirectoryProviderService(activeDirectoryProxyMock.Object, _appConfig);
        }

        [Fact]
        public async Task FindUserBySubjectId_ValidId_SuccessAsync()
        {
            var user = await _providerService.FindUserBySubjectIdAsync($"{_appConfig.DomainName}\\patrick.jones");

            Assert.NotNull(user);
            Assert.Equal("patrick", user.FirstName);
            Assert.Equal("jones", user.LastName);
            Assert.Equal(PrincipalType.User, user.PrincipalType);
        }

        [Fact]
        public async Task FindUserBySubjectId_InvalidSubjectIdFormat_NullResultAsync()
        {
            var user = await _providerService.FindUserBySubjectIdAsync($"{_appConfig.DomainName}.patrick.jones");

            Assert.Null(user); 
        }

        [Fact]
        public async Task FindUserBySubjectId_UserNotFound_NullResultAsync()
        {
            var user = await _providerService.FindUserBySubjectIdAsync($"{_appConfig.DomainName}\\patrick.jon");

            Assert.Null(user);
        }
    }
}
