using System.Linq;
using Fabric.Identity.API.CouchDb;
using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class ClientValidator : AbstractValidator<Client>
    {
        private readonly IDocumentDbService _documentDbService;

        public ClientValidator(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
            ConfigureRules();
        }

        private void ConfigureRules()
        {
            RuleFor(client => client.ClientId)
                .NotEmpty()
                .WithMessage("Please specify an Id for this client");

            RuleFor(client => client.ClientId)
                .Must(BeUnique)
                .When(client => !string.IsNullOrEmpty(client.ClientId));

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

        private bool BeUnique(string clientId)
        {
            return _documentDbService.GetDocument<Client>(clientId) == null;
        }
    }
}
