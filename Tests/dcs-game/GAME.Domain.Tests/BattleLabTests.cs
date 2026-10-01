using GAME.Application.DTOs;
using GAME.Domain.Battle;
using GAME.Domain.Entities;
using GAME.Infrastructure.Services;
using Xunit;

namespace GAME.Domain.Tests;

public sealed class BattleLabTests
{
    private static BattleLabRequestDto Request() => new()
    {
        Count = 3, Seed = 12, MaxRounds = 2,
        Left = [new() { HeroTemplateId = 1, Position = 1, Stats = new() { Hp = 1000, Atk = 100, Spd = 100 } }],
        Right = [new() { HeroTemplateId = 1, Position = 1, Stats = new() { Hp = 1000, Atk = 100, Spd = 90 } }]
    };

    private static (BattleLabService service, TowerTestStore store) Setup()
    {
        var store = new TowerTestStore();
        store.Data<HrkHeroTemplate>().Add(new() { Id = 1, Name = "Test", Avatar = "test.png" });
        store.Data<HrkHeroSkill>().Add(new()
        {
            HeroTemplateId = 1,
            Skill = new()
            {
                Id = "TEST", Name = "Test", SkillTypeCode = "NORMAL", IsActive = true,
                Effects = [new()
                {
                    EffectType = new() { Code = "Damage", Name = "Damage" },
                    TargetType = new() { Code = "ENEMY_SINGLE", Name = "Enemy" },
                    DamageSchoolCode = "PHYSICAL", BaseValue = 100
                }]
            }
        });
        store.Data<HrkBattleConfig>().Add(new() { Code = BattleMitigationConfig.Code, Value = 1000, IsEnabled = true });
        return (new BattleLabService(store, new BattleSimulationEngine()), store);
    }

    [Fact]
    public async Task Batch_is_deterministic_and_aggregates_without_growing_replay_count()
    {
        var (service, store) = Setup();
        var first = await service.RunAsync(Request(), default);
        var second = await service.RunAsync(Request(), default);
        Assert.Equal(new[] { 12, 13, 14 }, first.Runs.Select(r => r.Seed));
        Assert.Equal(first.Runs.Select(r => (r.Seed, r.Winner, r.Rounds)), second.Runs.Select(r => (r.Seed, r.Winner, r.Rounds)));
        Assert.Single(first.Replays);
        Assert.Equal(1000, first.Settings.DefenseConstant);
        Assert.Equal(600, first.Totals.Single(s => s.Team == 0).PhysicalDamageDealt);
        Assert.Equal(1000, first.Replays[0].InitialState.LeftTeam[0].Stats.Hp);
        Assert.Empty(store.Data<HrkPlayer>());
        Assert.Empty(store.Data<HrkPlayerWallet>());
        Assert.Equal(first.SnapshotHash, second.SnapshotHash);
        Assert.Equal(200, first.Runs[0].Heroes.Single(h => h.Statistics.Team == 0).Statistics.PhysicalDamageDealt);
        Assert.Equal(800, first.Runs[0].Heroes.Single(h => h.Statistics.Team == 0).RemainingHp);
        Assert.Equal(2, first.Runs[0].Heroes.Single(h => h.Statistics.Team == 0).SkillCasts);
        Assert.Equal(600, first.Skills.Single(s => s.ActorId == 1).HpDamage);
        Assert.Equal(6, first.Skills.Single(s => s.ActorId == 1).Hits);
    }

    [Fact]
    public void Rejects_duplicate_positions_and_excessive_batch()
    {
        var request = Request();
        request.Left.Add(request.Left[0]);
        Assert.Throws<ArgumentException>(() => BattleLabService.Validate(request));
        request = Request(); request.Count = 301;
        Assert.Throws<ArgumentException>(() => BattleLabService.Validate(request));
    }

    [Fact]
    public void Rejects_mixed_modes_to_prevent_double_counting()
    {
        var request = Request(); request.Left[0].Mode = "BUILD"; request.Left[0].Build = new();
        Assert.Throws<ArgumentException>(() => BattleLabService.Validate(request));
        request.Left[0].Stats = null;
        BattleLabService.Validate(request);
        request.Left[0].Mode = "CUSTOM";
        Assert.Throws<ArgumentException>(() => BattleLabService.Validate(request));
    }

    [Fact]
    public async Task Rejects_unknown_template()
    {
        var (service, _) = Setup();
        var request = Request(); request.Left[0].HeroTemplateId = 999;
        await Assert.ThrowsAsync<ArgumentException>(() => service.RunAsync(request, default));
    }

    [Fact]
    public async Task Cancellation_stops_batch()
    {
        var (service, _) = Setup();
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.RunAsync(Request(), cancellation.Token));
    }
}
