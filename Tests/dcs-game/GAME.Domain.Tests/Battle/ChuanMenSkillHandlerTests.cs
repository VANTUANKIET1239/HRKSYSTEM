using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Reactions;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.ChuanMen;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class ChuanMenSkillHandlerTests
{
    private readonly BattleSimulationEngine _engine = new();

    private static BattleSkillEffectParameter DecimalParam(string code, decimal val) =>
        new(code, val, (int)val, null, null);
    private static BattleSkillEffectParameter IntParam(string code, int val) =>
        new(code, val, val, null, null);
    private static BattleSkillEffectParameter BoolParam(string code, bool val) =>
        new(code, null, null, val, null);

    private static Dictionary<string, BattleSkillEffectParameter> Params(params BattleSkillEffectParameter[] list) =>
        list.ToDictionary(x => x.ParameterCode, StringComparer.OrdinalIgnoreCase);

    private static BattleSkill CreateBasicSkill() => new()
    {
        Id = ChuanMenSkillCodes.Basic,
        Name = "Cú Đấm Nam Thần",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 1.10m)],
                Parameters = Params(BoolParam("CAN_CRIT", true))
            }
        ]
    };

    private static BattleSkill CreateRicardoMilosSkill() => new()
    {
        Id = ChuanMenSkillCodes.Ultimate,
        Name = "Ricardo Milos!",
        SkillTypeCode = BattleCodes.Energy,
        EnergyCost = 100,
        Effects =
        [
            // Normal: Damage 250% ATK to enemy same lane back row
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                ExecutionGroup = "NORMAL",
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySameLaneBackRow,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 2.50m)],
                Parameters = Params(BoolParam("CAN_CRIT", true))
            },
            // Normal: Stun 1 turn, 100%
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                ExecutionGroup = "NORMAL",
                EffectTypeCode = BattleCodes.Stun,
                TargetTypeCode = BattleCodes.EnemySameLaneBackRow,
                DurationTurns = 1,
                ChancePercent = 100m
            },
            // Normal: Apply RICARDO buff to self
            new BattleSkillEffect
            {
                DisplayOrder = 3,
                ExecutionGroup = "NORMAL",
                EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
                TargetTypeCode = BattleCodes.Self,
                DurationTurns = 0, // Permanent
                ChancePercent = 100m,
                MaxStacks = 6,
                StatModifiers =
                [
                    new BattleStatModifier("DEF", "PERCENT", 30m, "Phòng thủ"),
                    new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", 30m, "Kháng phép")
                ],
                Parameters = Params(
                    IntParam("RICARDO_INITIAL_STACKS", 1),
                    IntParam("RICARDO_MAX_STACKS", 6),
                    DecimalParam("RICARDO_DAMAGE_PER_STACK_PERCENT", 10m),
                    DecimalParam("RICARDO_DEF_PERCENT", 30m),
                    DecimalParam("RICARDO_MAGIC_RESISTANCE_PERCENT", 30m),
                    BoolParam("ONE_STACK_PER_ENEMY_ACTION", true),
                    BoolParam("IGNORE_DOT_FOR_STACK", true),
                    BoolParam("REQUIRE_HP_LOSS_FOR_STACK", true)
                )
            },
            // Empowered: AoE Damage 175% ATK to all living enemies
            new BattleSkillEffect
            {
                DisplayOrder = 4,
                ExecutionGroup = "EMPOWERED",
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemyAll,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 1.75m)],
                Parameters = Params(
                    BoolParam("CAN_CRIT", true),
                    BoolParam("IGNORE_OUTGOING_STATUS_DAMAGE_BONUS", true),
                    IntParam("REQUIRED_RICARDO_STACKS", 6),
                    BoolParam("CONSUME_RICARDO_AFTER_EXECUTION", true),
                    DecimalParam("EMPOWERED_DAMAGE_COEFFICIENT", 1.75m)
                )
            },
            // Empowered: Random Stun 1 turn, 100%
            new BattleSkillEffect
            {
                DisplayOrder = 5,
                ExecutionGroup = "EMPOWERED",
                EffectTypeCode = BattleCodes.Stun,
                TargetTypeCode = BattleCodes.EnemyRandom,
                DurationTurns = 1,
                ChancePercent = 100m,
                Parameters = Params(
                    DecimalParam("EMPOWERED_RANDOM_STUN_CHANCE_PERCENT", 100m),
                    IntParam("STUN_DURATION_TURNS", 1)
                )
            }
        ]
    };

    private static BattleCombatant CreateChuanMen(int position = 1, int team = 0, int energy = 0) => new()
    {
        Id = team == 0 ? 3 : -3,
        Name = "Chuẩn Men",
        Team = team,
        Position = position,
        MaxHp = 2000,
        Hp = 2000,
        Atk = 200,
        Def = 100,
        Spd = 100,
        CritChance = 0m,
        CritDamage = 150m,
        Energy = energy,
        MaxEnergy = 100,
        BasicSkill = CreateBasicSkill(),
        EnergySkill = CreateRicardoMilosSkill()
    };

    private static BattleCombatant CreateDummy(long id, int team, int position, int hp = 1000, int def = 0) => new()
    {
        Id = id,
        Name = $"Dummy_{id}",
        Team = team,
        Position = position,
        MaxHp = hp,
        Hp = hp,
        Atk = 100,
        Def = def,
        Spd = 50,
        CritChance = 0m,
        CritDamage = 150m,
        Energy = 0,
        MaxEnergy = 100,
        BasicSkill = new BattleSkill
        {
            Id = "DUMMY_BASIC",
            Name = "Dummy Basic",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.0m)]
                }
            ]
        }
    };

    [Fact]
    public void BasicAttack_Deals110PercentAtkDamage()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0);
        var enemy = CreateDummy(10, team: 1, position: 1, hp: 1000, def: 0);

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.BasicSkill,
            Actor = chuanMen,
            Combatants = [chuanMen, enemy],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        // 200 ATK * 1.10 = 220 damage
        var dmgEvent = result.Events.FirstOrDefault(e => e.EventType == "DAMAGE");
        Assert.NotNull(dmgEvent);
        Assert.Equal(220, dmgEvent.Value);
        Assert.Equal(780, enemy.Hp);
    }

    [Fact]
    public void NormalUltimate_PrioritizesSameLaneBackRow()
    {
        // Position 1 (front top) has same-lane back row at Position 2 (back top)
        var chuanMen = CreateChuanMen(position: 1, team: 0, energy: 100);
        var frontEnemy = CreateDummy(10, team: 1, position: 1, hp: 2000, def: 0);
        var backEnemy = CreateDummy(11, team: 1, position: 2, hp: 2000, def: 0);

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.EnergySkill!,
            Actor = chuanMen,
            Combatants = [chuanMen, frontEnemy, backEnemy],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        var dmgEvent = result.Events.FirstOrDefault(e => e.EventType == "DAMAGE");
        Assert.NotNull(dmgEvent);
        Assert.Equal(backEnemy.Id, dmgEvent.TargetId);
    }

    [Fact]
    public void NormalUltimate_SelectsSameLaneFrontRow_WhenBackRowDead()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0, energy: 100);
        var frontEnemy = CreateDummy(10, team: 1, position: 1, hp: 2000, def: 0);
        var deadBackEnemy = CreateDummy(11, team: 1, position: 2, hp: 0, def: 0); // Dead

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.EnergySkill!,
            Actor = chuanMen,
            Combatants = [chuanMen, frontEnemy, deadBackEnemy],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        var dmgEvent = result.Events.FirstOrDefault(e => e.EventType == "DAMAGE");
        Assert.NotNull(dmgEvent);
        Assert.Equal(frontEnemy.Id, dmgEvent.TargetId);
    }

    [Fact]
    public void NormalUltimate_RespectsTauntRedirect()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0, energy: 100);
        var backEnemy = CreateDummy(11, team: 1, position: 2, hp: 2000, def: 0);
        var taunter = CreateDummy(12, team: 1, position: 5, hp: 2000, def: 0);

        // Chuẩn Men is Taunted by taunter
        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "taunt_1",
            EffectTypeCode = BattleCodes.Taunt,
            SourceSkillId = "TAUNT_SKILL",
            SourceHeroId = taunter.Id,
            RemainingTurns = 2,
            AppliedTurn = 1,
            StatModifiers = []
        });

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.EnergySkill!,
            Actor = chuanMen,
            Combatants = [chuanMen, backEnemy, taunter],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        var dmgEvent = result.Events.FirstOrDefault(e => e.EventType == "DAMAGE");
        Assert.NotNull(dmgEvent);
        Assert.Equal(taunter.Id, dmgEvent.TargetId);
    }

    [Fact]
    public void NormalUltimate_Deals250PercentAtk_AndStunsForOneTurn_AndAppliesRicardoBuff()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0, energy: 100);
        var enemy = CreateDummy(11, team: 1, position: 2, hp: 2000, def: 0);

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.EnergySkill!,
            Actor = chuanMen,
            Combatants = [chuanMen, enemy],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        // 200 ATK * 2.50 = 500 damage
        var dmgEvent = result.Events.FirstOrDefault(e => e.EventType == "DAMAGE");
        Assert.NotNull(dmgEvent);
        Assert.Equal(500, dmgEvent.Value);

        // Stun applied to enemy
        var stunEvent = result.Events.FirstOrDefault(e => e.EventType == "STATUS_APPLIED" && e.EffectTypeCode == BattleCodes.Stun);
        Assert.NotNull(stunEvent);
        Assert.Equal(enemy.Id, stunEvent.TargetId);
        Assert.Equal(1, stunEvent.RemainingTurns);
        Assert.Contains(enemy.StatusEffects, s => s.EffectTypeCode == BattleCodes.Stun && s.RemainingTurns == 1);

        // RICARDO applied to Chuẩn Men with 1 stack
        var ricardoStatus = chuanMen.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.NotNull(ricardoStatus);
        Assert.Equal(1, ricardoStatus.Stacks);
        Assert.Equal(6, ricardoStatus.MaxStacks);
        Assert.Equal(-1, ricardoStatus.RemainingTurns); // Permanent

        var ricardoEvent = result.Events.FirstOrDefault(e => e.EventType == "STATUS_APPLIED" && e.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.NotNull(ricardoEvent);
        Assert.Equal(1, ricardoEvent.CurrentStacks);
        Assert.Equal(6, ricardoEvent.MaxStacks);
    }

    [Fact]
    public void NormalUltimate_RecastKeepsExistingStacks_DoesNotResetOrAdd()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0, energy: 100);
        var enemy = CreateDummy(11, team: 1, position: 2, hp: 2000, def: 0);

        // Pre-existing RICARDO with 3 stacks
        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{chuanMen.Id}:{ChuanMenSkillCodes.Ultimate}:{ChuanMenSkillCodes.RicardoStatus}:{chuanMen.Id}",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 3,
            MaxStacks = 6,
            Value = 10m,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = [new BattleStatModifier("DEF", "PERCENT", 30m)]
        });

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.EnergySkill!,
            Actor = chuanMen,
            Combatants = [chuanMen, enemy],
            Random = new Random(42),
            Round = 2,
            Turn = 3
        };

        var result = handler.Execute(context);

        var ricardoStatus = chuanMen.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.NotNull(ricardoStatus);
        Assert.Equal(3, ricardoStatus.Stacks); // Stacks must NOT reset to 1 and must NOT become 4

        // Emits STATUS_REFRESHED with CurrentStacks = 3
        var refreshedEvent = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.StatusRefreshed && e.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.NotNull(refreshedEvent);
        Assert.Equal(3, refreshedEvent.CurrentStacks);
    }

    [Fact]
    public void OnDamaged_EnemyDirectAttackReducesHp_GainsOneStack()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0);
        var enemy = CreateDummy(10, team: 1, position: 1, hp: 1000);

        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ricardo_1",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 6,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = []
        });

        var reactionHandler = new RicardoStatusReactionHandler();
        var registry = new BattleStatusReactionHandlerRegistry([reactionHandler]);
        var damageHandler = new DamageEffectHandler(registry);

        var dmgContext = new BattleEffectContext
        {
            Effect = new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 1.0m)]
            },
            Skill = enemy.BasicSkill,
            Actor = enemy,
            Target = chuanMen,
            SelectedTargets = [chuanMen],
            Combatants = [chuanMen, enemy],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "turn_1_enemy_10"
        };

        var result = damageHandler.ExecuteDamage(dmgContext);

        var ricardoStatus = chuanMen.StatusEffects.First(s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.Equal(2, ricardoStatus.Stacks);

        var stackEvent = result.EmittedEvents.FirstOrDefault(e => e.EventType == BattleCodes.StatusStackChanged);
        Assert.NotNull(stackEvent);
        Assert.Equal(1, stackEvent.PreviousStacks);
        Assert.Equal(2, stackEvent.CurrentStacks);
        Assert.Equal(6, stackEvent.MaxStacks);
    }

    [Fact]
    public void OnDamaged_MultiHitEnemyAction_OnlyGainsOneStackTotal()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0);
        var enemy = CreateDummy(10, team: 1, position: 1, hp: 1000);

        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ricardo_1",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 6,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = []
        });

        var reactionHandler = new RicardoStatusReactionHandler();
        var registry = new BattleStatusReactionHandlerRegistry([reactionHandler]);
        var damageHandler = new DamageEffectHandler(registry);

        const string enemyActionId = "turn_2_smile_ult";

        // Hit 1
        var hit1 = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle, DamageSchoolCode = BattleCodes.Physical, Scalings = [new BattleEffectScaling("ATK", 0.5m)] },
            Skill = enemy.BasicSkill, Actor = enemy, Target = chuanMen, SelectedTargets = [chuanMen], Combatants = [chuanMen, enemy], Random = new Random(42), Round = 1, Turn = 2, ActionId = enemyActionId
        });

        // Hit 2
        var hit2 = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle, DamageSchoolCode = BattleCodes.Physical, Scalings = [new BattleEffectScaling("ATK", 0.5m)] },
            Skill = enemy.BasicSkill, Actor = enemy, Target = chuanMen, SelectedTargets = [chuanMen], Combatants = [chuanMen, enemy], Random = new Random(42), Round = 1, Turn = 2, ActionId = enemyActionId
        });

        // Hit 3
        var hit3 = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle, DamageSchoolCode = BattleCodes.Physical, Scalings = [new BattleEffectScaling("ATK", 0.5m)] },
            Skill = enemy.BasicSkill, Actor = enemy, Target = chuanMen, SelectedTargets = [chuanMen], Combatants = [chuanMen, enemy], Random = new Random(42), Round = 1, Turn = 2, ActionId = enemyActionId
        });

        var ricardoStatus = chuanMen.StatusEffects.First(s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.Equal(2, ricardoStatus.Stacks); // Gained exactly 1 stack, NOT 3!

        Assert.Contains(hit1.EmittedEvents, e => e.EventType == BattleCodes.StatusStackChanged);
        Assert.DoesNotContain(hit2.EmittedEvents, e => e.EventType == BattleCodes.StatusStackChanged);
        Assert.DoesNotContain(hit3.EmittedEvents, e => e.EventType == BattleCodes.StatusStackChanged);
    }

    [Fact]
    public void OnDamaged_FullShieldAbsorb_ZeroHpLoss_GainsNoStack()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0);
        var enemy = CreateDummy(10, team: 1, position: 1, hp: 1000);

        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ricardo_1",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 2,
            MaxStacks = 6,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = []
        });

        // Giant shield absorbing all damage
        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "shield_1",
            EffectTypeCode = BattleCodes.Shield,
            SourceSkillId = "SHIELD_SKILL",
            SourceHeroId = chuanMen.Id,
            RemainingTurns = 2,
            AppliedTurn = 1,
            ShieldRemaining = 5000,
            StatModifiers = []
        });

        var hpBefore = chuanMen.Hp;

        var reactionHandler = new RicardoStatusReactionHandler();
        var registry = new BattleStatusReactionHandlerRegistry([reactionHandler]);
        var damageHandler = new DamageEffectHandler(registry);

        var result = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle, DamageSchoolCode = BattleCodes.Physical, Scalings = [new BattleEffectScaling("ATK", 1.0m)] },
            Skill = enemy.BasicSkill, Actor = enemy, Target = chuanMen, SelectedTargets = [chuanMen], Combatants = [chuanMen, enemy], Random = new Random(42), Round = 1, Turn = 1, ActionId = "action_shield_test"
        });

        Assert.Equal(hpBefore, chuanMen.Hp); // No HP loss
        var ricardoStatus = chuanMen.StatusEffects.First(s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.Equal(2, ricardoStatus.Stacks); // Stacks remained at 2
        Assert.DoesNotContain(result.EmittedEvents, e => e.EventType == BattleCodes.StatusStackChanged);
    }

    [Fact]
    public void OnDamaged_StacksCappedAtSix_AndEmitsRageReady()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0);
        var enemy = CreateDummy(10, team: 1, position: 1, hp: 1000);

        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ricardo_1",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 5, // Currently at 5 stacks
            MaxStacks = 6,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = []
        });

        var reactionHandler = new RicardoStatusReactionHandler();
        var registry = new BattleStatusReactionHandlerRegistry([reactionHandler]);
        var damageHandler = new DamageEffectHandler(registry);

        // Attack 1: 5 -> 6 stacks
        var res1 = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle, DamageSchoolCode = BattleCodes.Physical, Scalings = [new BattleEffectScaling("ATK", 1.0m)] },
            Skill = enemy.BasicSkill, Actor = enemy, Target = chuanMen, SelectedTargets = [chuanMen], Combatants = [chuanMen, enemy], Random = new Random(42), Round = 1, Turn = 1, ActionId = "turn_1"
        });

        var ricardoStatus = chuanMen.StatusEffects.First(s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.Equal(6, ricardoStatus.Stacks);
        Assert.Contains(res1.EmittedEvents, e => e.EventType == BattleCodes.RicardoRageReady);

        // Attack 2: at 6 stacks, cannot exceed 6
        var res2 = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle, DamageSchoolCode = BattleCodes.Physical, Scalings = [new BattleEffectScaling("ATK", 1.0m)] },
            Skill = enemy.BasicSkill, Actor = enemy, Target = chuanMen, SelectedTargets = [chuanMen], Combatants = [chuanMen, enemy], Random = new Random(42), Round = 1, Turn = 2, ActionId = "turn_2"
        });

        Assert.Equal(6, ricardoStatus.Stacks);
        Assert.DoesNotContain(res2.EmittedEvents, e => e.EventType == BattleCodes.StatusStackChanged);
    }

    [Fact]
    public void RicardoStatus_DamageDealtBonus_ScalesWithStacks_AndDefDoesNotScale()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0);
        var enemy = CreateDummy(10, team: 1, position: 1, hp: 5000, def: 0);

        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ricardo_1",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 3, // +30% damage dealt
            MaxStacks = 6,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = [new BattleStatModifier("DEF", "PERCENT", 30m)]
        });

        // 1. DEF check: Base DEF is 100. +30% = 130. It should NOT be 100 + 30% * 3 = 190!
        var effectiveDef = BattleStatCalculator.GetEffectiveStat(chuanMen, "DEF");
        Assert.Equal(130m, effectiveDef);

        // 2. Damage check: Base damage with 200 ATK is 220 (110%). With 3 stacks (+30%), damage is 220 * 1.30 = 286.
        var damageHandler = new DamageEffectHandler();
        var result = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 1.10m)]
            },
            Skill = chuanMen.BasicSkill,
            Actor = chuanMen,
            Target = enemy,
            SelectedTargets = [enemy],
            Combatants = [chuanMen, enemy],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        });

        var dmgEvent = result.EmittedEvents.First(e => e.EventType == "DAMAGE");
        Assert.Equal(286, dmgEvent.Value);
    }

    [Fact]
    public void EmpoweredUltimate_TriggersWhenSixStacksAndFullEnergy_Deals175PercentAoE_AndConsumesBuff()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0, energy: 100);
        var enemy1 = CreateDummy(10, team: 1, position: 1, hp: 2000, def: 0);
        var enemy2 = CreateDummy(11, team: 1, position: 2, hp: 2000, def: 0);
        var enemy3 = CreateDummy(12, team: 1, position: 4, hp: 2000, def: 0);

        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ricardo_1",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 6, // 6 stacks unlocked!
            MaxStacks = 6,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = [new BattleStatModifier("DEF", "PERCENT", 30m)]
        });

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.EnergySkill!,
            Actor = chuanMen,
            Combatants = [chuanMen, enemy1, enemy2, enemy3],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        // 1. AoE damage dealt to all 3 alive enemies
        var dmgEvents = result.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(3, dmgEvents.Count);

        // Base ATK is 200 * 1.75 = 350.
        // MUST NOT have the +60% bonus from RICARDO!
        foreach (var dmg in dmgEvents)
        {
            Assert.Equal(350, dmg.Value);
        }

        // 2. Exactly one random alive enemy is Stunned for 1 turn
        var stunEvents = result.Events.Where(e => e.EventType == "STATUS_APPLIED" && e.EffectTypeCode == BattleCodes.Stun).ToList();
        Assert.Single(stunEvents);
        Assert.Equal(1, stunEvents[0].RemainingTurns);

        // 3. RICARDO buff is consumed and removed from Chuẩn Men
        Assert.DoesNotContain(chuanMen.StatusEffects, s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);

        var removedEvent = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.StatusRemoved && e.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
        Assert.NotNull(removedEvent);
        Assert.Equal(6, removedEvent.PreviousStacks);
        Assert.Equal(0, removedEvent.CurrentStacks);

        var consumedEvent = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.RicardoConsumed);
        Assert.NotNull(consumedEvent);
        Assert.Equal(0, consumedEvent.CurrentStacks);
    }

    [Fact]
    public void EmpoweredUltimate_WhenAllEnemiesDie_SkipsStunSafely()
    {
        var chuanMen = CreateChuanMen(position: 1, team: 0, energy: 100);
        var weakEnemy = CreateDummy(10, team: 1, position: 1, hp: 100, def: 0); // 100 HP < 350 damage

        chuanMen.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ricardo_1",
            EffectTypeCode = ChuanMenSkillCodes.RicardoStatus,
            SourceSkillId = ChuanMenSkillCodes.Ultimate,
            SourceHeroId = chuanMen.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 6,
            MaxStacks = 6,
            DamageBonusPerStackPercent = 10m,
            ScaleModifiersWithStacks = false,
            StatModifiers = []
        });

        var handlers = BattleEffectHandlerRegistry.CreateDefault();
        var targetSelectors = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(handlers, targetSelectors);
        var handler = ChuanMenSkillHandler.Create(defaultHandler, handlers, targetSelectors);

        var context = new SkillExecutionContext
        {
            Skill = chuanMen.EnergySkill!,
            Actor = chuanMen,
            Combatants = [chuanMen, weakEnemy],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        // Target died
        Assert.False(weakEnemy.IsAlive);

        // Stun step was safely skipped because no enemies survived
        Assert.DoesNotContain(result.Events, e => e.EventType == "STATUS_APPLIED" && e.EffectTypeCode == BattleCodes.Stun);

        // RICARDO still consumed cleanly
        Assert.DoesNotContain(chuanMen.StatusEffects, s => s.EffectTypeCode == ChuanMenSkillCodes.RicardoStatus);
    }
}
