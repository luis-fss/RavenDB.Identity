﻿using Raven.Client.Documents;
using Raven.Client.Documents.Commands;
using System.Collections.Generic;
using System.Linq;

namespace Raven.Identity.Migrations
{
    /// <summary>
    /// Base class for migrations.
    /// </summary>
    public class MigrationBase
    {
        /// <summary>
        /// The Raven doc store.
        /// </summary>
        protected readonly IDocumentStore DocStore;

        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="db">The Raven document store.</param>
        protected MigrationBase(IDocumentStore db)
        {
            DocStore = db;
        }

        /// <summary>
        /// Lazily streams documents of the specified type back.
        /// </summary>
        /// <typeparam name="T">The type of document to stream.</typeparam>
        /// <returns>A lazy stream of documents.</returns>
        public IEnumerable<T> Stream<T>()
        {
            return StreamWithMetadata<T>().Select(r => r.Document);
        }

        /// <summary>
        /// Lazily streams document of the specified type back, including metadata.
        /// </summary>
        /// <typeparam name="T">The type of document to stream.</typeparam>
        /// <returns>A lazy stream of documents.</returns>
        public IEnumerable<StreamResult<T>> StreamWithMetadata<T>()
        {
            using var dbSession = DocStore.OpenSession();
            var collectionName = DocStore.Conventions.FindCollectionName(typeof(T));
            var identityPartsSeparator = DocStore.Conventions.IdentityPartsSeparator;
            using var stream = dbSession.Advanced.Stream<T>(collectionName + identityPartsSeparator);
            while (stream.MoveNext())
            {
                yield return stream.Current ?? new StreamResult<T>();
            }
        }
    }
}
