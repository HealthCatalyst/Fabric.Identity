namespace Fabric.Identity.Client.Models
{
    /// <summary>
    /// The details of the error returned by Fabric.Identity. It will include the HttpStatus code in the Code property, and the message will have the details.
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the details.
        /// </summary>
        public Error[] Details { get; set; }

        /// <summary>
        /// Gets or sets the inner error.
        /// </summary>
        public InnerError Innererror { get; set; }
    }
}
