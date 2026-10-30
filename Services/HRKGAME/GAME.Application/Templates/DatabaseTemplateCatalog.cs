using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Templates;

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
