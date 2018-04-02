using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fabric.Identity.API.Models
{
    /// <summary>
    /// A model to represent the response from DiscoveryService
    /// </summary>
    public class DiscoveryServiceResponseModel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryServiceResponseModel"/> class.
        /// </summary>
        public DiscoveryServiceResponseModel()
        {
            Value = new List<DiscoveryServiceApiModel>();
        }

        /// <summary>
        /// Gets or sets the OData context.
        /// </summary>
        [JsonProperty("@odata.context")]
        public string Context { get; set; }

        /// <summary>
        /// Gets or sets the Value
        /// </summary>
        public List<DiscoveryServiceApiModel> Value { get; set; }
    }
}
