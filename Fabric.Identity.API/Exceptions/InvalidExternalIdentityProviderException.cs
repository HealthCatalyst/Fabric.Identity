using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Exceptions
{
    public class InvalidExternalIdentityProviderException : Exception
    {
        public InvalidExternalIdentityProviderException()
        {
        }

        public InvalidExternalIdentityProviderException(string message) : base(message)
        {
        }

        public InvalidExternalIdentityProviderException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
