using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Documentation;
using Fabric.Identity.API.EventSinks;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Infrastructure;
using Fabric.Identity.API.Infrastructure.QueryStringBinding;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.Couchdb.Configuration;
using Fabric.Identity.API.Persistence.CouchDb.Configuration;
using Fabric.Identity.API.Persistence.CouchDb.Services;
using Fabric.Identity.API.Persistence.InMemory.Services;
using Fabric.Identity.API.Services;
using Fabric.Platform.Logging;
using IdentityServer4.Models;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using ILogger = Serilog.ILogger;
using LogFactory = Fabric.Identity.API.Logging.LogFactory;

namespace Fabric.Identity.API
{
    public class Startup
    {
        private static readonly string ChallengeDirectory = @".well-known";
        private readonly IAppConfiguration _appConfig;
        private readonly ICertificateService _certificateService;
        private readonly ICouchDbSettings _couchDbSettings;
        private readonly ILogger _logger;
        private readonly LoggingLevelSwitch _loggingLevelSwitch;

        public Startup(IHostingEnvironment env)
        {
            _certificateService = MakeCertificateService();
            _appConfig =
                new IdentityConfigurationProvider().GetAppConfiguration(env.ContentRootPath, _certificateService);
            _loggingLevelSwitch = new LoggingLevelSwitch();
            _logger = LogFactory.CreateTraceLogger(_loggingLevelSwitch, _appConfig.ApplicationInsights);
            _couchDbSettings = _appConfig.CouchDbSettings;
        }

        private static string XmlCommentsFilePath
        {
            get
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var identityServerApiSettings = _appConfig.IdentityServerConfidentialClientSettings;
            var eventLogger = LogFactory.CreateEventLogger(_loggingLevelSwitch, _appConfig.ApplicationInsights);
            var serilogEventSink = new SerilogEventSink(eventLogger);
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IEventSink>(serilogEventSink);
            services.AddSingleton(_appConfig);
            services.AddSingleton(_logger);
            services.AddFluentValidations();
            services.AddIdentityServer(_appConfig, _certificateService, _logger);
            services.AddScopedDecorator<IDocumentDbService, AuditingDocumentDbService>();
            services.AddAuthorizationServices();
            services.AddScoped<IUserResolveService, UserResolverService>();
            services.AddSingleton<ISerializationSettings, SerializationSettings>();
            services.AddSingleton<ILdapConnectionProvider, LdapConnectionProvider>();
            services.AddSingleton<IExternalIdentityProviderServiceResolver, ExternalIdentityProviderServiceResolver>();
            services.AddSingleton<LdapProviderService>();
            services.AddSingleton<PolicyProvider>();

            // filter settings
            var filterSettings = _appConfig.FilterSettings ??
                                 new FilterSettings {GroupFilterSettings = new GroupFilterSettings()};
            filterSettings.GroupFilterSettings = filterSettings.GroupFilterSettings ?? new GroupFilterSettings();
            services.TryAddSingleton(filterSettings.GroupFilterSettings);

            services.AddSingleton<GroupFilterService>();
            services.TryAddSingleton(_appConfig.LdapSettings);
            services.TryAddSingleton(new IdentityServerAuthenticationOptions
            {
                Authority = identityServerApiSettings.Authority,
                RequireHttpsMetadata = false,
                ApiName = identityServerApiSettings.ClientId
            });
            services.AddTransient<IIdentityProviderConfigurationService, IdentityProviderConfigurationService>();
            services.AddTransient<AccountService>();

            services.AddMvc(options => { options.Conventions.Add(new CommaSeparatedQueryStringConvention()); })
                .AddJsonOptions(x =>
                {
                    x.SerializerSettings.ReferenceLoopHandling =
                        new SerializationSettings().JsonSettings.ReferenceLoopHandling;
                });

            services.AddApiVersioning(options =>
            {
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.ReportApiVersions = true;
            });

            // Swagger
            services.AddSwaggerGen(c =>
            {
                // this defines the Swagger doc (1 call to SwaggerDoc per version)
                c.SwaggerDoc("v1",
                    new Info
                    {
                        Version = "v1",
                        Title = "Health Catalyst Fabric Identity API V1",
                        Description =
                            "Fabric.Identity contains a set of APIs that provides authentication for applications based on the OpenID Connect protocol. If you don't include the version in the URL you will get the v1 API."
                    });

                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    var versions = apiDesc.ControllerAttributes()
                        .OfType<ApiVersionAttribute>()
                        .SelectMany(attr => attr.Versions);

                    return versions.Any(v => $"v{v.ToString().Substring(0, 1)}" == docName);
                });

                c.AddSecurityDefinition("oauth2", new OAuth2Scheme
                {
                    Description =
                        "The Fabric.Identity management API requires authentication using oath2 and requires the below scopes.",
                    Type = "oauth2",
                    AuthorizationUrl = identityServerApiSettings.Authority,
                    Flow = "hybrid, implicit, client_credentials",
                    Scopes = new Dictionary<string, string>
                    {
                        {"fabric/identity.manageresources", "Access to manage Client, API, and Identity resources."}
                    }
                });


                c.CustomSchemaIds(type => type.FullName);

                c.OperationFilter<VersionRemovalOperationFilter>();
                c.OperationFilter<ParamMetadataOperationFilter>();
                c.OperationFilter<SecurityRequirementsOperationFilter>();
                c.OperationFilter<SetBodyParametersRequiredOperationFilter>();
                c.DocumentFilter<PathVersionDocumentFilter>();
                c.DocumentFilter<TagFilter>();
                c.IncludeXmlComments(XmlCommentsFilePath);
                c.DescribeAllEnumsAsStrings();
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

            var serializationSettings = app.ApplicationServices.GetService<ISerializationSettings>();
            InitializeStores(_appConfig.HostingOptions.UseInMemoryStores, serializationSettings);

            loggerFactory.AddSerilog(_logger);
            app.UseCors(FabricIdentityConstants.FabricCorsPolicyName);

            app.UseIdentityServer();
            app.UseExternalIdentityProviders(_appConfig);
            app.UseStaticFiles();
            app.UseStaticFilesForAcmeChallenge(ChallengeDirectory, _logger);

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var options = app.ApplicationServices.GetService<IdentityServerAuthenticationOptions>();
            app.UseIdentityServerAuthentication(options);
            app.UseMvcWithDefaultRoute();
            app.UseOwin()
                .UseFabricMonitoring(HealthCheck, _loggingLevelSwitch);

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                // this sets up the JSON endpoint (1 call to SwaggerEndpoint per version)
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Health Catalyst Fabric Identity API V1");
            });
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
                documentDbService = new CouchDbAccessService(_couchDbSettings, _logger, new SerializationSettings());
            }
            var identityResources =
                await documentDbService.GetDocuments<IdentityResource>(FabricIdentityConstants.DocumentTypes
                    .IdentityResourceDocumentType);
            return identityResources.Any();
        }

        private void InitializeStores(bool useInMemoryStores, ISerializationSettings serializationSettings)
        {
            if (useInMemoryStores)
            {
                var inMemoryBootStrapper = new DocumentDbBootstrapper(new InMemoryDocumentService());
                inMemoryBootStrapper.Setup();
            }
            else
            {
                var couchDbBootStrapper =
                    new CouchDbBootstrapper(new CouchDbAccessService(_couchDbSettings, _logger, serializationSettings),
                        _couchDbSettings, _logger);
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