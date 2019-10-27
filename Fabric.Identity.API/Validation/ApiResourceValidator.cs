using Fabric.Identity.API.Persistence;
using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class ApiResourceValidator : AbstractValidator<ApiResource>
    {
        private readonly IApiResourceStore _apiResourceStore;

        public ApiResourceValidator(IApiResourceStore apiResourceStore)
        {
            _apiResourceStore = apiResourceStore;
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(apiResource => apiResource.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Api Resource");

            RuleFor(apiResource => apiResource.Scopes)
                .NotNull()
                .WithMessage("Please specify at least one Scope for this Api Resource");

            RuleForEach(apiResource => apiResource.Scopes)
                .SetValidator(new ScopeValidator())
                .WithMessage("Please specify at least one Scope for this Api Resource");

            RuleFor(apiResource => apiResource.UserClaims)
                .NotNull()
                .WithMessage("Please specify at least one User Claim for this Api Resource");

            RuleForEach(apiResource => apiResource.UserClaims)
                .NotEmpty()
                .WithMessage("Please ensure all User Claim items have a value for this Api Resource");

            RuleSet(
                FabricIdentityConstants.ValidationRuleSets.ApiResourcePost,
                () => RuleFor(apiResource => apiResource.Name)
                    .Must(BeUnique)
                    .When(apiResource => !string.IsNullOrEmpty(apiResource.Name))
                    .WithMessage(a => $"Api resource {a.Name} already exists. Please provide a new name")
                    .WithState(a => FabricIdentityEnums.ValidationState.Duplicate));
        }

        private bool BeUnique(string name)
        {
            return _apiResourceStore.GetResource(name) == null;
        }
    }
}