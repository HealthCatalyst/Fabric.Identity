using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.CouchDb
{
    public class CouchDbSetupException : Exception
    {
        public CouchDbSetupException(string message) : base(message)
        {
            
        }
    }
}
