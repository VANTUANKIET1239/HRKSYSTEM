using System.Collections.Concurrent;
using GAME.Domain.Battle.Effects;

namespace GAME.Domain.Battle.Reactions;

public static class CatScratchTracker
{
    private static readonly ConcurrentDictionary<string, (int NormalDmg, int DeepDmg)> DamageByActionAndActor = new();

    public static void RecordDamage(string? actionId, long actorId, int damage, bool isDeep)
    {
        if (string.IsNullOrEmpty(actionId) || damage <= 0) return;
        var key = $"{actionId}_{actorId}";
        DamageByActionAndActor.AddOrUpdate(
            key,
            isDeep ? (0, damage) : (damage, 0),
            (_, cur) => isDeep ? (cur.NormalDmg, cur.DeepDmg + damage) : (cur.NormalDmg + damage, cur.DeepDmg));
    }

    public static bool TryConsumeDamage(string? actionId, long actorId, out int normalDmg, out int deepDmg)
    {
        normalDmg = 0;
        deepDmg = 0;
        if (string.IsNullOrEmpty(actionId)) return false;
        var key = $"{actionId}_{actorId}";
        if (DamageByActionAndActor.TryRemove(key, out var val))
        {
            normalDmg = val.NormalDmg;
            deepDmg = val.DeepDmg;
            return true;
        }
        return false;
    }
}
