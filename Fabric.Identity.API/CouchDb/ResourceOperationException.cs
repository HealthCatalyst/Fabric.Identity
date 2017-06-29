using System;

namespace Fabric.Identity.API.CouchDb
{
    public class ResourceOperationException : Exception
    {
        public ResourceOperationException()
        {
        }

        public ResourceOperationException(string message) 
            : base(message)
        {
        }

        public ResourceOperationException(string message, ResourceOperationType operationType) 
            : base(ConstructMessage(message, operationType))
        {
        }

        public ResourceOperationException(string message, ResourceOperationType operationType, Exception innerException) 
            : base(ConstructMessage(message, operationType), innerException)
        {
        }

        private static string ConstructMessage(string message, ResourceOperationType operationType)
        {
            return $"Resource operation: {operationType} - Failure reason: {message}";
        }
    }

    public enum ResourceOperationType
    {
        Add,
        Update,
        Delete
    }
}
