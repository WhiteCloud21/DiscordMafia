using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DiscordMafia.DB;
using Microsoft.EntityFrameworkCore;
using NonFactors.Mvc.Grid;
using Microsoft.AspNetCore.HttpOverrides;

namespace MafiaWeb
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connection = Configuration.GetConnectionString("DefaultConnection");
            services.AddDbContext<GameContext>(options => options.UseSqlite(connection));

            // Add framework services.
            services.AddCloudscribeNavigation(Configuration.GetSection("NavigationOptions"));
            services.AddMvc()
                .AddRazorOptions(options =>
                {
                    options.AddCloudscribeNavigationBootstrap3Views();
                });
            services.AddMvcGrid();

            var dirSection = Configuration.GetSection("Directories");
            var settings = new DiscordMafia.Config.MainSettings(System.IO.Path.Combine(dirSection["Config"], "mainSettings.xml"), System.IO.Path.Combine(dirSection["Config"], "Local/mainSettings.xml"));
            settings.LoadLanguage();
            services.AddSingleton(typeof(DiscordMafia.Services.ILanguage), settings.Language);
            services.AddSingleton(typeof(DiscordMafia.Config.MainSettings), settings);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseStaticFiles();
            app.UseStatusCodePagesWithReExecute("/Error/{0}");

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}");
            });
        }
    }
}
