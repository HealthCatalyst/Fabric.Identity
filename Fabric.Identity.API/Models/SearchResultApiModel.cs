using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Models
{
    public class SearchResultApiModel<T>
    {
        public ICollection<T> Principals { get; set; }
        public int ResultCount { get; set; }
    }
}
