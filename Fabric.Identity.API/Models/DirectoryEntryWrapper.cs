using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.DirectoryServices;

namespace Fabric.Identity.API.Models
{
    public class DirectoryEntryWrapper : IDirectoryEntry
    {
        private Dictionary<string, string> Properties { get; }
        public string SchemaClassName { get; }

        public DirectoryEntryWrapper(DirectoryEntry directoryEntry)
        {
            Properties = new Dictionary<string, string>();

            SchemaClassName = directoryEntry.SchemaClassName;

            foreach (var property in PropertiesToSet)
            {
                var directoryEntryProperty = directoryEntry.Properties[property];
                Properties.Add(directoryEntryProperty.PropertyName.ToLower(), ReadUserEntryProperty(directoryEntryProperty));
            }
            directoryEntry.Dispose();
        }

        public string FirstName => Properties[GivenNameString];
        public string LastName => Properties[SnString];
        public string MiddleName => Properties[MiddleNameString];
        public string SamAccountName => Properties[SamAccountNameString];
        public string Name => Properties[NameString];
        public string Email => Properties[EmailString];

        private string ReadUserEntryProperty(PropertyValueCollection propertyValueCollection)
        {
            return propertyValueCollection.Value?.ToString() ?? string.Empty;
        }

        private static readonly IEnumerable<string> PropertiesToSet = new List<string>
        {
            GivenNameString,
            SnString,
            MiddleNameString,
            SamAccountNameString,
            NameString,
            EmailString
        };

        private const string GivenNameString = "givenname";
        private const string SnString = "sn";
        private const string MiddleNameString = "middlename";
        private const string SamAccountNameString = "samaccountname";
        private const string NameString = "name";
        private const string EmailString = "mail";
    }

    public interface IDirectoryEntry
    {
        string SchemaClassName { get; }
        string FirstName { get; }
        string LastName { get; }
        string MiddleName { get; }
        string SamAccountName { get; }
        string Name { get; }
        string Email { get; }
    }
}
