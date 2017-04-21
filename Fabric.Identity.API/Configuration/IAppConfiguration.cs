using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Platform.Shared.Configuration;

namespace Fabric.Identity.API.Configuration
{
    public interface IAppConfiguration
    {
        ElasticSearchSettings ElasticSearchSettings { get; }
        HostingOptions HostingOptions { get; }
    }
}
