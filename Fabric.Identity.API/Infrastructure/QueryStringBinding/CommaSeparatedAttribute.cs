using System;

namespace Fabric.Identity.API.Infrastructure.QueryStringBinding
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = true, AllowMultiple = false)]
    public class CommaSeparatedAttribute : Attribute
    {
    }
}
