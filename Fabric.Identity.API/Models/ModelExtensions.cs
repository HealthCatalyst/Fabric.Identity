using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation.Results;
using IS4 = IdentityServer4.Models;

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

        public static Client ToClientViewModel(this IS4.Client client)
        {
            var newClient = new Client()
            {
                Id = client.ClientId,
                Name = client.ClientName,
                AllowedScopes = client.AllowedScopes,
                AllowedGrantTypes = client.AllowedGrantTypes,
                AllowedCorsOrigins = client.AllowedCorsOrigins
            };

            return newClient;
        }

    }
}
