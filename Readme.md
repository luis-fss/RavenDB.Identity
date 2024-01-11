# <img src="https://github.com/luis-fss/RavenDB.Identity/blob/master/RavenDB.Identity/nuget-icon.png?raw=true" width="50px" height="50px" /> RavenDB.Identity
The simple and easy Identity provider for RavenDB and ASP.NET Core. Use Raven to store your users and roles.

## IMPORTANT ##

This project is a fork of [JudahGabriel/RavenDB.Identity](https://github.com/JudahGabriel/RavenDB.Identity). Many thanks to the author for the excellent work and inspiration.

I created this fork because I like to keep my projects up-to-date with the latest versions of dotnet, and I intend to use this code in my own projects. It is not my intention to make it available and/or maintain a package for general public use. Although at some point, I may publish a NuGet package, I strongly discourage its use by third parties. I won't consider potential incompatibilities in future changes. The original license remains intact, so anyone is still free to copy the code and use it as they wish.

<a id="fork-changes">How does this project differ from the original?</a>

1. All projects have been updated to the latest version of dotnet, and all third-party dependencies have also been updated.
2. The SaveChangesAsync method was added to the UserStore.UpdateAsync method. I think it's better this way, but I still need to conduct some tests, so this may change, and I am also considering creating an option that can be configured at the time of service registration, enabling or disabling this feature;
3. The source code of the RavenDB.DependencyInjection project was added to facilitate dependency management and updates.
4. The default email key reservation prefix was changed from "emails/" to "identity-emails/".
5. An example Blazor Web App project was added.
6. Additionally, I made some minor improvements to log messages, namespace usage, and code cleanups.
7. Finally, I still intend to include a configuration to use a username that will not allow future changes, unlike the email, which is set during registration.

## Instructions ##

1. Add an [ApplicationUser class](https://github.com/luis-fss/RavenDB.Identity/blob/master/Samples/Sample.BlazorApp/Data/ApplicationUser.cs) that derives from Raven.Identity.IdentityUser:
```csharp
public class ApplicationUser : Raven.Identity.IdentityUser
{
    /// <summary>
    /// A user's full name.
    /// </summary>
    public string FullName { get; set; }
}
```

2. In appsettings.json, configure your connection to Raven:

```json
"RavenSettings": {
    "Urls": [
        "http://live-test.ravendb.net"
    ],
    "DatabaseName": "Raven.Identity.Sample.BlazorApp",
    "CertFilePath": "",
    "CertPassword": ""
},
```

3a. Blazor Web App with server interactive components: in [Program.cs](https://github.com/luis-fss/RavenDB.Identity/blob/master/Samples/Sample.BlazorApp/Program.cs), wire it all up. Note that this approach will use the email address as part of the User Id, such that changing their email will change their User Id as well:

```csharp
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();
builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

// Policies configuration
builder.Services.AddAuthorizationBuilder().AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));

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
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    })
    // Use RavenDB as the store for identity users and roles. Specify your app user type here, and your role type.
    // If you don't have a role type, use Raven.Identity.IdentityRole.
    .AddRavenDbIdentityStores<ApplicationUser, Raven.Identity.IdentityRole>()
    .AddDefaultTokenProviders();
```

3b. RazorPages or MVC: in [Startup.cs](https://github.com/luis-fss/RavenDB.Identity/blob/master/Samples/Sample.RazorPages/Startup.cs), wire it all up. Note that this approach will use the email address as part of the User Id, such that changing their email will change their User Id as well:

```csharp
public void ConfigureServices(IServiceCollection services)
{    
    // Add RavenDB and identity.
    services
        .AddRavenDbDocStore() // Create an IDocumentStore singleton from the RavenSettings.
        .AddRavenDbAsyncSession() // Create a RavenDB IAsyncDocumentSession for each request. You're responsible for calling .SaveChanges after each request.
        .AddIdentity<ApplicationUser, IdentityRole>() // Adds an identity system to ASP.NET Core
        .AddRavenDbIdentityStores<ApplicationUser, IdentityRole>(); // Use RavenDB as the store for identity users and roles. Specify your app user type here, and your role type. If you don't have a role type, use Raven.Identity.IdentityRole.
    ...
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    ...
    // Instruct ASP.NET Core to use authentication and authorization.
    app.UseAuthentication();
    app.UseAuthorization();
    ...
}
```

4. I'm leaving this for future reference, but this step may be obsolete or not working at all, see: <a href="#fork-changes">fork changes</a>: In your controller actions, [call .SaveChangesAsync() when you're done making changes](https://github.com/luis-fss/RavenDB.Identity/blob/master/Samples/Sample.RazorPages/Filters/RavenSaveChangesAsyncFilter.cs#L35). Typically this is done via a [RavenController base class](https://github.com/luis-fss/RavenDB.Identity/blob/master/Samples/Sample.Mvc/Controllers/RavenController.cs) for MVC/WebAPI projects or via a [page filter](https://github.com/luis-fss/RavenDB.Identity/blob/master/Samples/Sample.RazorPages/Filters/RavenSaveChangesAsyncFilter.cs) for Razor Pages projects.

## Modifying RavenDB conventions

Need to modify RavenDB conventions? You can use the `services.AddRavenDbDocStore(options)` overload:

```csharp
services.AddRavenDbDocStore(options =>
{
    // Maybe we want to change the identity parts separator.
    options.BeforeInitializeDocStore = docStore => docStore.Conventions.IdentityPartsSeparator = "-";
})
```

## Getting Started and Sample Project

Need help? Checkout the our samples to see how to use it:

- [Razor Pages](https://github.com/luis-fss/RavenDB.Identity/tree/master/Samples/Sample.RazorPages) 
- [MVC](https://github.com/luis-fss/RavenDB.Identity/tree/master/Samples/Sample.Mvc)
- [Blazor Web App](https://github.com/luis-fss/RavenDB.Identity/tree/master/Samples/Sample.BlazorApp)
