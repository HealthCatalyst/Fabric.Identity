using Microsoft.Security.Application;

namespace Fabric.Identity.API.Services.PrincipalQuery
{
    public abstract class ActiveDirectoryQuery : IActiveDirectoryQuery
    {
        public abstract string GetFilter(string encodedSearchText);

        public virtual string QueryText(string searchText, FabricIdentityEnums.PrincipalType principalType)
        {
            var encodedSearchText = Encoder.LdapFilterEncode(searchText);
            var filter = GetFilter(encodedSearchText);
            var nameFilter = $"(|(sAMAccountName={filter})(givenName={filter})(sn={filter})(cn={filter})(mail={filter}))";
            return GetCategoryFilter(nameFilter, principalType);
        }

        protected virtual string GetCategoryFilter(string nameFilter, FabricIdentityEnums.PrincipalType principalType)
        {
            switch (principalType)
            {
                case FabricIdentityEnums.PrincipalType.User:
                    return $"(&(objectClass=user)(objectCategory=person){nameFilter})";
                case FabricIdentityEnums.PrincipalType.Group:
                    return $"(&(objectCategory=group){nameFilter})";
                default:
                    return $"(&(|(&(objectClass=user)(objectCategory=person))(objectCategory=group)){nameFilter})";
            }
        }
    }
}
