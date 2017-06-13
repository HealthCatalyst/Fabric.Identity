using System.Linq;
using System.Threading.Tasks;
using Fabric.Identity.API.Configuration;
using Fabric.Identity.API.CouchDb;
using Fabric.Identity.API.EventSinks;
using Fabric.Identity.API.Extensions;
using Fabric.Identity.API.Services;
using Fabric.Platform.Logging;
using IdentityServer4.Models;
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
using System;

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
            _logger = LogFactory.CreateLogger(_loggingLevelSwitch, _appConfig.ElasticSearchSettings, _appConfig.ClientName, "identityservice", _appConfig.LogToFile);
            _couchDbSettings = _appConfig.CouchDbSettings;
        }
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton<IEventSink, ElasticSearchEventSink>();
            services.AddSingleton(_appConfig);           
            services.AddSingleton(_logger);
            services.AddFluentValidations();
            services.AddIdentityServer(_appConfig);

            services.AddCors(options => options.AddPolicy("AllowAll", p => p.AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()));

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

            InitializeStores(_appConfig.HostingOptions.UseInMemoryStores);
            
            loggerFactory.AddSerilog(_logger);
            app.UseCors("AllowAll");

            app.UseIdentityServer();
            app.UseExternalIdentityProviders(_appConfig);
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();           
            app.UseOwin()
                .UseFabricMonitoring(() => Task.FromResult(true), _loggingLevelSwitch);
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
                var couchDbBootStrapper = new CouchDbBootstrapper(new CouchDbAccessService(_couchDbSettings, _logger), _couchDbSettings);
                couchDbBootStrapper.Setup();
            }
        }
    }
}
