namespace GAME.Application.Common;

public sealed record SummonStoneCapacity(int QuantityPerDuplicate, int StackLimit, long ExistingFreeUnits);

public static class SummonInventoryPolicy
{
    // Reserve for the worst possible draw sequence before rolling. Rejecting only
    // duplicate outcomes after a roll would let a full bag bias successful draws.
    public static long RequiredFreeSlots(int count, IReadOnlyList<SummonStoneCapacity> stones)
    {
        if (count is not (1 or 10)) throw new ArgumentOutOfRangeException(nameof(count));
        var worst = Enumerable.Repeat(long.MinValue, count + 1).ToArray();
        worst[0] = 0;
        foreach (var stone in stones)
        {
            if (stone.QuantityPerDuplicate <= 0 || stone.StackLimit <= 0 || stone.ExistingFreeUnits < 0)
                throw new InvalidOperationException("Invalid duplicate stone capacity configuration.");
            var next = Enumerable.Repeat(long.MinValue, count + 1).ToArray();
            for (var used = 0; used <= count; used++)
            {
                if (worst[used] == long.MinValue) continue;
                for (var draws = 0; draws <= count - used; draws++)
                {
                    var units = Math.Max(0, (long)draws * stone.QuantityPerDuplicate - stone.ExistingFreeUnits);
                    var slots = (units + stone.StackLimit - 1) / stone.StackLimit;
                    next[used + draws] = Math.Max(next[used + draws], worst[used] + slots);
                }
            }
            worst = next;
        }
        return Math.Max(0, worst.Max());
    }
}
