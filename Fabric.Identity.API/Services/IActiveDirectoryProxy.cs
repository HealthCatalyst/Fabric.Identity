using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Models;

namespace Fabric.Identity.API.Services
{
    public interface IActiveDirectoryProxy
    {
        IEnumerable<IDirectoryEntry> SearchDirectory(string ldapQuery);
        FabricPrincipal SearchForUser(string domain, string accountName);
    }
}
