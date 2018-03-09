namespace Fabric.Identity.Client.Models
{
    /// <summary>
    /// The model for representing an API's scopes
    /// </summary>
    public class Scope
    {
        /// <summary>
        /// Gets or sets the unique name of the scope
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the DisplayName of the scope, shown on the consent screen
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Description of the scope, shown on the consent screen
        /// </summary>
        public string Description { get; set; }
    }
}