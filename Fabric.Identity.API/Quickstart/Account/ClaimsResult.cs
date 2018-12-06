// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http.Authentication;
using System.Security.Claims;

namespace Fabric.Identity.API.Quickstart.Account
{
    public class ClaimsResult
    {
        public string Provider { get; internal set; }
        public string UserId { get; internal set; }
        public string ClientId { get; internal set; }
        public AuthenticationProperties AuthenticationProperties { get; internal set; }
        public Claim[] AdditionalClaims { get; internal set; }
    }
}