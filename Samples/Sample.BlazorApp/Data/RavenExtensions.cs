using Raven.Client.Documents;

namespace Sample.BlazorApp.Data
{
    public static class RavenExtensions
    {
        public static IDocumentStore EnsureDatabaseExists(this IDocumentStore store)
        {
            try
            {
                using var dbSession = store.OpenSession();
                // ReSharper disable once UnusedVariable
                var applicationUsers = dbSession.Query<ApplicationUser>().Take(0).ToList();
            }
            catch (Raven.Client.Exceptions.Database.DatabaseDoesNotExistException)
            {
                store.Maintenance.Server.Send(new Raven.Client.ServerWide.Operations.CreateDatabaseOperation(new Raven.Client.ServerWide.DatabaseRecord
                {
                    DatabaseName = store.Database
                }));
            }

            return store;
        }

        public static void EnsureRolesExist(this IDocumentStore docStore, IEnumerable<string> roleNames)
        {
            using var dbSession = docStore.OpenSession();
            var roleIds = roleNames.Select(r => "IdentityRoles/" + r);
            var roles = dbSession.Load<Raven.Identity.IdentityRole>(roleIds);

            foreach (var (id, value) in roles)
            {
                if (value != null) continue;
                var roleName = id.Replace("IdentityRoles/", string.Empty);
                dbSession.Store(new Raven.Identity.IdentityRole(roleName), id);
            }

            if (roles.Any(i => i.Value == null))
            {
                dbSession.SaveChanges();
            }
        }
    }
}
