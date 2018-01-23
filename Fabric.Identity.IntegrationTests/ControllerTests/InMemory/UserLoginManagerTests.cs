using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
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

            var firstLoginDate = user.LastLoginDatesByClient.First().LoginDate;
            
            //Sleep to ensure some time passes between logins
            Thread.Sleep(10);

            user = await  userLoginManager.UserLogin(provider, userId, claims, clientId);

            var secondLoginDate = user.LastLoginDatesByClient.First().LoginDate;
            
            Assert.True(secondLoginDate >= firstLoginDate, "second login date is not greater than first");
            
        }

        [Fact]
        public async void TestUserLogin_AddClaims_Success()
        {
            var userLoginManager = new UserLoginManager(UserStore, new Mock<ILogger>().Object);
            var userId = $"HealthCatalyst\\{GetRandomString()}";
            var provider = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows;
            var clientId = "sampleApp";
            var claims = GetBaseClaims();

            await userLoginManager.UserLogin(provider, userId, claims, clientId);

            var storedUser = UserStore.FindBySubjectIdAsync(userId).Result;

            Assert.Equal(claims.Count, storedUser.Claims.Count);
            Assert.Equal(claims.First(c => c.Type == ClaimTypes.Name).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.Name).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.GivenName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.GivenName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.FamilyName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.FamilyName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.Role).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.Role).Value);

        }

        [Fact]
        public async void TestUserLogin_UpdateClaims_Success()
        {
            var userLoginManager = new UserLoginManager(UserStore, new Mock<ILogger>().Object);
            var userId = $"HealthCatalyst\\{GetRandomString()}";
            var provider = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows;
            var clientId = "sampleApp";
            var claims = GetBaseClaims();

            await userLoginManager.UserLogin(provider, userId, claims, clientId);

            var storedUser = UserStore.FindBySubjectIdAsync(userId).Result;

            Assert.Equal(claims.Count, storedUser.Claims.Count);

            var claim = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.FamilyName);
            claims.Remove(claim);
            claim = new Claim(JwtClaimTypes.FamilyName, "bar");
            claims.Add(claim);
            await userLoginManager.UserLogin(provider, userId, claims, clientId);

            storedUser = UserStore.FindBySubjectIdAsync(userId).Result;
            Assert.Equal(claims.Count, storedUser.Claims.Count);
            Assert.Equal(claims.First(c => c.Type == ClaimTypes.Name).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.Name).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.GivenName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.GivenName).Value);
            Assert.Equal(claim.Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.FamilyName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.Role).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.Role).Value);

        }

        [Fact]
        public async void TestUserLogin_UpdateClaims_AddsNewClaim()
        {
            var userLoginManager = new UserLoginManager(UserStore, new Mock<ILogger>().Object);
            var userId = $"HealthCatalyst\\{GetRandomString()}";
            var provider = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows;
            var clientId = "sampleApp";
            var claims = GetBaseClaims();

            await userLoginManager.UserLogin(provider, userId, claims, clientId);

            var storedUser = UserStore.FindBySubjectIdAsync(userId).Result;

            Assert.Equal(claims.Count, storedUser.Claims.Count);

            var claim = new Claim(JwtClaimTypes.Role, @"FABRIC\Health Catalyst Editor");
            claims.Add(claim);
            await userLoginManager.UserLogin(provider, userId, claims, clientId);

            storedUser = UserStore.FindBySubjectIdAsync(userId).Result;
            Assert.Equal(claims.Count, storedUser.Claims.Count);
            Assert.Equal(claims.First(c => c.Type == ClaimTypes.Name).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.Name).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.GivenName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.GivenName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.FamilyName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.FamilyName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value);
            Assert.Equal(claims.Count(c => c.Type == JwtClaimTypes.Role), storedUser.Claims.Count(c => c.Type == JwtClaimTypes.Role));
            Assert.Equal(2, storedUser.Claims.Count(c => c.Type == JwtClaimTypes.Role));

        }

        [Fact]
        public async void TestUserLogin_UpdateClaims_RemovesClaim()
        {
            var userLoginManager = new UserLoginManager(UserStore, new Mock<ILogger>().Object);
            var userId = $"HealthCatalyst\\{GetRandomString()}";
            var provider = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows;
            var clientId = "sampleApp";
            var claims = GetBaseClaims();
            claims.Add(new Claim(JwtClaimTypes.Role, @"FABRIC\Health Catalyst Editor"));

            await userLoginManager.UserLogin(provider, userId, claims, clientId);

            var storedUser = UserStore.FindBySubjectIdAsync(userId).Result;

            Assert.Equal(claims.Count, storedUser.Claims.Count);

            var claim = claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Role);
            claims.Remove(claim);
            await userLoginManager.UserLogin(provider, userId, claims, clientId);

            storedUser = UserStore.FindBySubjectIdAsync(userId).Result;
            Assert.Equal(claims.Count, storedUser.Claims.Count);
            Assert.Equal(claims.First(c => c.Type == ClaimTypes.Name).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.Name).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.GivenName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.GivenName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.FamilyName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.FamilyName).Value);
            Assert.Equal(claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value, storedUser.Claims.First(c => c.Type == JwtClaimTypes.MiddleName).Value);
            Assert.Equal(claims.Count(c => c.Type == JwtClaimTypes.Role), storedUser.Claims.Count(c => c.Type == JwtClaimTypes.Role));
            Assert.Equal(1, storedUser.Claims.Count(c => c.Type == JwtClaimTypes.Role));

        }

        private List<Claim> GetBaseClaims()
        {
            var userNameClaim = new Claim(ClaimTypes.Name, "foo baz");
            var firstNameClaim = new Claim(JwtClaimTypes.GivenName, "foo");
            var lastNameClaim = new Claim(JwtClaimTypes.FamilyName, "baz");
            var middleNameClaim = new Claim(JwtClaimTypes.MiddleName, "dot");
            var roleClaim = new Claim(JwtClaimTypes.Role, @"FABRIC\Health Catalyst Viewer");
            var claims = new List<Claim>
            {
                userNameClaim,
                firstNameClaim,
                lastNameClaim,
                middleNameClaim,
                roleClaim
            };
            return claims;
        }


    }
}
