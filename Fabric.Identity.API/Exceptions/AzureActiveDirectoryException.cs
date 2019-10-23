using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Exceptions
{
    public class AzureActiveDirectoryException : Exception
    {
        public AzureActiveDirectoryException()
        {
        }

        public AzureActiveDirectoryException(string message) : base(message)
        {
        }

        public AzureActiveDirectoryException(string message, Exception inner) : base(message, inner)
        {

        }
    }
}
