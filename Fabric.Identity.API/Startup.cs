using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.CouchDb;
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
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Serilog.ILogger;

namespace Fabric.Identity.API
{
    public class Startup
    {
        private readonly IAppConfiguration _appConfig;
        private readonly ILogger _logger;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;
        private readonly ICouchDbSettings _couchDbSettings;

        public Startup(IHostingEnvironment env)
        {
            _appConfig = new ConfigurationProvider().GetAppConfiguration(env.ContentRootPath);
            _loggingLevelSwitch = new LoggingLevelSwitch();
            _logger = LogFactory.CreateLogger(_loggingLevelSwitch, _appConfig.ElasticSearchSettings, "identityservice");
            _couchDbSettings = _appConfig.CouchDbSettings;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IEventSink, ElasticSearchEventSink>();
            services.AddSingleton<IDocumentDbService, CouchDbAccessService>();
            services.AddSingleton(_appConfig);           
            services.AddSingleton(_logger);
            services.AddSingleton(_couchDbSettings);
            services
                .AddIdentityServer(options =>
                {
                    options.Events.RaiseSuccessEvents = true;
                    options.Events.RaiseFailureEvents = true;
                    options.Events.RaiseErrorEvents = true;
                })
                .AddTemporarySigningCredential()
                .AddInMemoryIdentityResources(Config.GetIdentityResources())
                .AddTestUsers(TestUsers.Users)
                .AddCorsPolicyService<CorsPolicyService>()
                .AddResourceStore<CouchDbResourcesStore>()
                .AddClientStore<CouchDbClientStore>();

            services.AddMvc();
          
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                _loggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }

            var couchDbClientStore = new CouchDbClientStore(new CouchDbAccessService(_couchDbSettings, _logger));
            couchDbClientStore.AddClients();
            var couchDbResourceStore = new CouchDbResourcesStore(new CouchDbAccessService(_couchDbSettings, _logger));
            couchDbResourceStore.AddResources();

            loggerFactory.AddSerilog(_logger);

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
                .UseFabricMonitoring(() => Task.FromResult(true), _loggingLevelSwitch);
        }
    }

    public class CorsPolicyService : ICorsPolicyService
    {
        private readonly IDocumentDbService _documentDbService;

        public CorsPolicyService(IDocumentDbService documentDbService)
        {
            _documentDbService = documentDbService;
        }

        public Task<bool> IsOriginAllowedAsync(string origin)
        {
            return _documentDbService.DoesDocumentExist("client", origin);
        }
    }

    
}
