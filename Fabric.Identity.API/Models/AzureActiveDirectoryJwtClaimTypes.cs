using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    /// <summary>
    /// These are custom Jwt Claims for Azure AD
    /// For more information: https://docs.microsoft.com/en-us/azure/active-directory/develop/access-tokens
    /// </summary>
    public static class AzureActiveDirectoryJwtClaimTypes
    {
        /// <summary>
        /// The immutable identifier for an object in the Microsoft identity platform, in this case, a user account. 
        /// For more information, visit the docs url on the class documentation.
        /// </summary>
        public const string OID = "oid";

        /// <summary>
        /// While the claim "oid" is present on the access token, the ASP.net OpenID Connect Middleware will
        /// change the name of the token to below.
        /// 
        /// For more information, visit https://docs.microsoft.com/en-us/azure/architecture/multitenant-identity/claims
        /// </summary>
        public const string OID_Alternative = "http://schemas.microsoft.com/identity/claims/objectidentifier";
    }
}
