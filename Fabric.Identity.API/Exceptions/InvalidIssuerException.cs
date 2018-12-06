using System;

namespace Fabric.Identity.API.Exceptions
{
    public class InvalidIssuerException : Exception
    {
        public InvalidIssuerException()
        {
        }
        public InvalidIssuerException(string message) : base(message)
        {
        }

        public InvalidIssuerException(string message, Exception innerException) : base(message,
            innerException)
        {
        }

        public string LogMessage { get; set; }
    }
}
