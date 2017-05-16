using Fabric.Identity.API.Models;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Net;

namespace Fabric.Identity.API.Management
{
    public abstract class BaseController<T> : Controller
    {
        protected readonly AbstractValidator<T> Validator;
        protected readonly ILogger Logger;

        protected BaseController(AbstractValidator<T> validator, ILogger logger)
        {
            Validator = validator;
            Logger = logger;
        }

        protected ValidationResult Validate(T model)
        {
            var validationResults = Validator.Validate(model);
            if (!validationResults.IsValid)
            {
                Logger.Information("Validation failed for model: {@model}. ValidationResults: {@validationResults}.",
                    model, validationResults);
            }

            return validationResults;
        }

        protected IActionResult CreateValidationFailureResponse(ValidationResult validationResult)
        {
            var error = ErrorFactory.CreateError<T>(validationResult, HttpStatusCode.BadRequest);
            return BadRequest(error);
        }

        protected IActionResult CreateFailureResponse(string message, HttpStatusCode statusCode)
        {
            var error = ErrorFactory.CreateError<T>(message, statusCode);
            return Json(error);
        }
    }
}
