using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    public class ExternalProviderApiModel
    {
        public string DisplayName { get; set; }
        public string AuthenticationScheme { get; set; }
    }
}
