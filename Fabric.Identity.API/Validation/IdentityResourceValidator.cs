using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class IdentityResourceValidator : AbstractValidator<IdentityResource>
    { 
        public IdentityResourceValidator()
        {
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
        }
    }
}
