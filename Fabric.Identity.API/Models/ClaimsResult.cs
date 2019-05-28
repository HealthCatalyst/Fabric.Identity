// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http.Authentication;
using System.Collections.Generic;
using System.Security.Claims;

namespace Fabric.Identity.API.Models
{
    public class ClaimsResult
    {
        public string Provider { get; set; }
        public string UserId { get; set; }
        public string ClientId { get; set; }
        public AuthenticationProperties AuthenticationProperties { get; set; }
        public Claim[] AdditionalClaims { get; set; }
        public string SchemeItem { get; set; }
        public List<Claim> Claims { get; set; }
        public Claim UserIdClaim { get; set; }

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();

            foreach (var c in Claims)
            {
                sb.Append($"Type={c.Type}, Value={c.Value}, ");
            }

            foreach (var c in AdditionalClaims)
            {
                sb.Append($"Type={c.Type}, Value={c.Value}, ");
            }

            return $"Provider={Provider}, UserId={UserId}, ClientId={ClientId}, SchemeItem={SchemeItem}, {sb} ";
        }
    }
}