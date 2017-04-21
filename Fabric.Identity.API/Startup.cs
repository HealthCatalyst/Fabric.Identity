using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.EventSinks;
using Fabric.Platform.Logging;
using IdentityServer4;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Fabric.Identity.API
{
    public class Startup
    {
        private IAppConfiguration _appConfig;
        public Startup(IHostingEnvironment env)
        {
            _appConfig = new Configuration.ConfigurationProvider().GetAppConfiguration(env.ContentRootPath);
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IEventSink, ElasticSearchEventSink>();
            services.AddSingleton(_appConfig);
            services
                .AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                })
                .AddTemporarySigningCredential()
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddInMemoryClients(Config.GetClients())
                .AddInMemoryApiResources(Config.GetApiResources())
                .AddTestUsers(TestUsers.Users);

            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            var levelSwitch = new LoggingLevelSwitch();
            var logger = LogFactory.CreateLogger(levelSwitch, _appConfig.ElasticSearchSettings, "identityservice");
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }

            loggerFactory.AddSerilog(logger);

            app.UseIdentityServer();

            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme,
                SignOutScheme = IdentityServerConstants.SignoutScheme,

                DisplayName = "Azure Active Directory",
                Authority = "https://login.microsoftonline.com/common",
                ClientId = "0f1ee72e-1d9c-45ac-9948-0a837ba12950",

                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false
                }
            });
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
            app.UseOwin()
                .UseFabricMonitoring(() => Task.FromResult(true), levelSwitch);
        }
    }
}
