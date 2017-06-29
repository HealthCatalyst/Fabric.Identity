using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.EventSinks;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Services;
using Fabric.Platform.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IdentityServer4.Services;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ILogger = Serilog.ILogger;
using System.Runtime.InteropServices;
using Fabric.Identity.API.Authorization;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Fabric.Identity.API
{
    public class Startup
    {
        private readonly IAppConfiguration _appConfig;
        private readonly ILogger _logger;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;
        private readonly ICouchDbSettings _couchDbSettings;
        private readonly ICertificateService _certificateService;

        public Startup(IHostingEnvironment env)
        {
            _certificateService = MakeCertificateService();
            _appConfig = new IdentityConfigurationProvider().GetAppConfiguration(env.ContentRootPath, _certificateService);
            _loggingLevelSwitch = new LoggingLevelSwitch();
            _logger = LogFactory.CreateLogger(_loggingLevelSwitch, _appConfig.ElasticSearchSettings, _appConfig.ClientName, FabricIdentityConstants.ServiceName);
            _couchDbSettings = _appConfig.CouchDbSettings;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var identityServerApiSettings = _appConfig.IdentityServerConfidentialClientSettings;
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IEventSink, ElasticSearchEventSink>();
            services.AddSingleton(_appConfig);
            services.AddSingleton(_logger);
            services.AddFluentValidations();
            services.AddIdentityServer(_appConfig, _certificateService, _logger);
            services.AddScopedDecorator<IDocumentDbService, AuditingDocumentDbService>();
            services.AddSingleton<IAuthorizationHandler, RegistrationAuthorizationHandler>();
            services.AddScoped<IUserResolveService, UserResolverService>();
            services.TryAddSingleton(new IdentityServerAuthenticationOptions
            {
                Authority = identityServerApiSettings.Authority,
                RequireHttpsMetadata = false,
                ApiName = identityServerApiSettings.ClientId
            });
            
            services.AddMvc();
            services.AddAuthorization(options =>
            {
                options.AddPolicy("RegistrationThreshold",
                    policy => policy.Requirements.Add(new RegisteredClientThresholdRequirement(1)));
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                _loggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }

            InitializeStores(_appConfig.HostingOptions.UseInMemoryStores);
            
            loggerFactory.AddSerilog(_logger);
            app.UseCors(FabricIdentityConstants.FabricCorsPolicyName);

            app.UseIdentityServer();
            app.UseExternalIdentityProviders(_appConfig);
            app.UseStaticFiles();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var options = app.ApplicationServices.GetService<IdentityServerAuthenticationOptions>();
            app.UseIdentityServerAuthentication(options);
            app.UseMvcWithDefaultRoute();
            app.UseOwin()
                .UseFabricMonitoring(HealthCheck, _loggingLevelSwitch);
        }

        public async Task<bool> HealthCheck()
        {
            IDocumentDbService documentDbService;
            if (_appConfig.HostingOptions.UseInMemoryStores)
            {
                documentDbService = new InMemoryDocumentService();
            }
            else
            {
                documentDbService = new CouchDbAccessService(_couchDbSettings, _logger);
            }
            var identityResources = await documentDbService.GetDocuments<IdentityResource>(FabricIdentityConstants.DocumentTypes.IdentityResourceDocumentType);
            return identityResources.Any();
        }

        private void InitializeStores(bool useInMemoryStores)
        {
            if (useInMemoryStores)
            {
                var inMemoryBootStrapper = new DocumentDbBootstrapper(new InMemoryDocumentService());
                inMemoryBootStrapper.Setup();
            }
            else
            {
                var couchDbBootStrapper = new CouchDbBootstrapper(new CouchDbAccessService(_couchDbSettings, _logger), _couchDbSettings, _logger);
                couchDbBootStrapper.Setup();
            }
        }

        private ICertificateService MakeCertificateService()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return new LinuxCertificateService();
            }
            return new WindowsCertificateService();
        }
    }
}
