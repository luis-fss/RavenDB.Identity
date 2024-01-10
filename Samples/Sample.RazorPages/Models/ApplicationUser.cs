namespace Sample.RazorPages.Models
{
    public class ApplicationUser : Raven.Identity.IdentityUser
    {
        /// <summary>
        /// The user's full name.
        /// </summary>
        public string FullName { get; set; }
    }
}
