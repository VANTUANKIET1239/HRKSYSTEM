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
