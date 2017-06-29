using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Services
{
    public interface IUserResolveService
    {
        string Username { get; }
        string ClientId { get; }
        string Subject { get; }
    }
}
