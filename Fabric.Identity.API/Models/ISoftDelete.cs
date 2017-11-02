using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    public interface ISoftDelete
    {
        bool IsDeleted { get; set; }
    }
}
