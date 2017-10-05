// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System;
using Fabric.Identity.API;

namespace IdentityServer4.Quickstart.UI
{
    public class AccountOptions
    {
        public static bool AllowLocalLogin = false;
        public static bool AllowRememberLogin = true;
        public static TimeSpan RememberMeLoginDuration = TimeSpan.FromDays(30);

        public static bool ShowLogoutPrompt = true;
        public static bool AutomaticRedirectAfterSignOut = true;

        // to enable windows authentication, the host (IIS or IIS Express) also must have 
        // windows auth enabled.
        public static bool WindowsAuthenticationEnabled = true;
        public static bool IncludeWindowsGroups = true;
        // specify the Windows authentication schemes you want to use for authentication
        public static readonly string[] WindowsAuthenticationSchemes = new string[] { "Negotiate", "NTLM" };
        public static readonly string WindowsAuthenticationProviderName = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows;
        public static readonly string WindowsAuthenticationDisplayName = FabricIdentityConstants.FabricExternalIdentityProviderTypes.Windows;

        public static string InvalidCredentialsErrorMessage = "Invalid username or password";
    }
}
