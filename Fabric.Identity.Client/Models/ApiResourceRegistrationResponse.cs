namespace Fabric.Identity.Client.Models
{
    /// <summary>
    /// The API resource registration response.
    /// </summary>
    public class ApiResourceRegistrationResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether there is an error.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Gets or sets the API resource.
        /// </summary>
        public ApiResource ApiResource { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public Error Error { get; set; }
        
    }
}
