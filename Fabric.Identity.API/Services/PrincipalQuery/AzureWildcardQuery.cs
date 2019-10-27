using System;
using System.Text.RegularExpressions;
using Fabric.Identity.API.Exceptions;

namespace Fabric.Identity.API.Services.PrincipalQuery
{
    public class AzureWildcardQuery : IAzureQuery
    {
        public string QueryText(string searchText, FabricIdentityEnums.PrincipalType principalType)
        {
            string inputText = Regex.Replace(searchText, @"\s+", " ").Trim();
            switch (principalType)
            {
                case FabricIdentityEnums.PrincipalType.User:
                    string surname = inputText;
                    string firstNameQuery = $"or startswith(GivenName, '{inputText}')";

                    // Check if user inputted two names and add query condition to eliminate matching last names
                    string[] names = inputText.Split(' ');
                    if (names.Length > 1)
                    {
                        surname = names[1];
                        firstNameQuery = $"and startswith(GivenName, '{names[0]}')";
                    }

                    return
                        $"startswith(DisplayName, '{inputText}') or startswith(UserPrincipalName, '{inputText}') or startswith(Surname, '{surname}') {firstNameQuery} or startswith(Mail, '{inputText}')";
                case FabricIdentityEnums.PrincipalType.Group:
                    return $"startswith(DisplayName, '{inputText}')";
                default:
                    throw new DirectorySearchException($"Query type {principalType} not supported in Azure AD.");
            }
        }
    }
}
