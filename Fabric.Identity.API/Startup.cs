using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.InteropServices;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.Documentation;
using Fabric.Identity.API.EventSinks;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Infrastructure;
using Fabric.Identity.API.Infrastructure.Monitoring;
using Fabric.Identity.API.Infrastructure.QueryStringBinding;
using Fabric.Identity.API.Persistence;
using Fabric.Identity.API.Persistence.SqlServer.Configuration;
using Fabric.Identity.API.Services;
using Fabric.Platform.Logging;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Quickstart.UI;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Cors.Infrastructure;
using ILogger = Serilog.ILogger;
using LogFactory = Fabric.Identity.API.Logging.LogFactory;

namespace Fabric.Identity.API
{
    public class Startup
    {
        private static readonly string ChallengeDirectory = @".well-known";
        private readonly IAppConfiguration _appConfig;
        private readonly ICertificateService _certificateService;

        public IConfiguration Configuration { get; set; }  

        public Startup(IConfiguration configuration)
        {
            _certificateService = IdentityConfigurationProvider.MakeCertificateService();
            var decryptionService = new DecryptionService(_certificateService);
            _appConfig =
                new IdentityConfigurationProvider(configuration).GetAppConfiguration(decryptionService);
        }

        private static string XmlCommentsFilePath
        {
            get
            {
                var basePath = System.AppContext.BaseDirectory;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            if (_appConfig.ApplicationInsights?.Enabled ?? false)
            {
                services.AddApplicationInsightsTelemetry();
            }

            var identityServerApiSettings = _appConfig.IdentityServerConfidentialClientSettings;

            services.TryAddSingleton(_appConfig.HostingOptions);

            services.TryAddSingleton<IConnectionStrings>(_appConfig.ConnectionStrings);

            var hostingOptions = services.BuildServiceProvider().GetRequiredService<HostingOptions>();
            var connectionStrings = services.BuildServiceProvider().GetRequiredService<IConnectionStrings>();

            var eventLogger = LogFactory.CreateEventLogger(hostingOptions, connectionStrings);
            var serilogEventSink = new SerilogEventSink(eventLogger);

            var settings = _appConfig.IdentityServerConfidentialClientSettings;
            var tokenUriAddress = $"{settings.Authority.EnsureTrailingSlash()}connect/token";
            services.AddTransient<IHttpRequestMessageFactory>(serviceProvider => new HttpRequestMessageFactory(
                tokenUriAddress,
                FabricIdentityConstants.FabricIdentityClient,
                settings.ClientSecret,
                null,
                null));

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
                .AddSingleton<HttpClient>()
                .AddSingleton<IEventSink>(serilogEventSink)
                .AddSingleton(_appConfig)
                .AddSingleton(Log.Logger)
                .AddIdentityServer(_appConfig, _certificateService, Log.Logger, hostingOptions, connectionStrings)
                .AddAuthorizationServices()
                .AddScoped<IUserResolverService, UserResolverService>()
                .AddSingleton<ISerializationSettings, SerializationSettings>()
                .AddSingleton<ILdapConnectionProvider, LdapConnectionProvider>()
                .AddSingleton<IExternalIdentityProviderServiceResolver, ExternalIdentityProviderServiceResolver>()
                .AddSingleton<IExternalIdentityProviderService, IdPSearchServiceProvider>()
                .AddSingleton<Services.IClaimsService, ClaimsService>()
                .AddSingleton<LdapProviderService>()
                .AddSingleton<PolicyProvider>()
                .AddTransient<IHealthCheckerService, HealthCheckerService>()
                .AddTransient<ICorsPolicyProvider, DefaultCorsPolicyProvider>()
                .AddFluentValidations();

            // filter settings
            var filterSettings = _appConfig.FilterSettings ??
                                 new FilterSettings { GroupFilterSettings = new GroupFilterSettings() };
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
            
            // The sigin and signout scheme for Authentication, is part of AddAzureIdentityProvider and AddExternalIdentityProvider
            services.AddAuthentication().AddJwtBearer(o =>
            {
                o.Authority = identityServerApiSettings.Authority;
                o.Audience = identityServerApiSettings.ClientId;
                o.RequireHttpsMetadata = false;
            }).AddAzureIdentityProviderIfApplicable(_appConfig).AddExternalIdentityProviders(_appConfig);

            services.AddTransient<IIdentityProviderConfigurationService, IdentityProviderConfigurationService>();
            services.AddTransient<AccountService>();

            services.AddMvc(options => { options.Conventions.Add(new CommaSeparatedQueryStringConvention()); })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
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
                    if (!apiDesc.TryGetMethodInfo(out var methodInfo)) return false;

                    var versions = methodInfo.DeclaringType
                        .GetCustomAttributes(true)
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
        public void Configure(
            IApplicationBuilder app,
            IHostingEnvironment env,
            IDbBootstrapper dbBootstrapper)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                LogFactory.LoggingLevelSwitch.MinimumLevel = LogEventLevel.Verbose;
            }

            app.UseExceptionHandler("/Home/UnauthorizedError");
            app.UseStatusCodePages(
                async context =>
                    {
                        if (context.HttpContext.Response.StatusCode == 401)
                        {
                            context.HttpContext.Response.ContentType = "text/html";
                            await context.HttpContext.Response.WriteAsync(
                                String.Format(
                                    "<script>window.location='/identity/home/UnauthorizedError'</script>",
                                    context.HttpContext.Request.QueryString));
                        }
                    });

            app.UseCheckXForwardHeader();

            InitializeDatabase(dbBootstrapper);

            app.UseCors(FabricIdentityConstants.FabricCorsPolicyName);

            app.UseStaticFiles();
            app.UseStaticFilesForAcmeChallenge(ChallengeDirectory, Log.Logger);


            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            app.UseSerilogRequestLogging();

            app.UseAuthentication();
            app.UseIdentityServer();

            app.UseMvcWithDefaultRoute();

            var healthCheckService = app.ApplicationServices.CreateScope().ServiceProvider.GetRequiredService<IHealthCheckerService>();
            app.UseOwin()
                .UseFabricMonitoring(healthCheckService.CheckHealth, LogFactory.LoggingLevelSwitch);

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger(c => { c.RouteTemplate = "swagger/ui/index/{documentName}/swagger.json"; });

            // Enable middleware to serve swagger-ui (HTML, JS, CSS etc.), specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                // this sets up the JSON endpoint (1 call to SwaggerEndpoint per version)
                c.SwaggerEndpoint("v1/swagger.json", "Health Catalyst Fabric Identity API V1");
                c.RoutePrefix = "swagger/ui/index";
            });
        }

        private static void InitializeDatabase(IDbBootstrapper dbBootstrapper)
        {
            if (dbBootstrapper.Setup())
            {
                dbBootstrapper.AddResources(Config.GetIdentityResources());
            }
        }

        
    }
}