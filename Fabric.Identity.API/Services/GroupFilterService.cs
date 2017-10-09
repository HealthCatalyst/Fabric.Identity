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
        private readonly GroupFilterSettings _groupFilterSettings;

        public GroupFilterService(GroupFilterSettings groupFilterSettings)
        {
            _groupFilterSettings = groupFilterSettings;
        }

        public IEnumerable<Claim> FilterClaims(IEnumerable<Claim> claims)
        {
            if (_groupFilterSettings == null)
            {
                return claims;
            }

            var prefixes = _groupFilterSettings.Prefixes?.ToList();
            var suffixes = _groupFilterSettings.Suffixes?.ToList();
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