namespace Sample.BlazorApp.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : Raven.Identity.IdentityUser
{
    public const string AdminRole = "Admin";
    public const string ManagerRole = "Manager";

    /// <summary>
    /// A user's full name.
    /// </summary>
    public string FullName { get; set; } = null!;
}