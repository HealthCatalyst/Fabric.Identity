using System;
using System.Linq;
using System.Reflection;
using Fabric.Identity.API.Models;
using Xunit;
using IS4 = IdentityServer4.Models;

namespace Fabric.Identity.UnitTests
{
    public class ModelTests
    {
        [Fact]
        public void TestGrantTypes()
        {
            var grantTypes = typeof(IS4.GrantType).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null).ToString());

            Assert.NotNull(grantTypes);
        }

        [Fact]
        public void TestClientConversion()
        {
            var is4Client = new IS4.Client();
            var client = is4Client.ToClientViewModel();

            // Secret should never be automatically copied to view model.
            Assert.Null(client.ClientSecret);
        }

        [Fact]
        public void TestResourceConversion()
        {
            var is4Resource = new IS4.ApiResource();
            var resource = is4Resource.ToApiResourceViewModel();

            // Secret should never be automatically copied to view model.
            Assert.Null(resource.ApiSecret);
        }

    }
}