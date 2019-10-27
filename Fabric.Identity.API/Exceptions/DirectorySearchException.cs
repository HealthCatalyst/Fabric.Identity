using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Exceptions
{
    public class DirectorySearchException : Exception
    {
        public DirectorySearchException()
        {
        }

        public DirectorySearchException(string message) : base(message)
        {
        }

        public DirectorySearchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DirectorySearchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
