namespace Fabric.Identity.Client.Models
{
    /// <summary>
    /// The inner error.
    /// </summary>
    public class InnerError
    {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Gets or sets the inner error.
        /// </summary>
        public InnerError Innererror { get; set; }
    }
}