using System.ComponentModel;
using System.Security.Cryptography.X509Certificates;
using Fabric.Identity.API.CouchDb;
using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class ApiResourceValidator : AbstractValidator<ApiResource>
    {
        private readonly IDocumentDbService _documentDbService;

        public ApiResourceValidator(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(apiResource => apiResource.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Api Resource");

            RuleFor(apiResource => apiResource.Name)
                .Must(BeUnique)
                .When(apiResource => !string.IsNullOrEmpty(apiResource.Name));

            RuleFor(apiResource => apiResource.Scopes)
                .NotNull()
                .SetCollectionValidator(new ScopeValidator())
                .WithMessage("Please specify at least one Scope for this Api Resource");

            RuleFor(apiResource => apiResource.UserClaims)
                .NotNull()
                .WithMessage("Please specify at least one Uesr Claim for this Api Resource");

            RuleForEach(apiResource => apiResource.UserClaims)
                .NotEmpty()
                .WithMessage("Please ensure all User Claim items have a value for this Api Resource");
        }

        private bool BeUnique(string apiResourceName)
        {
            return _documentDbService.GetDocument<ApiResource>(apiResourceName) == null;
        }

    }
}
