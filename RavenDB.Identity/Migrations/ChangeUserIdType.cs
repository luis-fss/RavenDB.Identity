using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using Raven.Client.Documents.Operations.CompareExchange;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Raven.Identity.Migrations
{
    /// <summary>
    /// This migration changes your existing users to use a different user ID strategy.
    /// Note: This migration will migrate your existing users. However, if you have objects in your database referring to existing user Ids, you'll need to manually migrate those.
    /// </summary>
    public class ChangeUserIdType<TUser> : MigrationBase
        where TUser : IdentityUser
    {
        private readonly UserIdType _newUserIdType;

        /// <summary>
        /// Creates a new ChangeUserIdType migration.
        /// </summary>
        /// <param name="db">The Raven doc store.</param>
        /// <param name="newUserIdType">The type of ID to migrate to.</param>
        public ChangeUserIdType(IDocumentStore db, UserIdType newUserIdType)
            : base(db)
        {
            _newUserIdType = newUserIdType;
        }

        /// <summary>
        /// Runs the migration. This operation can take several minutes depeneding on how many users are in your database.
        /// IMPORTANT: backup your database before running this migration, as data loss is possible.
        /// </summary>
        public void Migrate()
        {
            // Raven doesn't allow you to change a document's existing ID.
            // Instead, you must create a new document with that ID.
            //
            // Step 1, grab all the existing users.
            // Step 2, recreate each user with new IDs
            // Step 3, update all compare/exchange email reservations to point to the new user IDs
            // Step 4, delete all the users with old IDs

            // 1. Grab all the existing users.
            var existingUserStream = StreamWithMetadata<TUser>();

            // Step 2: recreate each user with new IDs
            var userIdsToDelete = CloneUserWithNewId(existingUserStream);

            // Step 3: Update all the email reservations to point to the new users.
            var newUsers = Stream<TUser>()
                .Where(u => !userIdsToDelete.Contains(u.Id, StringComparer.OrdinalIgnoreCase)) // Exclude the old users; we're going to delete those momentarily if everything else succeeds.
                .Select(u => (email: u.Email, id: u.Id!))
                .ToList();
            int migratedEmailReservations = 0;
            foreach (var (email, id) in newUsers)
            {
                migratedEmailReservations++;
                System.Diagnostics.Debug.WriteLine("Updating email reservation for {0} to {1}. {2} of {3}", email, id, migratedEmailReservations, userIdsToDelete.Count);
                UpdateEmailReservation(email, id);
            }

            // Step 4, delete the old users.
            using var dbSession = DocStore.OpenSession();
            userIdsToDelete.ForEach(dbSession.Delete);
            dbSession.SaveChanges();
        }

        private List<string> CloneUserWithNewId(IEnumerable<StreamResult<TUser>> users)
        {
            var userIdsToDelete = new List<string>(CountUsers());
            var nextUserId = _newUserIdType == UserIdType.Consecutive ?
                GetNextIdForUser() : 
                -1;
            using var bulkInsert = DocStore.BulkInsert();
            foreach (var userStream in users)
            {
                var user = userStream.Document;

                // Figure out what the new ID will be.
                var newId = Conventions.UserIdFor(user, _newUserIdType, DocStore);
                if (_newUserIdType == UserIdType.Consecutive && newId != null && newId.EndsWith("|"))
                {
                    // We'll need to specify the full ID, as bulk insert doens't work with "Users|" type IDs.
                    newId = Conventions.CollectionNameWithSeparator<TUser>(DocStore) + nextUserId.ToString();
                    nextUserId++;
                }

                // See if we actually need a new ID.
                // If not, we can skip processing this user.
                var needsNewId = !string.Equals(newId, user.Id);
                if (needsNewId)
                {
                    // Strip off the existing ID, otherwise bulk insert will pick it up and use the existing.
                    user.Id = null;
                    userStream.Metadata.Remove("@id");

                    // Clone the user with the new ID.
                    if (newId != null)
                    {
                        bulkInsert.Store(user, newId, userStream.Metadata);
                    }
                    else
                    {
                        bulkInsert.Store(user, userStream.Metadata);
                    }

                    // Step 3a, queue up the delete for the existing user.
                    var isNewId = !string.Equals(user.Id, userStream.Id, StringComparison.OrdinalIgnoreCase);
                    if (isNewId)
                    {
                        userIdsToDelete.Add(userStream.Id);
                    }
                }
            }

            return userIdsToDelete;
        }

        private int CountUsers()
        {
            using var dbSession = DocStore.OpenSession();
            return dbSession.Query<TUser>().Count();
        }

        private long GetNextIdForUser()
        {
            var getIdsCommand = new NextIdentityForCommand(Conventions.CollectionNameFor<TUser>(DocStore));
            using var dbSession = DocStore.OpenSession();
            dbSession.Advanced.RequestExecutor.Execute(getIdsCommand, dbSession.Advanced.Context);
            return getIdsCommand.Result;
        }

        private void UpdateEmailReservation(string email, string userId)
        {
            var emailReservation = GetEmailReservation(email);
            var index = emailReservation?.Index ?? 0;
            var needsUpdate = !string.Equals(emailReservation?.Value, userId, StringComparison.OrdinalIgnoreCase);
            if (needsUpdate)
            {
                var result = PutEmailReservation(email, userId, index);
                if (!result.Successful)
                { 
                    throw new ArgumentException($"Unable to migrate user {email} to new ID {userId}");
                }
            }
        }

        private CompareExchangeResult<string> PutEmailReservation(string email, string userId, long index = 0)
        {
            var key = Conventions.CompareExchangeKeyForEmail(email);
            var newEmailReservation = new PutCompareExchangeValueOperation<string>(key, userId, index);
            return DocStore.Operations.Send(newEmailReservation);
        }

        private CompareExchangeValue<string>? GetEmailReservation(string email)
        {
            var key = Conventions.CompareExchangeKeyForEmail(email);
            var getReservation = new GetCompareExchangeValueOperation<string>(key);
            return DocStore.Operations.Send(getReservation);
        }
    }
}
