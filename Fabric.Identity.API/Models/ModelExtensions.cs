using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;

namespace Fabric.Identity.API.Models
{
    public static class ModelExtensions
    {
        public static Error ToError(this ValidationResult validationResult)
        {
            var details = validationResult.Errors.Select(validationResultError => new Error
                {
                    Code = validationResultError.ErrorCode,
                    Message = validationResultError.ErrorMessage,
                    Target = validationResultError.PropertyName
                })
                .ToList();

            var error = new Error
            {
                Message = details.Count > 1 ? "Multiple Errors" : details.FirstOrDefault().Message,
                Details = details.ToArray()
            };

            return error;
        }

    }
}
