using Core.Common.Errors;
using GAME.Domain.Entities;

namespace GAME.Application.Common;

public static class SummonPolicy
{
    public static void ValidateRequest(Guid requestId, string? poolCode, int count)
    {
        if (requestId == Guid.Empty || string.IsNullOrWhiteSpace(poolCode) || poolCode.Length > 30 || count is not (1 or 10))
            throw new BusinessException("INVALID_SUMMON_REQUEST", "Yêu cầu chiêu mộ không hợp lệ.", BusinessErrorKind.Validation);
    }

    public static long Cost(HrkSummonPool pool, int count)
    {
        if (count is not (1 or 10)) throw new ArgumentOutOfRangeException(nameof(count));
        var cost = count == 1 ? pool.SingleCost : pool.TenCost;
        if (cost <= 0 || pool.Currency is not ("GOLD" or "DIAMOND") || (pool.Currency == "DIAMOND" && cost > int.MaxValue))
            throw new InvalidOperationException("Invalid summon price configuration.");
        return cost;
    }

    public static int TotalWeight(IReadOnlyList<HrkSummonPoolEntry> entries)
    {
        if (entries.Count == 0 || entries.Any(x => x.Weight <= 0))
            throw new InvalidOperationException("Summon pool must contain positive weights.");
        var total = entries.Sum(x => (long)x.Weight);
        if (total > int.MaxValue) throw new InvalidOperationException("Summon pool weight exceeds the supported range.");
        return (int)total;
    }

    public static HrkSummonPoolEntry Select(IReadOnlyList<HrkSummonPoolEntry> entries, int roll)
    {
        var total = TotalWeight(entries);
        if (roll < 0 || roll >= total) throw new ArgumentOutOfRangeException(nameof(roll));
        foreach (var entry in entries)
        {
            if (roll < entry.Weight) return entry;
            roll -= entry.Weight;
        }
        throw new InvalidOperationException("Invalid summon selection.");
    }
}
