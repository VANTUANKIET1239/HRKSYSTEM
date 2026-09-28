using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Reactions;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.NghiaPhucPrime;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class NghiaPhucPrimeSkillHandlerTests
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

    public static BattleSkill CreateBasicSkill() => new()
    {
        Id = NghiaPhucPrimeSkillCodes.Basic,
        Name = "Khiên Này Có Bảo Hành",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.90m)],
                Parameters = Params(
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamDamageCoefficient, 0.90m),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamAllyShieldMaxHpPercent, 8m),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamExistingShieldRestorePercent, 4m),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamShieldDurationTurns, 2),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamFortitudeMaxStacks, 4),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamFortitudeDefPercentPerStack, 5m),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamFortitudeMagicResistancePercentPerStack, 5m),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamSelfShieldOnMaxStackPercent, 12m),
                    BoolParam(NghiaPhucPrimeSkillCodes.ParamCanCrit, true)
                )
            }
        ]
    };

    public static BattleSkill CreateUltimateSkill() => new()
    {
        Id = NghiaPhucPrimeSkillCodes.Ultimate,
        Name = "Thành Trì Prime: Không Ai Được Phép Ngã",
        SkillTypeCode = BattleCodes.Energy,
        EnergyCost = 100,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemyAll,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.60m)],
                Parameters = Params(
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamFortressDamageCoefficient, 0.60m),
                    BoolParam(NghiaPhucPrimeSkillCodes.ParamCanCrit, false),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamStaggerDurationTurns, 1),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamActionBarReduction, 15),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamSpeedReductionPercent, 10m),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamBrokenMoraleTargetCount, 3),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamOutgoingDamageReductionPercent, 15m),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamBrokenMoraleDurationTurns, 2),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamTeamShieldCasterMaxHpPercent, 10m),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamTeamShieldCasterDefPercent, 120m),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamTeamShieldTargetMaxHpCapPercent, 25m),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamTeamShieldDurationTurns, 2),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamGuardianDurationTurns, 2),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamDamageRedirectPercent, 35m),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamGuardianMinHp, 1),
                    IntParam(NghiaPhucPrimeSkillCodes.ParamPressureMaxStacks, 5),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamPressureHealMaxHpPercentPerStack, 2m),
                    DecimalParam(NghiaPhucPrimeSkillCodes.ParamPressureDamageDefPercentPerStack, 20m),
                    BoolParam(NghiaPhucPrimeSkillCodes.ParamPressureCanCrit, false),
                    BoolParam(NghiaPhucPrimeSkillCodes.ParamPressureReleaseAtMax, true)
                )
            }
        ]
    };

    public static BattleCombatant CreatePrime(long id = 1, int team = 0, int hp = 1000, int atk = 100, int def = 100, decimal critChance = 0m) => new()
    {
        Id = id,
        SourceHeroId = id,
        Team = team,
        Position = 1,
        Name = "Nghĩa Phục Prime",
        MaxHp = hp,
        Hp = hp,
        Atk = atk,
        Def = def,
        Spd = 90,
        MagicDamage = 40,
        MagicResistance = 80,
        CritChance = critChance,
        CritDamage = 150m,
        Energy = 0,
        MaxEnergy = 100,
        BasicSkill = CreateBasicSkill(),
        EnergySkill = CreateUltimateSkill()
    };

    public static BattleCombatant CreateDummy(long id, int team, int hp = 1000, int def = 0, int spd = 100) => new()
    {
        Id = id,
        SourceHeroId = id,
        Team = team,
        Position = (int)id,
        Name = $"Dummy {id}",
        MaxHp = hp,
        Hp = hp,
        Atk = 50,
        Def = def,
        Spd = spd,
        MagicDamage = 0,
        MagicResistance = 0,
        CritChance = 0m,
        CritDamage = 150m,
        Energy = 50,
        MaxEnergy = 100,
        BasicSkill = new BattleSkill
        {
            Id = "DUMMY_BASIC",
            Name = "Dummy Strike",
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

    // =========================================================================
    // 1. BASIC SKILL TESTS
    // =========================================================================

    [Fact]
    public void BasicSkill_Deals90PercentAtkBeforeDefense()
    {
        var prime = CreatePrime(atk: 200); // 90% of 200 = 180
        var enemy = CreateDummy(2, team: 1, hp: 1000, def: 0); // 0 def -> 180 dmg

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        var result = handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill,
            Actor = prime,
            Combatants = [prime, enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "action_1"
        });

        var dmgEvent = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.Damage);
        Assert.NotNull(dmgEvent);
        Assert.Equal(180, dmgEvent.Value);
        Assert.Equal(820, enemy.Hp);
    }

    [Fact]
    public void BasicSkill_ShieldsLowestHpPercentAlly_UsingCasterMaxHp()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 2000); // 8% of 2000 = 160
        var ally1 = CreateDummy(2, team: 0, hp: 1000);
        ally1.Hp = 500; // 50% HP
        var ally2 = CreateDummy(3, team: 0, hp: 1000);
        ally2.Hp = 800; // 80% HP
        var enemy = CreateDummy(4, team: 1, hp: 1000);

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        var result = handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill,
            Actor = prime,
            Combatants = [prime, ally1, ally2, enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "action_1"
        });

        // Shield should go to ally1 (lowest HP%)
        var shield = ally1.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.Shield);
        Assert.NotNull(shield);
        Assert.Equal(160, shield.ShieldRemaining); // 8% of Prime's MaxHp (2000), not ally's
        Assert.Equal(2, shield.RemainingTurns);
    }

    [Fact]
    public void BasicSkill_ExistingShieldRestoredAndRefreshed()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 2000);
        var ally = CreateDummy(2, team: 0, hp: 1000);
        ally.Hp = 500;
        var enemy = CreateDummy(3, team: 1, hp: 1000);

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        // First cast -> creates 160 shield
        handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill,
            Actor = prime,
            Combatants = [prime, ally, enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "action_1"
        });

        var shield = ally.StatusEffects.First(s => s.EffectTypeCode == BattleCodes.Shield);
        Assert.Equal(160, shield.ShieldRemaining);
        shield.RemainingTurns = 1; // Simulate 1 turn elapsed

        // Second cast -> restores 4% of 2000 = 80, refreshes to 2 turns
        handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill,
            Actor = prime,
            Combatants = [prime, ally, enemy],
            Random = new Random(1),
            Round = 2,
            Turn = 2,
            ActionId = "action_2"
        });

        // Still single shield instance, not duplicate
        var shields = ally.StatusEffects.Where(s => s.EffectTypeCode == BattleCodes.Shield).ToList();
        Assert.Single(shields);
        Assert.Equal(240, shields[0].ShieldRemaining); // 160 + 80
        Assert.Equal(2, shields[0].RemainingTurns);
    }

    [Fact]
    public void BasicSkill_KiênCố_AccumulatesUpTo4Stacks_AndIncreasesStats()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000, def: 100);
        var enemy = CreateDummy(2, team: 1, hp: 1000);

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        // Cast 1
        handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill, Actor = prime, Combatants = [prime, enemy],
            Random = new Random(1), Round = 1, Turn = 1, ActionId = "action_1"
        });

        var fort = prime.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.Fortitude);
        Assert.NotNull(fort);
        Assert.Equal(1, fort.Stacks);
        // +5% DEF -> 100 + 5 = 105
        Assert.Equal(105m, BattleStatCalculator.GetEffectiveStat(prime, "DEF"));

        // Cast 2
        handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill, Actor = prime, Combatants = [prime, enemy],
            Random = new Random(1), Round = 1, Turn = 2, ActionId = "action_2"
        });
        Assert.Equal(2, fort.Stacks);
        // +10% DEF -> 100 + 10 = 110
        Assert.Equal(110m, BattleStatCalculator.GetEffectiveStat(prime, "DEF"));

        // Cast 3
        handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill, Actor = prime, Combatants = [prime, enemy],
            Random = new Random(1), Round = 1, Turn = 3, ActionId = "action_3"
        });
        Assert.Equal(3, fort.Stacks);
        // +15% DEF -> 100 + 15 = 115
        Assert.Equal(115m, BattleStatCalculator.GetEffectiveStat(prime, "DEF"));
    }

    [Fact]
    public void BasicSkill_At4Stacks_Grants12PercentSelfShield_AndResetsFortitude()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000);
        var enemy = CreateDummy(2, team: 1, hp: 1000);

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        // 4 casts
        for (int i = 1; i <= 3; i++)
        {
            handler.Execute(new SkillExecutionContext
            {
                Skill = prime.BasicSkill, Actor = prime, Combatants = [prime, enemy],
                Random = new Random(1), Round = 1, Turn = i, ActionId = $"action_{i}"
            });
        }

        var result4 = handler.Execute(new SkillExecutionContext
        {
            Skill = prime.BasicSkill, Actor = prime, Combatants = [prime, enemy],
            Random = new Random(1), Round = 1, Turn = 4, ActionId = "action_4"
        });

        // Fortitude consumed event
        Assert.Contains(result4.Events, e => e.EventType == NghiaPhucPrimeSkillCodes.EventFortitudeConsumed);

        // Fortitude status removed from prime
        Assert.DoesNotContain(prime.StatusEffects, s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.Fortitude);

        // Self-shield 12% of 1000 = 120 exists on Prime
        var selfShield = prime.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.Shield);
        Assert.NotNull(selfShield);
        Assert.True(selfShield.ShieldRemaining >= 120);
    }

    // =========================================================================
    // 2. ULTIMATE SKILL TESTS
    // =========================================================================

    [Fact]
    public void UltimateSkill_Deals60PercentAtkToAllAliveEnemies_CannotCrit()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000, atk: 100, critChance: 100m); // Forced 100% crit to prove CAN_CRIT = false
        var enemy1 = CreateDummy(2, team: 1, hp: 1000, def: 0);
        var enemy2 = CreateDummy(3, team: 1, hp: 1000, def: 0);

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        var result = handler.Execute(new SkillExecutionContext
        {
            Skill = prime.EnergySkill!,
            Actor = prime,
            Combatants = [prime, enemy1, enemy2],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "ult_1"
        });

        var dmgEvents = result.Events.Where(e => e.EventType == BattleCodes.Damage).ToList();
        Assert.Equal(2, dmgEvents.Count);
        foreach (var dmg in dmgEvents)
        {
            Assert.Equal(60, dmg.Value); // 60% of 100, no crit
            Assert.False(dmg.IsCrit);
        }
    }

    [Fact]
    public void UltimateSkill_StaggerReduces15ActionBar_And10PercentSpeed()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000);
        var enemy = CreateDummy(2, team: 1, hp: 1000, spd: 100);
        enemy.Energy = 50;

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        var result = handler.Execute(new SkillExecutionContext
        {
            Skill = prime.EnergySkill!,
            Actor = prime,
            Combatants = [prime, enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "ult_1"
        });

        // Action bar reduced by 15 points
        Assert.Equal(35, enemy.Energy);
        var abEvent = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.ActionBarChanged);
        Assert.NotNull(abEvent);
        Assert.Equal(50, abEvent.PreviousValue);
        Assert.Equal(35, abEvent.CurrentValue);

        // Stagger status with -10% SPD
        var stagger = enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.Stagger);
        Assert.NotNull(stagger);
        Assert.Equal(1, stagger.RemainingTurns);
        Assert.Equal(90m, BattleStatCalculator.GetEffectiveStat(enemy, "SPD"));
    }

    [Fact]
    public void UltimateSkill_BrokenMorale_DeterministicSelectionAndOutgoingDamageReduction()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000);
        var enemies = Enumerable.Range(2, 5).Select(i => CreateDummy(i, team: 1, hp: 1000)).ToList();

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        // Run 1 with seed 42
        var combatants1 = new List<BattleCombatant> { prime };
        combatants1.AddRange(enemies);

        handler.Execute(new SkillExecutionContext
        {
            Skill = prime.EnergySkill!,
            Actor = prime,
            Combatants = combatants1,
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "ult_run1"
        });

        var debuffed1 = combatants1.Where(c => c.Team == 1 && c.StatusEffects.Any(s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.BrokenMorale))
            .Select(c => c.Id).OrderBy(x => x).ToList();

        Assert.Equal(3, debuffed1.Count); // Max 3 targets

        // Check -15% outgoing damage on one of the debuffed enemies
        var debuffedEnemy = combatants1.First(c => debuffed1.Contains(c.Id));
        var moraleStatus = debuffedEnemy.StatusEffects.First(s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.BrokenMorale);
        Assert.Equal(-15m, moraleStatus.OutgoingDamageBonusPerStackPercent);
    }

    [Fact]
    public void UltimateSkill_TeamShield_FormulaAndCap()
    {
        // Formula: Prime.MaxHp * 10% + Prime.EffectiveDef * 120%
        // Prime: MaxHp 1000, Def 100 -> 1000*0.1 + 100*1.2 = 100 + 120 = 220
        var prime = CreatePrime(id: 1, team: 0, hp: 1000, def: 100);
        // Ally 1: MaxHp 2000 -> Cap = 2000 * 25% = 500 (220 < 500 -> 220)
        var ally1 = CreateDummy(2, team: 0, hp: 2000);
        // Ally 2: MaxHp 400 -> Cap = 400 * 25% = 100 (220 > 100 -> capped at 100)
        var ally2 = CreateDummy(3, team: 0, hp: 400);
        var enemy = CreateDummy(4, team: 1, hp: 1000);

        var handler = NghiaPhucPrimeSkillHandler.Create(
            new DefaultSkillHandler(BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()),
            BattleEffectHandlerRegistry.CreateDefault());

        handler.Execute(new SkillExecutionContext
        {
            Skill = prime.EnergySkill!,
            Actor = prime,
            Combatants = [prime, ally1, ally2, enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "ult_shield"
        });

        var shield1 = ally1.StatusEffects.First(s => s.EffectTypeCode == BattleCodes.Shield);
        Assert.Equal(220, shield1.ShieldRemaining);

        var shield2 = ally2.StatusEffects.First(s => s.EffectTypeCode == BattleCodes.Shield);
        Assert.Equal(100, shield2.ShieldRemaining); // Capped at 25% of 400
    }

    // =========================================================================
    // 3. GUARDIAN DAMAGE REDIRECTION TESTS
    // =========================================================================

    [Fact]
    public void Guardian_Redirects35PercentDirectDamageToPrime()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000);
        var ally = CreateDummy(2, team: 0, hp: 1000);
        var attacker = CreateDummy(3, team: 1, hp: 1000, def: 0);

        // Give Prime Guardian status
        prime.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Guardian}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = 2,
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 1
        });

        var redirectRegistry = DamageRedirectHandlerRegistry.CreateDefault();
        var reactionRegistry = BattleStatusReactionHandlerRegistry.CreateDefault();
        var damageHandler = new DamageEffectHandler(reactionRegistry, redirectRegistry);

        // Attacker hits ally for 100 damage (0 def)
        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            DamageSchoolCode = BattleCodes.Physical,
            TargetTypeCode = BattleCodes.EnemySingle,
            BaseValue = 100,
            Parameters = Params(BoolParam("CAN_CRIT", false))
        };

        var result = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = effect,
            Skill = attacker.BasicSkill,
            Actor = attacker,
            Target = ally,
            SelectedTargets = [ally],
            Combatants = [prime, ally, attacker],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "attack_1"
        });

        // 35% of 100 = 35 redirected to Prime
        // Ally takes remaining 65 damage
        Assert.Equal(1000 - 65, ally.Hp);
        Assert.Equal(1000 - 35, prime.Hp);

        // Verify PRIME_GUARD_REDIRECTED event
        var redirectEvt = result.EmittedEvents.FirstOrDefault(e => e.EventType == NghiaPhucPrimeSkillCodes.EventGuardRedirected);
        Assert.NotNull(redirectEvt);
        Assert.Equal(35, redirectEvt.Value);
        Assert.Equal(100, redirectEvt.OriginalDamage);
        Assert.Equal(65, redirectEvt.AllyDamageAfterRedirect);
    }

    [Fact]
    public void Guardian_DoesNotDropPrimeBelow1Hp_RemainingReturnsToAlly()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 10); // Prime has only 10 HP (max absorb = 9 HP down to 1)
        var ally = CreateDummy(2, team: 0, hp: 1000);
        var attacker = CreateDummy(3, team: 1, hp: 1000);

        prime.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Guardian}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = 2,
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 1
        });

        var redirectRegistry = DamageRedirectHandlerRegistry.CreateDefault();
        var damageHandler = new DamageEffectHandler(BattleStatusReactionHandlerRegistry.CreateDefault(), redirectRegistry);

        // 100 damage: 35 requested, but Prime can only absorb 9 HP (down to 1 HP).
        // Remaining 91 damage goes to Ally!
        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            DamageSchoolCode = BattleCodes.Physical,
            TargetTypeCode = BattleCodes.EnemySingle,
            BaseValue = 100,
            Parameters = Params(BoolParam("CAN_CRIT", false))
        };

        damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = effect,
            Skill = attacker.BasicSkill,
            Actor = attacker,
            Target = ally,
            SelectedTargets = [ally],
            Combatants = [prime, ally, attacker],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "attack_survival"
        });

        Assert.Equal(1, prime.Hp); // Never drops below 1 HP
        Assert.Equal(1000 - 91, ally.Hp); // 100 - 9 = 91 damage taken by ally (no lost damage!)
    }

    [Fact]
    public void Guardian_DoesNotRedirectDOTOrReflection()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000);
        var ally = CreateDummy(2, team: 0, hp: 1000);
        var attacker = CreateDummy(3, team: 1, hp: 1000);

        prime.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Guardian}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = 2,
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 1
        });

        var reactionHandler = new NghiaPhucPrimeReactionHandler();

        // Bleed / DOT context
        var dotContext = new DamageRedirectContext
        {
            Actor = attacker,
            Target = ally,
            Skill = attacker.BasicSkill,
            Effect = new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.BleedDamage,
                TargetTypeCode = BattleCodes.EnemySingle
            },
            IncomingDamage = 100,
            ActionId = "dot_action",
            Round = 1,
            Turn = 1,
            Combatants = [prime, ally, attacker],
            Random = new Random(1)
        };

        var result = reactionHandler.HandleRedirect(dotContext);
        Assert.Null(result); // DOT is not redirected
    }

    [Fact]
    public void Guardian_AoEAction_OnlyAddsMax1PressureStack()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000);
        var ally1 = CreateDummy(2, team: 0, hp: 1000);
        var ally2 = CreateDummy(3, team: 0, hp: 1000);
        var attacker = CreateDummy(4, team: 1, hp: 1000);

        prime.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Guardian}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = 2,
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 1
        });

        var redirectRegistry = DamageRedirectHandlerRegistry.CreateDefault();
        var damageHandler = new DamageEffectHandler(BattleStatusReactionHandlerRegistry.CreateDefault(), redirectRegistry);

        // Same AoE action hitting both ally1 and ally2
        var aoeActionId = "enemy_aoe_action_1";
        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            DamageSchoolCode = BattleCodes.Physical,
            TargetTypeCode = BattleCodes.EnemyAll,
            BaseValue = 100,
            Parameters = Params(BoolParam("CAN_CRIT", false))
        };

        damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = effect,
            Skill = attacker.BasicSkill,
            Actor = attacker,
            Target = ally1,
            SelectedTargets = [ally1, ally2],
            Combatants = [prime, ally1, ally2, attacker],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = aoeActionId
        });

        damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = effect,
            Skill = attacker.BasicSkill,
            Actor = attacker,
            Target = ally2,
            SelectedTargets = [ally1, ally2],
            Combatants = [prime, ally1, ally2, attacker],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = aoeActionId
        });

        var pressure = prime.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.Pressure);
        Assert.NotNull(pressure);
        Assert.Equal(1, pressure.Stacks); // Max 1 stack per action, even with AoE hitting multiple allies!
    }

    // =========================================================================
    // 4. PRESSURE RELEASE TESTS
    // =========================================================================

    [Fact]
    public void Pressure_At5Stacks_TriggersImmediateRelease_HealsPrimeAndDamagesEnemies()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000, def: 200);
        prime.Hp = 800; // Missing 200 HP
        var ally = CreateDummy(2, team: 0, hp: 1000);
        var enemy = CreateDummy(3, team: 1, hp: 1000, def: 0);

        prime.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Guardian}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = 2,
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 1
        });

        // Set existing pressure to 4 stacks
        prime.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Pressure}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Pressure,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 4,
            MaxStacks = 5
        });

        var redirectRegistry = DamageRedirectHandlerRegistry.CreateDefault();
        var damageHandler = new DamageEffectHandler(BattleStatusReactionHandlerRegistry.CreateDefault(), redirectRegistry);

        // 5th hit arrives
        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            DamageSchoolCode = BattleCodes.Physical,
            TargetTypeCode = BattleCodes.EnemySingle,
            BaseValue = 50,
            Parameters = Params(BoolParam("CAN_CRIT", false))
        };

        var result = damageHandler.ExecuteDamage(new BattleEffectContext
        {
            Effect = effect,
            Skill = enemy.BasicSkill,
            Actor = enemy,
            Target = ally,
            SelectedTargets = [ally],
            Combatants = [prime, ally, enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1,
            ActionId = "action_5th_hit"
        });

        // Event PRIME_PRESSURE_RELEASED emitted
        Assert.Contains(result.EmittedEvents, e => e.EventType == NghiaPhucPrimeSkillCodes.EventPressureReleased);

        // At 5 stacks:
        // Heal = 1000 * 2% * 5 = 100 HP healed to Prime
        var healEvent = result.EmittedEvents.FirstOrDefault(e => e.EventType == BattleCodes.Heal && e.TargetId == prime.Id);
        Assert.NotNull(healEvent);
        Assert.Equal(100, healEvent.Value);

        // Damage = 200 DEF * 20% * 5 = 200 Raw Damage (0 def enemy -> 200 dmg)
        var dmgRelease = result.EmittedEvents.FirstOrDefault(e => e.EventType == BattleCodes.Damage && e.ActorId == prime.Id);
        Assert.NotNull(dmgRelease);
        Assert.Equal(200, dmgRelease.Value);
        Assert.False(dmgRelease.IsCrit);

        // Both Guardian and Pressure status removed
        Assert.DoesNotContain(prime.StatusEffects, s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.Pressure);
        Assert.DoesNotContain(prime.StatusEffects, s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.Guardian);
    }

    [Fact]
    public void Pressure_ReleasesOnGuardianExpiration_BasedOnCurrentStacks()
    {
        var prime = CreatePrime(id: 1, team: 0, hp: 1000, def: 100);
        prime.Hp = 900;
        var enemy = CreateDummy(2, team: 1, hp: 1000, def: 0);

        var guardianStatus = new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Guardian}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Guardian,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = 0, // Expired
            AppliedTurn = 1,
            Stacks = 1,
            MaxStacks = 1
        };

        // 3 stacks of pressure
        prime.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = $"{prime.Id}:{NghiaPhucPrimeSkillCodes.Pressure}",
            EffectTypeCode = NghiaPhucPrimeSkillCodes.Pressure,
            SourceSkillId = NghiaPhucPrimeSkillCodes.Ultimate,
            SourceHeroId = prime.Id,
            RemainingTurns = -1,
            AppliedTurn = 1,
            Stacks = 3,
            MaxStacks = 5
        });

        var reactionHandler = new NghiaPhucPrimeReactionHandler();
        var events = reactionHandler.OnTurnEnd(guardianStatus, prime, round: 2, turn: 2, combatants: [prime, enemy]);

        // Release event emitted
        Assert.Contains(events, e => e.EventType == NghiaPhucPrimeSkillCodes.EventPressureReleased);

        // 3 stacks: Heal = 1000 * 2% * 3 = 60 HP
        var heal = events.First(e => e.EventType == BattleCodes.Heal);
        Assert.Equal(60, heal.Value);

        // 3 stacks: Damage = 100 DEF * 20% * 3 = 60 DMG
        var dmg = events.First(e => e.EventType == BattleCodes.Damage);
        Assert.Equal(60, dmg.Value);

        // Pressure status removed
        Assert.DoesNotContain(prime.StatusEffects, s => s.EffectTypeCode == NghiaPhucPrimeSkillCodes.Pressure);
    }
}
