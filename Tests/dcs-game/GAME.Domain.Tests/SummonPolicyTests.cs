using Core.Common.Errors;
using GAME.Application.Common;
using GAME.Domain.Entities;
using Xunit;

namespace GAME.Domain.Tests;

public class SummonPolicyTests
{
    [Fact]
    public void CapacityReserveIncludesDistinctStoneTypesAndExistingStackSpace()
    {
        Assert.Equal(2, SummonInventoryPolicy.RequiredFreeSlots(10, [new(20, 9999, 0), new(20, 9999, 0)]));
        Assert.Equal(0, SummonInventoryPolicy.RequiredFreeSlots(10, [new(20, 9999, 200), new(20, 9999, 200)]));
        Assert.Equal(4, SummonInventoryPolicy.RequiredFreeSlots(10, [new(20, 50, 0)]));
    }

    [Theory]
    [InlineData(-1)] [InlineData(0)] [InlineData(2)] [InlineData(9)] [InlineData(11)]
    public void RejectsUnsupportedDrawCounts(int count) =>
        Assert.Throws<BusinessException>(() => SummonPolicy.ValidateRequest(Guid.NewGuid(), "NORMAL", count));

    [Fact]
    public void RejectsMissingIdentityOrPool()
    {
        Assert.Throws<BusinessException>(() => SummonPolicy.ValidateRequest(Guid.Empty, "NORMAL", 1));
        Assert.Throws<BusinessException>(() => SummonPolicy.ValidateRequest(Guid.NewGuid(), null, 1));
    }

    [Fact]
    public void WeightedSelectionHonorsEveryBoundary()
    {
        var entries = new[] { new HrkSummonPoolEntry { HeroTemplateId = 1, Weight = 3 }, new HrkSummonPoolEntry { HeroTemplateId = 2, Weight = 2 } };
        Assert.Equal(5, SummonPolicy.TotalWeight(entries));
        Assert.Equal([1, 1, 1, 2, 2], Enumerable.Range(0, 5).Select(roll => SummonPolicy.Select(entries, roll).HeroTemplateId));
        Assert.Throws<ArgumentOutOfRangeException>(() => SummonPolicy.Select(entries, 5));
    }

    [Fact]
    public void RejectsEmptyNonPositiveAndOverflowingPools()
    {
        Assert.Throws<InvalidOperationException>(() => SummonPolicy.TotalWeight([]));
        Assert.Throws<InvalidOperationException>(() => SummonPolicy.TotalWeight([new() { Weight = 0 }]));
        Assert.Throws<InvalidOperationException>(() => SummonPolicy.TotalWeight([new() { Weight = int.MaxValue }, new() { Weight = 1 }]));
    }

    [Fact]
    public void UsesConfiguredBatchPriceAndRejectsInvalidCurrency()
    {
        var pool = new HrkSummonPool { Currency = "GOLD", SingleCost = 10, TenCost = 90 };
        Assert.Equal(90, SummonPolicy.Cost(pool, 10));
        pool.Currency = "UNKNOWN";
        Assert.Throws<InvalidOperationException>(() => SummonPolicy.Cost(pool, 1));
    }
}
