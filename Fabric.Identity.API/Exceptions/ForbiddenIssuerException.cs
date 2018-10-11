using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Exceptions
{
    public class ForbiddenIssuerException : Exception
    {
        public ForbiddenIssuerException()
        {            
        }

        public ForbiddenIssuerException(string message) : base(message)
        {
        }

        public ForbiddenIssuerException(string message, Exception innerException) : base(message,
            innerException)
        {
        }
    }
}
