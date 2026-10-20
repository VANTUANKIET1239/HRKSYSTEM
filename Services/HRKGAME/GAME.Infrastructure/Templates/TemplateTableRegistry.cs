using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GAME.Infrastructure.Templates;

/// <summary>Resolves configured entity names once; stores metadata, not template data.</summary>
public sealed class TemplateTableRegistry
{
    private readonly Dictionary<Type, IEntityType> _entities = new();
    private readonly Dictionary<Type, Type[]> _components;
    private readonly JsonSerializerOptions _json;
    public IReadOnlyCollection<Type> Types => _entities.Keys;

    public TemplateTableRegistry(IServiceScopeFactory scopes, IOptions<GameTemplateCacheOptions> options)
    {
        using var scope = scopes.CreateScope();
        // EF model metadata only; table data is loaded through IUnitOfWork.
        var model = scope.ServiceProvider.GetRequiredService<GameDbContext>().Model;
        foreach (var name in options.Value.Tables)
        {
            var matches = model.GetEntityTypes().Where(e => e.ClrType.Name == name || e.ClrType.FullName == name).ToArray();
            if (matches.Length != 1 || matches[0].IsOwned() || matches[0].FindPrimaryKey() == null)
                throw new InvalidOperationException($"GameTemplateCache:Tables entity '{name}' must identify one mapped, non-owned entity with a primary key.");
            var entity = matches[0];
            if (entity.GetProperties().Any(p => p.PropertyInfo == null))
                throw new InvalidOperationException($"Template {name} has shadow/field-only columns, which are not supported by the scalar cache.");
            if (!_entities.TryAdd(entity.ClrType, entity))
                throw new InvalidOperationException($"GameTemplateCache:Tables contains duplicate entity '{name}'.");
        }
        if (_entities.Count == 0) throw new InvalidOperationException("GameTemplateCache:Tables must contain at least one entity.");
        _components = _entities.Keys.ToDictionary(t => t, RelatedTypes);
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(info =>
        {
            if (!_entities.TryGetValue(info.Type, out var entity)) return;
            var names = entity.GetProperties().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
            for (var i = info.Properties.Count - 1; i >= 0; i--)
                if (!names.Contains(info.Properties[i].Name)) info.Properties.RemoveAt(i);
        });
        _json = new JsonSerializerOptions { TypeInfoResolver = resolver };
    }

    public void Require(Type type)
    {
        if (!_entities.ContainsKey(type)) throw new ArgumentException($"{type.Name} is not configured in GameTemplateCache:Tables.");
    }

    public IReadOnlyList<Type> Relations(Type type) { Require(type); return _components[type]; }
    public string Name(Type type) { Require(type); return type.FullName!; }

    private Type[] RelatedTypes(Type root)
    {
        var result = new HashSet<Type> { root };
        var queue = new Queue<Type>(); queue.Enqueue(root);
        while (queue.TryDequeue(out var current))
        {
            foreach (var fk in _entities.Values.SelectMany(e => e.GetForeignKeys()))
            {
                var child = fk.DeclaringEntityType.ClrType;
                var parent = fk.PrincipalEntityType.ClrType;
                if (!_entities.ContainsKey(child) || !_entities.ContainsKey(parent)) continue;
                var related = child == current ? parent : parent == current ? child : null;
                if (related != null && result.Add(related)) queue.Enqueue(related);
            }
        }
        return result.ToArray();
    }

    internal Task<JsonElement> LoadAsync(Type type, IUnitOfWork store, CancellationToken ct)
    {
        Require(type);
        return (Task<JsonElement>)typeof(TemplateTableRegistry).GetMethod(nameof(LoadTableAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(type).Invoke(this, [store, ct])!;
    }

    private async Task<JsonElement> LoadTableAsync<T>(IUnitOfWork store, CancellationToken ct) where T : class =>
        JsonSerializer.SerializeToElement(await store.ReadOnlyRepository<T>().Query().ToListAsync(ct), _json);

    internal IList Materialize(Type type, JsonElement rows) => (IList)rows.Deserialize(typeof(List<>).MakeGenericType(type), _json)!;

    /// <summary>Connects scope-owned copies only; no entity is attached to EF or shared via L1.</summary>
    internal void Link(IReadOnlyDictionary<Type, IList> rows)
    {
        foreach (var type in rows.Keys)
        foreach (var fk in _entities[type].GetForeignKeys())
        {
            if (!rows.TryGetValue(fk.PrincipalEntityType.ClrType, out var parents)) continue;
            var lookup = parents.Cast<object>().ToDictionary(p => Key(p, fk.PrincipalKey.Properties));
            foreach (var child in rows[type])
            {
                if (fk.Properties.Any(p => p.PropertyInfo!.GetValue(child) == null)) continue;
                if (!lookup.TryGetValue(Key(child!, fk.Properties), out var parent))
                    throw new InvalidOperationException($"Template relation {type.Name} -> {fk.PrincipalEntityType.ClrType.Name} is incomplete. Commit related database edits before refreshing.");
                fk.DependentToPrincipal?.PropertyInfo?.SetValue(child, parent);
                var inverse = fk.PrincipalToDependent?.PropertyInfo;
                if (inverse == null) continue;
                if (fk.PrincipalToDependent!.IsCollection)
                {
                    var collection = inverse.GetValue(parent)!;
                    collection.GetType().GetMethod("Add")!.Invoke(collection, [child]);
                }
                else inverse.SetValue(parent, child);
            }
        }
    }

    private static string Key(object row, IReadOnlyList<IProperty> properties) =>
        JsonSerializer.Serialize(properties.Select(p => p.PropertyInfo!.GetValue(row)).ToArray());
}
