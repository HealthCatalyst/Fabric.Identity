using System;

namespace Fabric.Identity.API.Models
{
    public class UserApiModel
    {
        public string SubjectId { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string LastName { get; set; }
    }
}