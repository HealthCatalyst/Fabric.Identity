using System.Collections.Generic;

namespace Fabric.Identity.API.Configuration
{
    public class GroupFilterSettings
    {
        public IEnumerable<string> Prefixes { get; set; }
        public IEnumerable<string> Suffixes { get; set; }
    }
}