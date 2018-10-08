using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Exceptions
{
    public class MissingIssuerClaimException : Exception
    {
        public MissingIssuerClaimException()
        {            
        }
        public MissingIssuerClaimException(string message) : base(message)
        {
        }

        public MissingIssuerClaimException(string message, Exception innerException) : base(message,
            innerException)
        {
        }
    }
}
