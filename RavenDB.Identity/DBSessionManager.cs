using System;
using System.Threading;
using System.Threading.Tasks;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;

namespace Raven.Identity;

/// <summary>
/// DbSessionManager for manage RavenDb document session.
/// </summary>
public class DbSessionManager
{
    private readonly IDocumentStore _documentStore;
    private IAsyncDocumentSession? _session;

    /// <summary>
    /// Creates a new DbSessionManager for manage RavenDb document session.
    /// </summary>
    /// <param name="documentStore"></param>
    public DbSessionManager(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    /// <summary>
    /// Returns the RavenDb <see cref="IDocumentStore"/>
    /// </summary>
    /// <returns><see cref="IDocumentStore"/></returns>
    public IDocumentStore GetDocumentStore()
    {
        return _documentStore;
    }

    /// <summary>
    /// Returns the RavenDb <see cref="IAsyncDocumentSession"/>
    /// </summary>
    /// <returns><see cref="IAsyncDocumentSession"/></returns>
    public IAsyncDocumentSession GetAsyncSession()
    {
        _session ??= _documentStore.OpenAsyncSession();
        return _session;
    }

    /// <summary>
    /// Returns a new RavenDb <see cref="IAsyncDocumentSession"/>
    /// </summary>
    /// <param name="saveChanges">Call <see cref="SaveChangesAsync"/> on the current <see cref="IAsyncDocumentSession"/>,
    /// if it has tracked changes, and disposes, before opens a new one.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns><see cref="IAsyncDocumentSession"/></returns>
    public async Task<IAsyncDocumentSession> RenewSessionAsync(bool saveChanges, CancellationToken cancellationToken)
    {
        if (_session?.Advanced.HasChanges is true && saveChanges)
        {
            await SaveChangesAsync(cancellationToken);
            _session.Dispose();
        }
        _session = _documentStore.OpenAsyncSession();
        return _session;
    }

    /// <summary>
    /// Call <see cref="SaveChangesAsync"/> on the current <see cref="IAsyncDocumentSession"/>
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns><see cref="Task"/></returns>
    /// <exception cref="ObjectDisposedException"></exception>
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _session?.SaveChangesAsync(cancellationToken) ?? throw new ObjectDisposedException("AsyncDocumentSession");
    }
}