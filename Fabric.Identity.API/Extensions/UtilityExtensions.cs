using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Extensions
{
    public static class UtilityExtensions
    {
        public static string FormatUrl(this string url)
        {
            return !url.EndsWith("/") ? $"{url}/" : url;
        }
    }
}
