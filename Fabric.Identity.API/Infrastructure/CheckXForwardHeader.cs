using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
namespace Fabric.Identity.API.Infrastructure;
{
    public class CheckXForwardHeader
    {
        private readonly RequestDelegate _next;

        public CheckXForwardHeader(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var prefix = context.Request.Headers["x-forwarded-prefix"];

            if (!StringValues.IsNullOrEmpty(prefix))
            {
                context.Request.PathBase = PathString.FromUriComponent(prefix.ToString());
                context.Items["prefix"] = prefix;

                await _next(context);

                // TODO: subtract PathBase from Path if needed.
            }
            await _next(context);
        }
    }

    //Extension method used to add the middleware to the HTTP request pipeline.
    public static class CheckXForwardHeaderExtensions
    {
        public static IApplicationBuilder UseCheckXForwardHeader(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CheckXForwardHeader>();


        }
    }
}
