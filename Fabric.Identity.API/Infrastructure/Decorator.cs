using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fabric.Identity.API.Infrastructure
{
    public class Decorator<TService>
    {
        public TService Instance { get; set; }

        public Decorator(TService instance)
        {
            Instance = instance;
        }
    }

    public class Decorator<TService, TImplementation> : Decorator<TService> where TImplementation : class, TService
    {
        public Decorator(TImplementation instance) : base(instance)
        {
        }
    }
}
