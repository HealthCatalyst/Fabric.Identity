using System.ComponentModel;

namespace Fabric.Identity.API.Models
{
    public class IdentityResource : BaseResource
    {
        /// <summary>
        /// Specifies whether the user can de-select the scope on the consent screen (if 
        /// the consent screen wants to implement such a feature). Defaults to false.
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// Specifies whether the consent screen will emphasize this scope (if the consent screen 
        /// wants to implement such a feature). Use this setting for sensitive or important scopes. 
        /// Defaults to false.
        /// </summary>
        public bool Emphasize { get; set; }

        /// <summary>
        ///  Specifies whether this scope is shown in the discovery document. Defaults to true.
        /// </summary>
        [DefaultValue(true)]
        public bool ShowInDiscoveryDocument { get; set; } = true;
    }
}