using Fabric.Identity.API.Persistence;
using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class IdentityResourceValidator : AbstractValidator<IdentityResource>
    {
        private readonly IIdentityResourceStore _identityResourceStore;

        public IdentityResourceValidator(IIdentityResourceStore identityResourceStore)
        {
            _identityResourceStore = identityResourceStore;
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(identityResource => identityResource.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Identity Resource");

            RuleFor(identityResource => identityResource.UserClaims)
                .NotNull()
                .WithMessage("Please specify at least one User Claim for this Identity Resource");

            RuleForEach(identityResource => identityResource.UserClaims)
                .NotEmpty()
                .WithMessage("Please ensure all User Claim items have a value for this Identity Resource");

            RuleFor(identityResource => identityResource.Name)
                .Must(BeUnique)
                .When(identityResource => !string.IsNullOrEmpty(identityResource.Name))
                .WithMessage(i => $"Identity resource {i.Name} already exists. Please provide a new name")
                .WithState(i => FabricIdentityEnums.ValidationState.Duplicate);
        }

        private bool BeUnique(string name)
        {
            return _identityResourceStore.GetResource(name) == null;
        }
    }
}