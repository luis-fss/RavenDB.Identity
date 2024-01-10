using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;
using Raven.Identity;
using RavenDB.DependencyInjection;
using Sample.BlazorApp.Components;
using Sample.BlazorApp.Components.Account;
using Sample.BlazorApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

// builder.Services.AddAuthentication(options =>
//     {
//         options.DefaultScheme = IdentityConstants.ApplicationScheme;
//         options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
//     })
//     .AddIdentityCookies();

// var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
// builder.Services.AddDbContext<ApplicationDbContext>(options =>
//     options.UseSqlite(connectionString));
// builder.Services.AddDatabaseDeveloperPageExceptionFilter();
//
// builder.Services.AddIdentityCore<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
//     .AddEntityFrameworkStores<ApplicationDbContext>()
//     .AddSignInManager()
//     .AddDefaultTokenProviders();

// Add RavenDB and identity.
builder.Services
    // Create an IDocumentStore singleton from the RavenSettings.
    .AddRavenDbDocumentStore()
    // Create a RavenDB IAsyncDocumentSession for each request. You're responsible for calling .SaveChanges after each request.
    .AddRavenDbAsyncSession()
    // Adds an identity system to ASP.NET Core
    .AddIdentity<ApplicationUser, Raven.Identity.IdentityRole>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = true;
        options.SignIn.RequireConfirmedAccount = true;
        options.SignIn.RequireConfirmedPhoneNumber = false;
        options.Password.RequiredLength = 4;
        options.Password.RequiredUniqueChars = 1;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
    })
    // Use RavenDB as the store for identity users and roles. Specify your app user type here, and your role type.
    // If you don't have a role type, use Raven.Identity.IdentityRole.
    .AddRavenDbIdentityStores<ApplicationUser, Raven.Identity.IdentityRole>()
    //.AddSignInManager()
    .AddDefaultTokenProviders();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

// Instruct ASP.NET Core to use authentication and authorization.
// app.UseAuthentication();
// app.UseAuthorization();

// Create the database if it doesn't exist.
// Also, create our roles if they don't exist. Needed because we're doing some role-based auth in this demo.
var docStore = app.Services.GetRequiredService<IDocumentStore>();
docStore.EnsureExists();
docStore.EnsureRolesExist([ApplicationUser.AdminRole, ApplicationUser.ManagerRole]);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();