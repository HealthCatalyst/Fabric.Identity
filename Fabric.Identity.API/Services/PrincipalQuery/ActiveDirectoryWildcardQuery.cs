namespace Fabric.Identity.API.Services.PrincipalQuery
{
    public class ActiveDirectoryWildcardQuery : ActiveDirectoryQuery
    {
        public override string GetFilter(string encodedSearchText)
        {
            return $"{encodedSearchText}*";
        }
    }
}
