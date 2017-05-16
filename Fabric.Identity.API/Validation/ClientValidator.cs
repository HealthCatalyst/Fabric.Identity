using System.Linq;
using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class ClientValidator : AbstractValidator<Client>
    {
        public ClientValidator()
        {     
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(client => client.ClientId)
                .NotEmpty()
                .WithMessage("Please specify an Id for this client");

            RuleFor(client => client.ClientName)
                .NotEmpty()
                .WithMessage("Please specify a Name for this client");

            RuleFor(client => client.AllowedScopes)
                .NotNull()
                .WithMessage("Please specify at least one Allowed Scope for this client");

            RuleFor(client => client.AllowedCorsOrigins)
                .NotNull()
                .When(client => client.AllowedGrantTypes.Contains(GrantType.Implicit))
                .WithMessage("Please specify at least one Allowed Cors Origin when using implicit grant type");

        }
    }
}
