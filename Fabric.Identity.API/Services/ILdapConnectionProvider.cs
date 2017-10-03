using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Novell.Directory.Ldap;

namespace Fabric.Identity.API.Services
{
    public interface ILdapConnectionProvider
    {
        ILdapConnection GetConnection();
        string BaseDn { get; }
    }
}
