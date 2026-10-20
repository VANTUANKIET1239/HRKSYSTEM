using System.Collections;
using GAME.Application.Templates;

namespace GAME.Infrastructure.Templates;

public sealed class GameTemplateCatalog(GameTemplateStore store, TemplateTableRegistry registry) : IGameTemplateCatalog
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    // Only connected tables requested by this scope, not an eager global snapshot.
    private readonly Dictionary<Type, IList> _rows = new();
    private string? _revision;

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        registry.Require(typeof(T));
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_rows.ContainsKey(typeof(T)))
            {
                _revision ??= await store.GetRevisionAsync(cancellationToken);
                var component = new Dictionary<Type, IList>();
                try
                {
                    foreach (var type in registry.Relations(typeof(T)))
                        component[type] = registry.Materialize(type, await store.GetTableAsync(type, _revision, cancellationToken));
                }
                catch (GameTemplateStore.TemplateTableExpiredException) when (_rows.Count == 0)
                {
                    await store.RenewAsync(_revision, cancellationToken);
                    _revision = await store.GetRevisionAsync(cancellationToken);
                    foreach (var type in registry.Relations(typeof(T)))
                        component[type] = registry.Materialize(type, await store.GetTableAsync(type, _revision, cancellationToken));
                }
                registry.Link(component);
                foreach (var (type, rows) in component) _rows.Add(type, rows);
            }
            return ((List<T>)_rows[typeof(T)]).AsReadOnly();
        }
        finally { _gate.Release(); }
    }
}
