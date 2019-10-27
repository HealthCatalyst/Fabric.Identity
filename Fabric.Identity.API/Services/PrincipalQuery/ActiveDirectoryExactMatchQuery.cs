namespace Fabric.Identity.API.Services.PrincipalQuery
{
    public class ActiveDirectoryExactMatchQuery : ActiveDirectoryQuery
    {
        public override string GetFilter(string encodedSearchText)
        {
            return encodedSearchText;
        }
    }
}
