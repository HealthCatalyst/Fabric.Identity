using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace Fabric.Identity.API.Models
{
    public static class ErrorFactory
    {
        public static Error CreateError<T>(ValidationResult validationResult, HttpStatusCode statusCode)
        {
            var error = validationResult.ToError();
            error.Code = Enum.GetName(typeof(HttpStatusCode), statusCode);
            error.Target = typeof(T).Name;
            return error;
        }

        public static Error CreateError<T>(string message, HttpStatusCode statusCode)
        {
            var error = new Error
            {
                Code = Enum.GetName(typeof(HttpStatusCode), statusCode),
                Target = typeof(T).Name,
                Message = message
            };
            return error;
        }
    }
}
