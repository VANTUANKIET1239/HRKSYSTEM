using System.Security.Cryptography;
using GAME.Domain.Entities;

namespace GAME.Infrastructure.Services;

public static class HeroStarRollCalculator
{
    public static (HrkHeroStarAttributePool Entry, decimal Value) Roll(IReadOnlyList<HrkHeroStarAttributePool> pool, Guid seed, int offset)
    {
        if (pool.Count == 0) throw new InvalidOperationException("Star attribute pool is empty.");
        var hash = BitConverter.ToUInt32(SHA256.HashData(seed.ToByteArray().Concat(BitConverter.GetBytes(offset)).ToArray()), 0);
        var cursor = (int)(hash % (uint)pool.Sum(p => Math.Max(1, p.Weight)));
        var selected = pool[0];
        foreach (var entry in pool)
        {
            cursor -= Math.Max(1, entry.Weight);
            if (cursor < 0) { selected = entry; break; }
        }
        if (selected.MinValue > selected.MaxValue) throw new InvalidOperationException("Invalid star attribute range.");
        var value = Math.Round(selected.MinValue + (selected.MaxValue - selected.MinValue) * (hash % 10001 / 10000m), 2);
        return (selected, value);
    }
}
