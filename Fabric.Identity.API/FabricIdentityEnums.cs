namespace Fabric.Identity.API
{
    public class FabricIdentityEnums
    {
        public enum ValidationState
        {
            Duplicate,
            MissingRequiredField            
        }

        public enum PrincipalType
        {
            User,
            Group,
            UserAndGroup
        }
    }
}
