using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class BattleSimulationEngineTests
{
    private readonly BattleSimulationEngine _engine = new();

    [Fact]
    public void Faster_hero_acts_first()
    {
        var result = Run(Hero(1, 0, speed: 200), Hero(2, 1, speed: 100));
        Assert.Equal(1, result.Events.First(x => x.EventType == "SKILL_CAST").ActorId);
    }

    [Fact]
    public void Dead_hero_does_not_act_later_in_same_round()
    {
        var killer = Hero(1, 0, attack: 10000, speed: 200);
        var victim = Hero(2, 1, hp: 100, speed: 100);
        var result = Run(killer, victim);
        Assert.DoesNotContain(result.Events, x => x.EventType == "SKILL_CAST" && x.ActorId == victim.Id);
        Assert.Equal("LEFT", result.Winner);
    }

    [Fact]
    public void Uses_energy_skill_at_full_energy_then_spends_energy()
    {
        var hero = Hero(1, 0, energy: 100, energySkill: DamageSkill("ULT", BattleCodes.Energy, 100, 2m));
        var result = Run(hero, Hero(2, 1, hp: 5000));
        var cast = result.Events.First(x => x.EventType == "SKILL_CAST" && x.ActorId == hero.Id);
        var energy = result.Events.First(x => x.EventType == "ENERGY_CHANGED" && x.ActorId == hero.Id);
        Assert.Equal("ULT", cast.SkillId);
        Assert.Equal(100, energy.EnergyBefore);
        Assert.Equal(0, energy.EnergyAfter);
    }

    [Fact]
    public void Basic_skill_gains_energy_and_energy_skill_is_not_used_early()
    {
        var hero = Hero(1, 0, energy: 0, energySkill: DamageSkill("ULT", BattleCodes.Energy, 100, 2m));
        var result = Run(hero, Hero(2, 1, hp: 10000));
        var firstCast = result.Events.First(x => x.EventType == "SKILL_CAST" && x.ActorId == hero.Id);
        var firstEnergy = result.Events.First(x => x.EventType == "ENERGY_CHANGED" && x.ActorId == hero.Id);
        Assert.Equal("BASIC", firstCast.SkillId);
        Assert.Equal(25, firstEnergy.EnergyAfter);
    }

    [Fact]
    public void Basic_random_heal_heals_one_living_ally_without_exceeding_max_hp()
    {
        var healer = Hero(1, 0, attack: 10, basicSkill: HealSkill());
        var ally = Hero(2, 0, hp: 1000);
        ally.Hp = 200;
        var enemy = Hero(3, 1, hp: 5000, speed: 1);
        var result = Run(healer, ally, enemy);
        var heal = result.Events.First(x => x.EventType == "HEAL" && x.ActorId == healer.Id);
        Assert.True(heal.Value > 0);
        Assert.True(heal.HpAfter <= 1000);
        Assert.Contains(heal.TargetId, new long?[] { healer.Id, ally.Id });
    }

    [Fact]
    public void Magic_damage_uses_magic_resistance_not_defense()
    {
        var caster = Hero(1, 0, magicDamage: 1000, basicSkill: DamageSkill("MAGIC", BattleCodes.Normal, 0, 1m, BattleCodes.Magic, "MAGIC_DAMAGE"));
        var lowResist = Hero(2, 1, hp: 5000, defense: 10000, magicResistance: 0);
        var result = Run(caster, lowResist);
        Assert.Equal(1000, result.Events.First(x => x.EventType == "DAMAGE").Value);
    }

    [Fact]
    public void Negative_percent_modifier_reduces_stat_until_expired()
    {
        var debuff = new BattleSkill
        {
            Id = "BREAK", Name = "Break", SkillTypeCode = BattleCodes.Normal, Effects =
            [new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.StatDebuff, TargetTypeCode = BattleCodes.EnemySingle,
                DurationTurns = 2, StatModifiers = [new BattleStatModifier("DEF", "PERCENT", -50)]
            }]
        };
        var actor = Hero(1, 0, speed: 200, basicSkill: debuff);
        var ally = Hero(2, 0, attack: 1000, speed: 100);
        var enemy = Hero(3, 1, hp: 10000, defense: 100, speed: 50);
        var result = Run(actor, ally, enemy);
        var damage = result.Events.First(x => x.EventType == "DAMAGE" && x.ActorId == ally.Id);
        Assert.Equal(667, damage.Value); // 1000 * 100 / (100 + 50)
    }

    [Fact]
    public void Same_seed_produces_same_event_timeline()
    {
        var team = new[] { Hero(1, 0), Hero(2, 0), Hero(3, 1), Hero(4, 1) };
        var a = _engine.Simulate(new BattleSimulationRequest { RandomSeed = 42, MaxRounds = 5, Combatants = team });
        var b = _engine.Simulate(new BattleSimulationRequest { RandomSeed = 42, MaxRounds = 5, Combatants = team });
        Assert.Equal(a.Events.Select(EventSignature), b.Events.Select(EventSignature));
    }

    [Fact]
    public void Dispatches_effect_to_registered_handler_without_changing_engine()
    {
        var customHandler = new TestEffectHandler();
        var engine = new BattleSimulationEngine(
            new BattleEffectHandlerRegistry(new IBattleEffectHandler[] { customHandler, new DamageEffectHandler() }),
            BattleTargetSelectorRegistry.CreateDefault());
        var skill = new BattleSkill
        {
            Id = "CUSTOM_SKILL", Name = "Custom", SkillTypeCode = BattleCodes.Normal,
            Effects = [new BattleSkillEffect { EffectTypeCode = "CUSTOM", TargetTypeCode = BattleCodes.EnemySingle }]
        };

        engine.Simulate(new BattleSimulationRequest
        {
            RandomSeed = 1, MaxRounds = 1,
            Combatants = [Hero(1, 0, attack: 10000, basicSkill: skill), Hero(2, 1)]
        });

        Assert.True(customHandler.WasCalled);
    }

    [Fact]
    public void Dispatches_target_selection_to_registered_selector_without_changing_engine()
    {
        var selector = new TestTargetSelector();
        var engine = new BattleSimulationEngine(
            BattleEffectHandlerRegistry.CreateDefault(),
            new BattleTargetSelectorRegistry(new IBattleTargetSelector[] { selector }));
        var skill = DamageSkill("CUSTOM_TARGET_SKILL", BattleCodes.Normal, 0, 1m);
        skill = new BattleSkill
        {
            Id = skill.Id, Name = skill.Name, SkillTypeCode = skill.SkillTypeCode,
            Effects = [new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage, TargetTypeCode = "CUSTOM_TARGET",
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 1m)]
            }]
        };
        engine.Simulate(new BattleSimulationRequest
        {
            RandomSeed = 1, MaxRounds = 1,
            Combatants = [Hero(1, 0, attack: 10000, basicSkill: skill), Hero(2, 1)]
        });
        Assert.True(selector.WasCalled);
    }

    private BattleSimulationResult Run(params BattleCombatant[] heroes) =>
        _engine.Simulate(new BattleSimulationRequest { RandomSeed = 123, MaxRounds = 10, Combatants = heroes });

    private static BattleCombatant Hero(long id, int team, int hp = 1000, int attack = 100, int defense = 0,
        int speed = 100, int magicDamage = 100, int magicResistance = 0, int energy = 0,
        BattleSkill? basicSkill = null, BattleSkill? energySkill = null) => new()
        {
            Id = id, SourceHeroId = id, Team = team, Position = (int)id, Name = $"Hero {id}",
            MaxHp = hp, Hp = hp, Atk = attack, Def = defense, Spd = speed,
            MagicDamage = magicDamage, MagicResistance = magicResistance, Energy = energy,
            BasicSkill = basicSkill ?? DamageSkill("BASIC", BattleCodes.Normal, 0, 1m), EnergySkill = energySkill
        };

    private static BattleSkill DamageSkill(string id, string type, int cost, decimal coefficient,
        string school = BattleCodes.Physical, string attribute = "ATK") => new()
        {
            Id = id, Name = id, SkillTypeCode = type, EnergyCost = cost, Effects =
            [new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = school, Scalings = [new BattleEffectScaling(attribute, coefficient)]
            }]
        };

    private static BattleSkill HealSkill() => new()
    {
        Id = "BASIC_RANDOM_HEAL", Name = "Heal", SkillTypeCode = BattleCodes.Normal, Effects =
        [new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Heal, TargetTypeCode = BattleCodes.AllyRandom,
            Scalings = [new BattleEffectScaling("HP", 0.2m)]
        }]
    };

    private static string EventSignature(BattleEvent x) =>
        $"{x.Sequence}|{x.EventType}|{x.ActorId}|{x.TargetId}|{x.SkillId}|{x.Value}|{x.IsCrit}";

    private sealed class TestEffectHandler : IBattleEffectHandler
    {
        public string EffectTypeCode => "CUSTOM";
        public bool WasCalled { get; private set; }
        public IReadOnlyList<PendingBattleEvent> Apply(BattleEffectContext context)
        {
            WasCalled = true;
            return [];
        }
    }

    private sealed class TestTargetSelector : IBattleTargetSelector
    {
        public string TargetTypeCode => "CUSTOM_TARGET";
        public bool WasCalled { get; private set; }
        public IReadOnlyList<BattleCombatant> Select(BattleTargetContext context)
        {
            WasCalled = true;
            return context.Enemies.Take(1).ToList();
        }
    }
}
