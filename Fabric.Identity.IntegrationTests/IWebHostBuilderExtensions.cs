using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Hosting;

namespace Fabric.Identity.IntegrationTests
{
    public static class IWebHostBuilderExtensions
    {
        public static IWebHostBuilder AddServices(this IWebHostBuilder builder, Action<IWebHostBuilder> addServices)
        {
            addServices?.Invoke(builder);

            return builder;
        }
    }
}
