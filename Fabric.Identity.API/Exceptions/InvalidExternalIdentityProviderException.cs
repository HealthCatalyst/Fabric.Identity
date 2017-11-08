using System;

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

        public InvalidExternalIdentityProviderException(string message, Exception innerException) : base(message,
            innerException)
        {
        }
    }
}