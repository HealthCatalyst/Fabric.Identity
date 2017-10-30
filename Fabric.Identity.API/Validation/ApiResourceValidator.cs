using Fabric.Identity.API.Services;

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

            RuleFor(apiResource => apiResource.Name)
                .Must(BeUnique)
                .When(apiResource => !string.IsNullOrEmpty(apiResource.Name))
                .WithMessage(a => $"Api resource {a.Name} already exists. Please provide a new name")
                .WithState(a => FabricIdentityEnums.ValidationState.Duplicate);
        }

        private bool BeUnique(string name)
        {
            return _documentDbService.GetDocument<ApiResource>(name).Result == null;
        }
    }
}
