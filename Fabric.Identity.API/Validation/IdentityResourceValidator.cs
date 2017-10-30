using Fabric.Identity.API.Services;

using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class IdentityResourceValidator : AbstractValidator<IdentityResource>
    {
        private readonly IDocumentDbService _documentDbService;

        public IdentityResourceValidator(IDocumentDbService documentDbService)
        {
            this._documentDbService = documentDbService;
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(identityResource => identityResource.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Identity Resource");

            RuleFor(identityResource => identityResource.UserClaims)
                .NotNull()
                .WithMessage("Please specify at least one Uesr Claim for this Identity Resource");

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
            return _documentDbService.GetDocument<IdentityResource>(name).Result == null;
        }
    }
}
