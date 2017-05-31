using Fabric.Identity.API.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Net;

namespace Fabric.Identity.API.Management
{
    public abstract class BaseController<T> : Controller
    {
        protected readonly AbstractValidator<T> Validator;
        protected readonly ILogger Logger;

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
                Logger.Information($"Input \"{typeof(T)}\" is null.");
                return CreateFailureResponse($"Input \"{typeof(T)}\" is null.", HttpStatusCode.BadRequest);
            }

            var validationResults = Validator.Validate(model);

            if (!validationResults.IsValid)
            {
                Logger.Information($"Validation failed for model: {model}. ValidationResults: {validationResults}.");
                return CreateValidationFailureResponse(validationResults);
            }

            try
            {
                // Validation passed.
                return successFunctor();
            }
            catch (Exception e)
            {
                return CreateFailureResponse(e.Message, HttpStatusCode.BadRequest);
            }
        }

        protected IActionResult CreateValidationFailureResponse(ValidationResult validationResult)
        {
            var error = ErrorFactory.CreateError<T>(validationResult, HttpStatusCode.BadRequest);
            return BadRequest(error);
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
