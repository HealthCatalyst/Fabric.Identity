using System;
using System.Linq;
using System.Net;
using Fabric.Identity.API.Models;
using FluentValidation;
using FluentValidation.Results;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Serilog;

namespace Fabric.Identity.API.Management
{
    public abstract class BaseController<T> : Controller where T : class
    {
        protected const string BadRequestErrorMsg = "The request has invalid or missing values.";
        protected const string DuplicateErrorMsg = "A duplicate resource is attempting to be added";
        protected readonly ILogger Logger;
        protected readonly AbstractValidator<T> Validator;

        protected BaseController(AbstractValidator<T> validator, ILogger logger)
        {
            Validator = validator;
            Logger = logger;
        }

        public Func<string> GeneratePassword { get; set; } =
            () => Convert.ToBase64String(Guid.NewGuid().ToByteArray()).Substring(0, 16);

        protected Secret GetNewSecret(string password)
        {
            return new Secret(password.Sha256());
        }

        protected virtual IActionResult ValidateAndExecute(T model, Func<IActionResult> successFunctor, string ruleSet)
        {
            // FluentValidation cannot handle null models.
            if (model == null)
            {
                Logger.Information($"Input \"{typeof(T)}\" is nonexistent or malformed.");
                return CreateFailureResponse($"Input \"{typeof(T)}\" is nonexistent or malformed.",
                    HttpStatusCode.BadRequest);
            }

            var validationResults = Validator.Validate(model, ruleSet: ruleSet);

            if (!validationResults.IsValid)
            {
                Logger.Information($"Validation failed for model: {model}. ValidationResults: {validationResults}.");
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

        protected virtual IActionResult ValidateAndExecute(T model, Func<IActionResult> successFunctor)
        {
            return ValidateAndExecute(model, successFunctor, FabricIdentityConstants.ValidationRuleSets.Default);
        }

        protected IActionResult CreateValidationFailureResponse(ValidationResult validationResult)
        {
            var statusCode = HttpStatusCode.BadRequest;

            if (validationResult.Errors.Any(e => e.CustomState != null &&
                                                 e.CustomState.Equals(FabricIdentityEnums.ValidationState.Duplicate)))
            {
                statusCode = HttpStatusCode.Conflict;
            }

            var error = ErrorFactory.CreateError<T>(validationResult, statusCode);

            return new ObjectResult(error) {StatusCode = (int) statusCode};
        }

        protected IActionResult CreateFailureResponse(string message, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            switch (statusCode)
            {
                case HttpStatusCode.NotFound: return NotFound(error);
                case HttpStatusCode.BadRequest: return BadRequest(error);
                default: return Json(error);
            }
        }
    }
}