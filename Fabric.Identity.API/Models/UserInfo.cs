using IdentityServer4.Test;

namespace Fabric.Identity.API.Models
{
    public class UserInfo
    {
        public string SubjectId { get; set; }
        public string Username { get; set; }

        public UserInfo(TestUser user)
        {
            SubjectId = user.SubjectId;
            Username = user.Username;
        }

        public UserInfo(User user)
        {
            SubjectId = user.SubjectId;
            Username = user.Username;
        }
    }
}
