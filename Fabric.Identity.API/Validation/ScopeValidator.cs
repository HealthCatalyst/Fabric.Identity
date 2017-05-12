using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class ScopeValidator : AbstractValidator<Scope>
    {
        public ScopeValidator()
        {
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(scope => scope.Name)
                .NotEmpty()
                .WithMessage("Please specify a Name for this Scope");
        }
    }
}
