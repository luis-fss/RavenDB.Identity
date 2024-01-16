using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using Raven.Identity;
using Raven.Identity.DependencyInjection;
using Sample.RazorPages.Common;
using Sample.RazorPages.Filters;
using Sample.RazorPages.Models;

namespace Sample.RazorPages
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure Raven Identity in just a few lines of code:
            services
                .AddRavenDbDocumentStore() // 1. Configures Raven connection using the settings in appsettings.json.
                .AddRavenDbAsyncSession(); // 2. Add a scoped IAsyncDocumentSession. For the sync version, use .AddRavenSession() instead.

            // 3. Add our RavenDB.Identity provider.
            var identityBuilder = services
                .AddDefaultIdentity<ApplicationUser>()
                .AddRavenDbIdentityStores<ApplicationUser>();

            // 4. Optional: some default UI pages for register/login/password reset/etc.
            identityBuilder.AddDefaultUI();

            // 5. Finally, instruct Razor Pages to call dbSession.SaveChangesAsync() when an action completes.
            // For MVC apps, you may instead use a base controller that calls .SaveChangesAsync().
            services.AddRazorPages().AddMvcOptions(o => o.Filters.Add<RavenSaveChangesAsyncFilter>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            // Needed for Raven Identity to work.
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });

            // Create our database if it doesn't exist yet.
            app.ApplicationServices.GetRequiredService<IDocumentStore>().EnsureExists();
        }
    }
}
