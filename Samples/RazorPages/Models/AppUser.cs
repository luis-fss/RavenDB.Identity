namespace Sample.RazorPages.Models
{
    public class AppUser : Raven.Identity.IdentityUser
    {
        /// <summary>
        /// The user's full name.
        /// </summary>
        public string FullName { get; set; }
    }
}
