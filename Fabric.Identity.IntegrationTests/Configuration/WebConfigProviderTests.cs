namespace Fabric.IdentityProviderSearchService.IntegrationTests.Configuration
{
    using Fabric.IdentityProviderSearchService.Configuration;

    using Microsoft.Extensions.Configuration;

    using Xunit;

    public class WebConfigProviderTests
    {
        [Fact]
        public void Load_BindsSettings_Success()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(configuration, appConfig);

            Assert.Equal("http://localhost:5001/", appConfig.IdentityServerConfidentialClientSettings.Authority);
        }

        [Fact]
        public void Load_WebConfigOverridesJson_Success()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Add(new WebConfigProvider())
                .Build();

            var appConfig = new AppConfiguration();
            ConfigurationBinder.Bind(configuration, appConfig);

            Assert.Equal("http://localhost/identity/", appConfig.IdentityServerConfidentialClientSettings.Authority);
        }
    }
}
