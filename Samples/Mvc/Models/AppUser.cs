namespace Sample.Mvc.Models
{
    public class AppUser : Raven.Identity.IdentityUser
    {
        public const string AdminRole = "Admin";
        public const string ManagerRole = "Manager";

        /// <summary>
        /// The user's full name.
        /// </summary>
        public string FullName { get; set; }
    }
}
