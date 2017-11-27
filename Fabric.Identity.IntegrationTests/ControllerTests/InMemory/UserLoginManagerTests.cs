using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Fabric.Identity.API;
using Fabric.Identity.API.Management;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Fabric.Identity.API.Persistence.InMemory.Stores;
using IdentityModel;
using Moq;
using Serilog;
using Xunit;

namespace Fabric.Identity.IntegrationTests.ControllerTests.InMemory
{
    public class UserLoginManagerTests : IntegrationTestsFixture
    {
        protected IUserStore UserStore { get; set; }

        public UserLoginManagerTests(string provider = FabricIdentityConstants.StorageProviders.InMemory) :
            base(provider)
        {
            UserStore = new InMemoryUserStore(new InMemoryDocumentService());
        }

        private static readonly Random Random = new Random();
        private static string GetRandomString()
        {
            var path = Path.GetRandomFileName();
            path = path.Replace(".", "");

            var stringLength = Random.Next(5, path.Length);

            return path.Substring(0, stringLength);
        }

        [Fact]
        public async void TestUserLogin_UpdatesLastLoginByClient()
        {
            var userLoginManager = new UserLoginManager(UserStore, new Mock<ILogger>().Object);

            var userId = $"HealthCatalyst\\{GetRandomString()}";
            var provider = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows;
            var clientId = "sampleApp";
            var userName = "foo baz";
            var firstName = "foo";
            var lastName = "baz";
            var middleName = "dot";
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(JwtClaimTypes.GivenName, firstName),
                new Claim(JwtClaimTypes.FamilyName, lastName),
                new Claim(JwtClaimTypes.MiddleName, middleName),
                new Claim(JwtClaimTypes.Role, @"FABRIC\Health Catalyst Viewer")
            };

            var user = await userLoginManager.UserLogin(provider, userId, claims, clientId);

            var firstLoginDate = user.LastLoginDatesByClient.First().Value;

            user = await  userLoginManager.UserLogin(provider, userId, claims, clientId);

            var secondLoginDate = user.LastLoginDatesByClient.First().Value;

            Assert.True(secondLoginDate > firstLoginDate, "second login date is not greater than first");


        }
    }
}
