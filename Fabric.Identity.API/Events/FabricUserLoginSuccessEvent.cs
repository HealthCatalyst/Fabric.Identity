using IdentityServer4.Events;

namespace Fabric.Identity.API.Events
{
    public class FabricUserLoginSuccessEvent : UserLoginSuccessEvent
    {
        public FabricUserLoginSuccessEvent(string provider, string providerUserId, string subjectId, string name, string clientId)
            : base(provider, providerUserId, subjectId, name, clientId: clientId)
        {
        }
    }
}
