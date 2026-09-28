using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class BattleEffectHandlerTests
{
    [Fact]
    public void Default_registry_contains_all_supported_effect_handlers()
    {
        var registry = BattleEffectHandlerRegistry.CreateDefault();
        var codes = new[] { BattleCodes.Stun, BattleCodes.Shield, BattleCodes.Mark,
            BattleCodes.PositionSwap, BattleCodes.Silence, BattleCodes.DamageReduction,
            BattleCodes.Taunt, BattleCodes.DamageReflection };
        Assert.All(codes, code => Assert.True(registry.CanHandle(code), code));
    }

    [Fact]
    public void Shield_absorbs_damage_before_hp()
    {
        var actor = Hero(1, 0, 1000, 100);
        var target = Hero(2, 1, 1000, 10);
        Apply(new ShieldEffectHandler(), Effect(BattleCodes.Shield, baseValue: 80, duration: 2), actor, target);

        var events = Apply(new DamageEffectHandler(), Effect(BattleCodes.Damage,
            school: BattleCodes.True, baseValue: 100), actor, target);

        Assert.Equal(980, target.Hp);
        Assert.Equal(80, events.Single(x => x.EventType == "SHIELD_ABSORBED").Value);
        Assert.Equal(20, events.Single(x => x.EventType == "DAMAGE").Value);
    }

    [Fact]
    public void Damage_event_reports_actual_hp_loss_when_hit_overkills_target()
    {
        var actor = Hero(1, 0, 1000, 1000);
        var target = Hero(2, 1, 200, 10);

        var events = Apply(new DamageEffectHandler(), Effect(BattleCodes.Damage,
            school: BattleCodes.True, baseValue: 1000), actor, target);

        var damageEvent = events.Single(x => x.EventType == "DAMAGE");
        Assert.Equal(200, damageEvent.Value);
        Assert.Equal(200, damageEvent.HpBefore);
        Assert.Equal(0, damageEvent.HpAfter);
    }

    [Fact]
    public void Mark_reduction_and_reflection_are_applied_in_damage_pipeline()
    {
        var actor = Hero(1, 0, 1000, 100);
        var target = Hero(2, 1, 1000, 10);
        Apply(new MarkEffectHandler(), Effect(BattleCodes.Mark, baseValue: 20, duration: 2), actor, target);
        Apply(new DamageReductionEffectHandler(), Effect(BattleCodes.DamageReduction, baseValue: 50, duration: 2), target, target);
        Apply(new DamageReflectionEffectHandler(), Effect(BattleCodes.DamageReflection, baseValue: 25, duration: 2), target, target);

        var events = Apply(new DamageEffectHandler(), Effect(BattleCodes.Damage,
            school: BattleCodes.True, baseValue: 100), actor, target);

        Assert.Equal(940, target.Hp); // 100 * 120% * 50%
        Assert.Equal(985, actor.Hp);  // reflect 25% of 60
        Assert.Contains(events, x => x.EffectTypeCode == BattleCodes.DamageReflection && x.Value == 15);
    }

    [Fact]
    public void Generic_outgoing_and_source_restricted_incoming_modifiers_are_applied()
    {
        var actor = Hero(1, 0, 1000, 100);
        var target = Hero(2, 1, 1000, 10);
        actor.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "outgoing",
            EffectTypeCode = "ANY_OUTGOING_BUFF",
            SourceSkillId = "BUFF",
            SourceHeroId = actor.Id,
            RemainingTurns = 2,
            Stacks = 2,
            MaxStacks = 3,
            OutgoingDamageBonusPerStackPercent = 10m,
            StatModifiers = []
        });
        target.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "incoming",
            EffectTypeCode = "ANY_SOURCE_MARK",
            SourceSkillId = "MARK",
            SourceHeroId = actor.Id,
            RemainingTurns = 2,
            Stacks = 1,
            MaxStacks = 3,
            IncomingDamageBonusPerStackPercent = 25m,
            IncomingDamageBonusRestrictedToSource = true,
            StatModifiers = []
        });

        var events = Apply(new DamageEffectHandler(), Effect(BattleCodes.Damage,
            school: BattleCodes.True, baseValue: 100), actor, target);

        // 100 * 1.25 incoming vulnerability * 1.20 outgoing bonus = 150.
        Assert.Equal(150, events.Single(x => x.EventType == "DAMAGE").Value);
    }

    [Fact]
    public void Position_swap_swaps_first_two_selected_targets()
    {
        var actor = Hero(1, 0, 1000, 100);
        var first = Hero(2, 1, 1000, 10, 1);
        var second = Hero(3, 1, 1000, 10, 3);
        var handler = new PositionSwapEffectHandler();
        var events = Apply(handler, Effect(BattleCodes.PositionSwap), actor, first, [first, second]);

        Assert.Equal(3, first.Position);
        Assert.Equal(1, second.Position);
        Assert.Equal(2, events.Count);
    }

    private static IReadOnlyList<PendingBattleEvent> Apply(IBattleEffectHandler handler,
        BattleSkillEffect effect, BattleCombatant actor, BattleCombatant target,
        IReadOnlyList<BattleCombatant>? selected = null)
    {
        var skill = new BattleSkill { Id = "TEST", Name = "Test", SkillTypeCode = BattleCodes.Normal, Effects = [effect] };
        var combatants = selected ?? [target];
        return handler.Apply(new BattleEffectContext { Effect = effect, Skill = skill, Actor = actor,
            Target = target, SelectedTargets = combatants, Combatants = combatants,
            Random = new Random(1), Round = 1, Turn = 1 });
    }

    private static BattleSkillEffect Effect(string code, string? school = null,
        decimal baseValue = 0, int duration = 0) => new()
    {
        EffectTypeCode = code, TargetTypeCode = BattleCodes.EnemySingle,
        DamageSchoolCode = school, BaseValue = baseValue, DurationTurns = duration
    };

    private static BattleCombatant Hero(long id, int team, int hp, int attack, int position = 1) => new()
    {
        Id = id, SourceHeroId = id, Team = team, Position = position, Name = $"Hero {id}",
        MaxHp = hp, Hp = hp, Atk = attack, Def = 0, Spd = 100, MagicDamage = attack,
        MagicResistance = 0, BasicSkill = new BattleSkill
        {
            Id = "BASIC", Name = "Basic", SkillTypeCode = BattleCodes.Normal, Effects = []
        }
    };
}
