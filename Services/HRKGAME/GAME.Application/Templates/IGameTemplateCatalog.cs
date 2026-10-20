using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Templates;

/// <summary>
/// Read-only collections of detached, scope-owned entities with configured static relations.
/// Never attach these entities to a write repository. Filtering uses ordinary in-memory LINQ.
/// </summary>
public interface IGameTemplateCatalog
{
    Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class;
}

public interface IGameTemplateRefresh
{
    /// <summary>Reloads every configured table and publishes them together.</summary>
    Task<TemplateRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);
    /// <summary>Reloads T and its static relation group, retaining other tables from the previous revision.</summary>
    Task<TemplateRefreshResult> RefreshAsync<T>(CancellationToken cancellationToken = default) where T : class;
}

public sealed record TemplateRefreshResult(string Revision, DateTimeOffset LoadedAt, int TableCount, int RowCount);

/// <summary>
/// Scalar-only fallback for standalone handlers/test doubles. Consumers needing navigation
/// properties must inject GAME's configured catalog, including when caching is disabled.
/// </summary>
public sealed class DatabaseTemplateCatalog(IUnitOfWork unitOfWork) : IGameTemplateCatalog
{
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await unitOfWork.ReadOnlyRepository<T>().Query().ToListAsync(cancellationToken);
    }
}
