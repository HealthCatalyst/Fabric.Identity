using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Exceptions
{
    public class MissingUserClaimException : Exception
    {
        public MissingUserClaimException()
        {           
        }

        public MissingUserClaimException(string message) : base(message)
        {
        }

        public MissingUserClaimException(string message, Exception innerException) : base(message,
            innerException)
        {
        }
    }
}
