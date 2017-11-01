using System.Linq;
using System.Reflection;
using Fabric.Identity.API.Persistence;
using FluentValidation;
using IdentityServer4.Models;

namespace Fabric.Identity.API.Validation
{
    public class ClientValidator : AbstractValidator<Client>
    {
        private readonly IClientManagementStore _clientManagementStore;

        public ClientValidator(IClientManagementStore clientManagementStore)
        {
            _clientManagementStore = clientManagementStore;
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
                .NotEmpty()
                .WithMessage("Please specify at least one Allowed Scope for this client");

            RuleFor(client => client.AllowedCorsOrigins)
                .NotNull()
                .NotEmpty()
                .When(client => client.AllowedGrantTypes.Contains(GrantType.Implicit))
                .WithMessage("Please specify at least one Allowed Cors Origin when using Implicit grant type");

            RuleFor(client => client.AllowOfflineAccess)
                .NotNull()
                .Must(v => !v)
                .When(client => client.AllowedGrantTypes.Contains(GrantType.Implicit) ||
                                client.AllowedGrantTypes.Contains(GrantType.ResourceOwnerPassword))
                .WithMessage(
                    "Client may not have Allow Offline Access when grant type is Implicit or ResourceOwnerPassword");

            var grantTypes = typeof(GrantType).GetFields(BindingFlags.Public | BindingFlags.Static)
                .Where(f => f.FieldType == typeof(string))
                .Select(f => f.GetValue(null).ToString());

            RuleFor(client => client.AllowedGrantTypes)
                .NotNull()
                .NotEmpty()
                .Must(xs => xs.All(x => grantTypes.Contains(x)))
                .WithMessage("Grant type not allowed. Allowed values: " +
                             grantTypes.Aggregate((acc, x) => $"{acc} ,{x}"));

            RuleFor(client => client.ClientId)
                .Must(BeUnique)
                .When(client => !string.IsNullOrEmpty(client.ClientId))
                .WithMessage(c => $"Client {c.ClientId} already exists. Please provide a new client id")
                .WithState(c => FabricIdentityEnums.ValidationState.Duplicate);
        }

        private bool BeUnique(string clientId)
        {
            return _clientManagementStore.FindClientByIdAsync(clientId).Result == null;
        }
    }
}