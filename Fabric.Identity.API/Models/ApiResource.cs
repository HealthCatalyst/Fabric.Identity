using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Fabric.Identity.API.Models
{
    public class ApiResource : BaseResource
    {
        /// <summary>
        /// The API secret is used for the introspection endpoint. The API can authenticate 
        /// with introspection using the API name and secret.
        /// </summary>
        public string ApiSecret { get; set; }

        /// <summary>
        /// An API must have at least one scope. Each scope can have different settings.
        /// </summary>
        [Required]
        public ICollection<Scope> Scopes { get; set; }
    }

    public class Scope
    {
        /// <summary>
        /// Name of the scope. This is the value a client will use to request the scope.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Display name. This value will be used e.g. on the consent screen.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Description. This value will be used e.g. on the consent screen.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Specifies whether the user can de-select the scope on the consent screen. 
        /// Defaults to false.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Specifies whether the consent screen will emphasize this scope. Use this setting 
        /// for sensitive or important scopes. Defaults to false.
        /// </summary>
        public bool Emphasize { get; set; }

        /// <summary>
        /// Specifies whether this scope is shown in the discovery document. Defaults to true.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowInDiscoveryDocument { get; set; } = true;

        /// <summary>
        /// List of user claims that should be included in the access token.
        /// </summary>
        public ICollection<string> UserClaims { get; set; } = new HashSet<string>();
    }
}