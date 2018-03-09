namespace Fabric.Identity.Client.Models
{
    /// <summary>
    /// The client registration response.
    /// </summary>
    public class ClientRegistrationResponse
    {
        /// <summary>
        /// Gets or sets a value indicating whether there is an error.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Gets or sets the client.
        /// </summary>
        public Client Client { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        public Error Error { get; set; }
    }
}
