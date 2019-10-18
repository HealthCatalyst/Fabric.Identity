using System;
using System.Linq;
using Fabric.Identity.API;
using Fabric.Identity.API.Models;
using FluentValidation;

namespace Fabric.IdentityProviderSearchService.Validators
{
    public class SearchRequestValidator : AbstractValidator<PrincipalSearchRequest>
    {
        public SearchRequestValidator()
        {
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleSet(FabricIdentityConstants.ValidationRuleSets.PrincipalSearch, () =>
                RuleFor(request => request.SearchText)
                    .NotEmpty()
                    .WithMessage("Search text was not provided and is required."));

            RuleSet(FabricIdentityConstants.ValidationRuleSets.PrincipalIdentityProviderSearch, () =>
                RuleFor(request => request.IdentityProvider)
                    .Must(identityProvider =>
                        FabricIdentityConstants.SearchIdenityProviders.ValidIdentityProviders.Contains(identityProvider,
                            StringComparer.OrdinalIgnoreCase))
                    .When(request => !string.IsNullOrWhiteSpace(request.IdentityProvider))
                    .WithMessage($"Please specify a valid IdentityProvider. Valid identity providers include the following: {string.Join(", ", FabricIdentityConstants.SearchIdenityProviders.ValidIdentityProviders)}"));

            RuleSet(FabricIdentityConstants.ValidationRuleSets.PrincipalGroupSearch, () =>
                RuleFor(request => request.GroupName)
                    .NotEmpty()
                    .WithMessage("GroupName was not provided and is required."));

            RuleSet(FabricIdentityConstants.ValidationRuleSets.PrincipalSubjectSearch, () =>
                RuleFor(request => request.SubjectId)
                    .NotEmpty()
                    .WithMessage("Subject Id was not provided and is required."));
        }
    }
}