using Fabric.Identity.API.Exceptions;

namespace Fabric.Identity.API.Services.PrincipalQuery
{
    public class AzureExactMatchQuery : IAzureQuery
    {
        public string QueryText(string searchText, FabricIdentityEnums.PrincipalType principalType)
        {
            switch (principalType)
            {
                case FabricIdentityEnums.PrincipalType.User:
                    return
                        $"DisplayName eq '{searchText}' or GivenName eq '{searchText}' or UserPrincipalName eq '{searchText}' or Surname eq '{searchText}' or Mail eq '{searchText}'";
                case FabricIdentityEnums.PrincipalType.Group:
                    return $"DisplayName eq '{searchText}'";
                default:
                    throw new DirectorySearchException($"Query type {principalType} not supported in Azure AD.");
            }
        }
    }
}
