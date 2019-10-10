using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Services;
using IdentityModel;
using Xunit;

namespace Fabric.Identity.UnitTests.Services
{
    public class GroupFilterServiceTests
    {
        [Fact]
        public void FilterClaims_NullGroupSettings_ReturnsOriginalClaims()
        {
            var claims = new List<Claim>
            {
                new Claim("role", "Admin")
            };

            var filteredClaims = new GroupFilterService(null).FilterClaims(claims).ToList();

            Assert.Single(filteredClaims);
            Assert.Equal("Admin", filteredClaims[0].Value);
        }

        [Fact]
        public void FilterClaims_GroupSettingsNullFilters_ReturnsOriginalClaims()
        {
            var groupMatchSettings = new GroupFilterSettings();
            
            var claims = new List<Claim>
            {
                new Claim("role", "Admin")
            };

            var filteredClaims = new GroupFilterService(groupMatchSettings).FilterClaims(claims).ToList();

            Assert.Single(filteredClaims);
            Assert.Equal("Admin", filteredClaims[0].Value);
        }

        [Fact]
        public void FilterClaims_EmptyPrefixesAndSuffixes_ReturnsOriginalClaims()
        {
            var groupMatchSettings = new GroupFilterSettings
            {
                Prefixes = new List<string>().ToArray(),
                Suffixes = new List<string>().ToArray()
            };

            var claims = new List<Claim>
            {
                new Claim("role", "Admin")
            };

            var filteredClaims = new GroupFilterService(groupMatchSettings).FilterClaims(claims).ToList();

            Assert.Single(filteredClaims);
            Assert.Equal("Admin", filteredClaims[0].Value);
        }

        [Fact]
        public void FilterClaims_NoMatchingPrefixesAndSuffixes_ReturnsEmptyClaimList()
        {
            var groupMatchSettings = new GroupFilterSettings
            {
                Prefixes = new List<string>
                {
                    "foo"
                }.ToArray(),
                Suffixes = new List<string>
                {
                    "bar"
                }.ToArray()
            };

            var claims = new List<Claim>
            {
                new Claim("role", "Admin")
            };

            var filteredClaims = new GroupFilterService(groupMatchSettings).FilterClaims(claims).ToList();

            Assert.Empty(filteredClaims);
        }

        [Fact]
        public void FilterClaims_MatchingPrefixesAndSuffixes_ReturnsFilteredClaims()
        {
            var groupMatchSettings = new GroupFilterSettings
            {
                Prefixes = new List<string>
                {
                    "Admin",
                    "User",
                    "HC"
                }.ToArray(),
                Suffixes = new List<string>
                {
                    "user",
                    "Safety"
                }.ToArray()
            };

            var claims = new List<Claim>
            {
                new Claim("role", "Administrator"),
                new Claim("role", "User"),
                new Claim("role", "NoMatch"),
                new Claim("role", "Superuser"),
                new Claim("role", "HCPatientSafety")
            };

            var filteredClaims = new GroupFilterService(groupMatchSettings).FilterClaims(claims).ToList();

            var claimComparer = new ClaimComparer();
            Assert.Equal(4, filteredClaims.Count);
            Assert.Contains(claims[0], filteredClaims, claimComparer);
            Assert.Contains(claims[1], filteredClaims, claimComparer);
            Assert.Contains(claims[3], filteredClaims, claimComparer);
            Assert.Contains(claims[4], filteredClaims, claimComparer);
        }
    }
}