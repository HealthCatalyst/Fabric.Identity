namespace Fabric.Identity.Client.Models
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The model for registering API's with Fabric.Identity
    /// </summary>
    public class ApiResource
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiResource"/> class.
        /// </summary>
        /// <param name="name">
        /// The API name.
        /// </param>
        /// <param name="userClaims">
        /// The list of user claims that should be included on the access token that gets passed to the API.
        /// </param>
        /// <param name="scopes">
        /// The list of scopes to register for the API.
        /// </param>
        public ApiResource(string name, ICollection<string> userClaims, ICollection<string> scopes)
        {
            this.Name = name;
            this.UserClaims = userClaims;
            this.Scopes = scopes.Select(s => new Scope { Name = s, DisplayName = s }).ToList();
        }

        /// <summary>
        /// Gets or sets the Unique name of the API
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the DisplayName of the API, will be used in consent screens and discovery document
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Description of the API, will be used in consent screens and discovery document
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the list of user claims that should be included on the access token
        /// </summary>
        public ICollection<string> UserClaims { get; set; }

        /// <summary>
        /// Gets or sets the API secret that is generated for the API upon registration
        /// </summary>
        public string ApiSecret { get; set; }

        /// <summary>
        /// Gets or sets the scopes that the API defines
        /// </summary>
        public ICollection<Scope> Scopes { get; set; }
    }
}
