namespace Fabric.Identity.API.Extensions
{
    public static class UtilityExtensions
    {
        public static string EnsureTrailingSlash(this string url)
        {
            return !url.EndsWith("/") ? $"{url}/" : url;
        }
    }
}