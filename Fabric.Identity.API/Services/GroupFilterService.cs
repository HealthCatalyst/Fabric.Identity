using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Fabric.Identity.API.Configuration;
using IdentityModel;

namespace Fabric.Identity.API.Services
{
    public class GroupFilterService
    {
        private readonly GroupFilterSettings _groupMatchSettings;

        public GroupFilterService(GroupFilterSettings groupMatchSettings)
        {
            _groupMatchSettings = groupMatchSettings;
        }

        public IEnumerable<Claim> FilterClaims(IEnumerable<Claim> claims)
        {
            if (_groupMatchSettings == null)
            {
                return claims;
            }

            var prefixes = _groupMatchSettings.Prefixes?.ToList();
            var suffixes = _groupMatchSettings.Suffixes?.ToList();
            var claimList = claims.ToList();
            var prefixFilteredClaims = claimList;
            var suffixFilteredClaims = claimList;

            if (prefixes?.Count > 0)
            {
                prefixFilteredClaims = prefixFilteredClaims
                    .Where(r => prefixes.Any(prefix => r.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (suffixes?.Count > 0)
            {
                suffixFilteredClaims = suffixFilteredClaims
                    .Where(r => suffixes.Any(suffix => r.Value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return prefixFilteredClaims.Union(suffixFilteredClaims).Distinct(new ClaimComparer());
        }
    }
}