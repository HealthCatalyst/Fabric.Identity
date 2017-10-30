using Fabric.Identity.API.Models;
using FluentValidation;
using FluentValidation.Results;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;

using IdentityModel.Client;

namespace Fabric.Identity.API.Management
{
    public abstract class BaseController<T> : Controller where T : class
    {
        protected const string BadRequestErrorMsg = "The request has invalid or missing values.";

        public Func<string> GeneratePassword { get; set; } = () => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 16);
        protected readonly AbstractValidator<T> Validator;
        protected readonly ILogger Logger;

        protected Secret GetNewSecret(string password)
        {            
            return new Secret(HashExtensions.Sha256(password));
        }

        protected BaseController(AbstractValidator<T> validator, ILogger logger)
        {
            this.Validator = validator;
            this.Logger = logger;
        }

        protected virtual IActionResult ValidateAndExecute(T model, Func<IActionResult> successFunctor)
        {
            // FluentValidation cannot handle null models.
            if (model == null)
            {
                this.Logger.Information($"Input \"{typeof(T)}\" is nonexistent or malformed.");
                return CreateFailureResponse($"Input \"{typeof(T)}\" is nonexistent or malformed.", HttpStatusCode.BadRequest);
            }

            var validationResults = this.Validator.Validate(model);

            if (!validationResults.IsValid)
            {
                this.Logger.Information($"Validation failed for model: {model}. ValidationResults: {validationResults}.");
                return CreateValidationFailureResponse(validationResults);
            }

            // Validation passed.
            try
            {
                return successFunctor();
            }
            catch (Exception e)
            {
                return CreateFailureResponse(e.Message, HttpStatusCode.BadRequest);
            }
        }

        protected IActionResult CreateValidationFailureResponse(ValidationResult validationResult)
        {
            var statusCode = HttpStatusCode.BadRequest;
        
            if (validationResult.Errors.Any(e => e.CustomState != null && e.CustomState.Equals(FabricIdentityEnums.ValidationState.Duplicate)))
            {
                statusCode = HttpStatusCode.Conflict;
            }

            var error = ErrorFactory.CreateError<T>(validationResult, statusCode);

            return new ObjectResult(error){ StatusCode = (int)statusCode};
        }

        protected IActionResult CreateFailureResponse(string message, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            switch(statusCode)
            {
                case HttpStatusCode.NotFound: return NotFound(error);
                case HttpStatusCode.BadRequest: return BadRequest(error);
                default: return Json(error);
            }
        }
    }
}
