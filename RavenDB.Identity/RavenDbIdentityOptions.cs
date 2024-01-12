namespace Raven.Identity
{
    /// <summary>
    /// Options for initializing RavenDB.Identity.
    /// </summary>
    public class RavenDbIdentityOptions
    {
        /// <summary>
        /// Whether to use static indexes, defaults to false.
        /// </summary>
        /// <remarks>
        /// Indexes need to be deployed to server in order for static index queries to work.
        /// </remarks>
        /// <seealso cref="IdentityUserIndex{TUser}"/>
        public bool UseStaticIndexes { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if changes should be persisted after UpdateAsync is called.
        /// Note: CreateAsync and DeleteAsync will call SaveChanges regardless of this configuration, this is necessary to guarantee the uniqueness of the email and username.
        /// </summary>
        /// <value>
        /// True if changes should be automatically persisted, otherwise false.
        /// </value>
        public bool AutoSaveChanges { get; set; } = true;
    }
}