using Newtonsoft.Json;

namespace Fabric.Identity.API.Configuration
{
    public class FilterSettings
    {
        [JsonProperty(PropertyName = "Groups")]
        public GroupFilterSettings GroupFilterSettings { get; set; }
    }
}