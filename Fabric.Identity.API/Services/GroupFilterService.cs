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

            var prefixFilters = _groupFilterSettings.Prefixes?.ToList();
            var suffixFilters = _groupFilterSettings.Suffixes?.ToList();

            var hasPrefixFilters = prefixFilters != null && prefixFilters.Count > 0;
            var hasSuffixFilters = suffixFilters != null && suffixFilters.Count > 0;
            var hasFilters = hasPrefixFilters || hasSuffixFilters;

            if (!hasFilters)
            {
                return claims;
            }

            var claimList = claims.ToList();
            var prefixFilteredClaims = new List<Claim>();
            var suffixFilteredClaims = new List<Claim>();

            if (prefixFilters?.Count > 0)
            {
                prefixFilteredClaims = claimList
                    .Where(r => prefixFilters.Any(prefix => r.Value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            if (suffixFilters?.Count > 0)
            {
                suffixFilteredClaims = claimList
                    .Where(r => suffixFilters.Any(suffix => r.Value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
            }

            return prefixFilteredClaims.Union(suffixFilteredClaims).Distinct(new ClaimComparer());
        }
    }
}