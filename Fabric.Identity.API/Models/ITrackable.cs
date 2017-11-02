using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    public interface ITrackable
    {
        DateTime CreatedDateTimeUtc { get; set; }
        DateTime? ModifiedDateTimeUtc { get; set; }
        string CreatedBy { get; set; }
        string ModifiedBy { get; set; }
    }
}
