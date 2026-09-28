using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Reactions;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.ThanhThaiAura;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class ThanhThaiAuraSkillHandlerTests
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
        Id = ThanhThaiAuraSkillCodes.Basic,
        Name = "Quét Cho Có",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            // Effect 1: Physical Damage 70% ATK
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.70m)],
                Parameters = Params(
                    IntParam("AURA_GAIN_BASIC", 10),
                    IntParam("LOSS_OF_CONFIDENCE_MAX_STACKS", 3),
                    IntParam("LOSS_OF_CONFIDENCE_DURATION_TURNS", 6),
                    DecimalParam("LOSS_OF_CONFIDENCE_DAMAGE_TAKEN_PER_STACK", 10m),
                    BoolParam("CAN_CRIT", true),
                    BoolParam("ABSOLUTE_ACCURACY", true),
                    BoolParam("UNDISPELLABLE", true)
                )
            },
            // Effect 2: Magic Damage 60% MagicDamage
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Magic,
                Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 0.60m)],
                Parameters = Params(
                    BoolParam("CAN_CRIT", true)
                )
            }
        ]
    };

    public static BattleSkill CreateUltimateSkill() => new()
    {
        Id = ThanhThaiAuraSkillCodes.Ultimate,
        Name = "Lôi Chổi Xích Hồng",
        SkillTypeCode = BattleCodes.Energy,
        EnergyCost = 100,
        Effects =
        [
            // Effect 1: Physical Damage 90% ATK
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.90m)],
                Parameters = Params(
                    IntParam("FULL_AURA_THRESHOLD", 100),
                    IntParam("FULL_AURA_CONSUMPTION", 100),
                    IntParam("DEFAULT_MAX_TARGETS", 3),
                    IntParam("FULL_AURA_MAX_TARGETS", 4),
                    DecimalParam("CHAIN_RANGE", 1.50m),
                    DecimalParam("CHAIN_DAMAGE_DECAY_PERCENT", 15.00m),
                    IntParam("AURA_GAIN_DEFEAT", 15),
                    IntParam("LOSS_OF_CONFIDENCE_MAX_STACKS", 3),
                    IntParam("LOSS_OF_CONFIDENCE_DURATION_TURNS", 6),
                    DecimalParam("LOSS_OF_CONFIDENCE_DAMAGE_TAKEN_PER_STACK", 10m),
                    DecimalParam("LOSS_OF_CONFIDENCE_SKILL_APPLY_CHANCE", 40m),
                    DecimalParam("DETONATION_PHYSICAL_STACK_1", 0.30m),
                    DecimalParam("DETONATION_MAGIC_STACK_1", 0.20m),
                    DecimalParam("DETONATION_PHYSICAL_STACK_2", 0.60m),
                    DecimalParam("DETONATION_MAGIC_STACK_2", 0.40m),
                    DecimalParam("DETONATION_PHYSICAL_STACK_3", 1.00m),
                    DecimalParam("DETONATION_MAGIC_STACK_3", 0.70m),
                    BoolParam("DETONATION_CAN_CRIT", false),
                    BoolParam("DETONATION_CAN_KILL", true)
                )
            },
            // Effect 2: Magic Damage 70% MagicDamage
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Magic,
                Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 0.70m)],
                Parameters = Params(
                    BoolParam("CAN_CRIT", true)
                )
            }
        ]
    };

    private static BattleSkill CreateDummyBasicSkill(int atkScaling = 100) => new()
    {
        Id = "DUMMY_BASIC",
        Name = "Đánh thường",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", atkScaling / 100m)]
            }
        ]
    };

    private static BattleSkill CreateMultiHitEnemySkill() => new()
    {
        Id = "DUMMY_MULTI_HIT",
        Name = "Liên Hoàn Kích",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.50m)]
            },
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.50m)]
            }
        ]
    };

    private static BattleSkill CreateAoeEnemySkill() => new()
    {
        Id = "DUMMY_AOE",
        Name = "Quét Toàn Sân",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemyAll,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.50m)]
            }
        ]
    };

    private static BattleStatusEffect CreateLocStatus(long sourceHeroId, string sourceSkillId, int stacks = 1, int remainingTurns = 6) => new()
    {
        InstanceId = Guid.NewGuid().ToString(),
        EffectTypeCode = ThanhThaiAuraSkillCodes.StatusLossOfConfidence,
        SourceHeroId = sourceHeroId,
        SourceSkillId = sourceSkillId,
        RemainingTurns = remainingTurns,
        AppliedTurn = 0,
        Stacks = stacks,
        MaxStacks = 3,
        DamageBonusPerStackPercent = 10m,
        IncomingDamageBonusPerStackPercent = 10m,
        IncomingDamageBonusRestrictedToSource = true,
        StatModifiers = []
    };

    private static BattleCombatant CreateThanhThai(int position = 1, int aura = 0, int energy = 0, int hp = 10000, int spd = 120)
    {
        var hero = new BattleCombatant
        {
            Id = 1,
            SourceHeroId = 100,
            Team = 0,
            Position = position,
            Name = "Thanh Thái Aura",
            MaxHp = hp,
            Hp = hp,
            Atk = 1000,
            Def = 200,
            Spd = spd,
            MagicDamage = 800,
            MagicResistance = 150,
            CritChance = 0m,
            CritDamage = 150m,
            Energy = energy,
            MaxEnergy = 100,
            BasicSkill = CreateBasicSkill(),
            EnergySkill = CreateUltimateSkill()
        };
        hero.SetResource(ThanhThaiAuraSkillCodes.ResourceAura, aura);
        if (aura >= 100)
        {
            hero.StatusEffects.Add(new BattleStatusEffect
            {
                InstanceId = Guid.NewGuid().ToString(),
                EffectTypeCode = ThanhThaiAuraSkillCodes.StatusFullAura,
                SourceHeroId = hero.Id,
                SourceSkillId = hero.BasicSkill.Id,
                RemainingTurns = -1,
                AppliedTurn = 0,
                Stacks = 1,
                MaxStacks = 1,
                StatModifiers = []
            });
        }
        return hero;
    }

    private static BattleCombatant CreateEnemy(long id, int position = 1, int hp = 5000, int def = 100, int spd = 50, int mr = 50) => new()
    {
        Id = id,
        SourceHeroId = 200 + id,
        Team = 1,
        Position = position,
        Name = $"Enemy_{id}",
        MaxHp = hp,
        Hp = hp,
        Atk = 500,
        Def = def,
        Spd = spd,
        MagicDamage = 0,
        MagicResistance = mr,
        CritChance = 0m,
        CritDamage = 150m,
        Energy = 0,
        MaxEnergy = 100,
        BasicSkill = CreateDummyBasicSkill()
    };

    // ==========================================
    // BÁ KHÍ TESTS (Cases 1 - 8)
    // ==========================================

    [Fact]
    public void Case01_BasicAttack_Gains10Aura()
    {
        var tt = CreateThanhThai(aura: 0);
        var enemy = CreateEnemy(2, hp: 10000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var auraGain = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.AuraGained && e.ActorId == tt.Id);
        Assert.NotNull(auraGain);
        Assert.Equal(10, auraGain.Value);
        Assert.Equal("BASIC_ATTACK", auraGain.ReasonCode);
    }

    [Fact]
    public void Case02_BasicAttack_EvenIfDodged_StillGains10Aura()
    {
        // When not full aura, if basic attack was executed, +10 aura is granted
        var tt = CreateThanhThai(aura: 0);
        var enemy = CreateEnemy(2, hp: 10000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var auraEvents = result.Events.Where(e => e.EventType == BattleCodes.AuraGained && e.ActorId == tt.Id).ToList();
        Assert.NotEmpty(auraEvents);
        Assert.Equal(10, auraEvents.First().Value);
    }

    [Fact]
    public void Case03_MultiHitEnemyAction_Gains5AuraOnlyOnce()
    {
        var tt = CreateThanhThai(spd: 50, aura: 0); // slower
        var enemy = CreateEnemy(2, spd: 150, hp: 10000); // faster
        enemy = new BattleCombatant
        {
            Id = enemy.Id, SourceHeroId = enemy.SourceHeroId, Team = enemy.Team, Position = enemy.Position,
            Name = enemy.Name, MaxHp = enemy.MaxHp, Hp = enemy.Hp, Atk = enemy.Atk, Def = enemy.Def,
            Spd = enemy.Spd, MagicDamage = enemy.MagicDamage, MagicResistance = enemy.MagicResistance,
            CritChance = enemy.CritChance, CritDamage = enemy.CritDamage, Energy = enemy.Energy,
            MaxEnergy = enemy.MaxEnergy,
            BasicSkill = CreateMultiHitEnemySkill()
        };

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        // Targeted reaction should only grant 5 aura once for the enemy action
        var targetedGains = result.Events
            .Where(e => e.EventType == BattleCodes.AuraGained && e.ReasonCode == "TARGETED_BY_ENEMY")
            .ToList();
        Assert.Single(targetedGains);
        Assert.Equal(5, targetedGains[0].Value);
    }

    [Fact]
    public void Case04_AoeEnemyAction_TargetingThanhThai_Gains5AuraOnlyOnce()
    {
        var tt = CreateThanhThai(position: 1, spd: 50, aura: 0);
        var ally = CreateEnemy(10, position: 3, spd: 50, hp: 10000);
        ally.Position = 3;
        // ally is on same team as tt
        var allyHero = new BattleCombatant
        {
            Id = 10, SourceHeroId = 210, Team = 0, Position = 3, Name = "Ally",
            MaxHp = 10000, Hp = 10000, Atk = 100, Def = 100, Spd = 50, MagicDamage = 0, MagicResistance = 50,
            CritChance = 0m, CritDamage = 150m, Energy = 0, MaxEnergy = 100, BasicSkill = CreateDummyBasicSkill()
        };

        var enemy = new BattleCombatant
        {
            Id = 2, SourceHeroId = 202, Team = 1, Position = 1, Name = "Enemy_Aoe",
            MaxHp = 10000, Hp = 10000, Atk = 100, Def = 100, Spd = 150, MagicDamage = 0, MagicResistance = 50,
            CritChance = 0m, CritDamage = 150m, Energy = 0, MaxEnergy = 100, BasicSkill = CreateAoeEnemySkill()
        };

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, allyHero, enemy],
            MaxRounds = 1
        });

        var targetedGains = result.Events
            .Where(e => e.EventType == BattleCodes.AuraGained && e.ActorId == tt.Id && e.ReasonCode == "TARGETED_BY_ENEMY")
            .ToList();
        Assert.Single(targetedGains);
        Assert.Equal(5, targetedGains[0].Value);
    }

    [Fact]
    public void Case05_CombatantDefeated_Gains15AuraOnce()
    {
        // Thanh Thai attacks low HP enemy and defeats him
        var tt = CreateThanhThai(aura: 0);
        var enemy = CreateEnemy(2, hp: 50); // Will die on hit

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var deathGains = result.Events
            .Where(e => e.EventType == BattleCodes.AuraGained && e.ReasonCode == "COMBATANT_DEFEATED")
            .ToList();
        Assert.Single(deathGains);
        Assert.Equal(15, deathGains[0].Value);
    }

    [Fact]
    public void Case06_AuraCannotExceed100()
    {
        var tt = CreateThanhThai(aura: 95);
        var enemy = CreateEnemy(2, hp: 10000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var finalResourceEvt = result.Events
            .LastOrDefault(e => e.EventType == BattleCodes.ResourceChanged && e.ActorId == tt.Id);
        Assert.NotNull(finalResourceEvt);
        Assert.Equal(100, finalResourceEvt.CurrentValue);
    }

    [Fact]
    public void Case07_DamageBonus_ReachesMax25Percent_At100Aura()
    {
        // 0 Aura damage vs 100 Aura damage
        var tt0 = CreateThanhThai(aura: 0);
        var enemy0 = CreateEnemy(2, hp: 50000, def: 0);
        var res0 = _engine.Simulate(new BattleSimulationRequest { Combatants = [tt0, enemy0], MaxRounds = 1 });
        var dmg0 = res0.Events.Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt0.Id).Sum(e => e.Value);

        var tt100 = CreateThanhThai(aura: 100);
        var enemy100 = CreateEnemy(2, hp: 50000, def: 0);
        var res100 = _engine.Simulate(new BattleSimulationRequest { Combatants = [tt100, enemy100], MaxRounds = 1 });
        var dmg100 = res100.Events.Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt100.Id).Sum(e => e.Value);

        // At 100 aura, damage multiplier is (1 + 100 * 0.0025) = 1.25 (+25%)
        Assert.True(dmg100 > dmg0);
        var ratio = (decimal)dmg100 / dmg0;
        Assert.InRange(ratio, 1.24m, 1.26m);
    }

    [Fact]
    public void Case08_HybridDamage_DoesNotApplyAuraBonusTwice()
    {
        // Both physical and magic damage are each multiplied once by 1 + (Aura * 0.0025)
        var tt = CreateThanhThai(aura: 50); // +12.5%
        var enemy = CreateEnemy(2, hp: 50000, def: 0, mr: 0);
        var res = _engine.Simulate(new BattleSimulationRequest { Combatants = [tt, enemy], MaxRounds = 1 });
        var hits = res.Events.Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id).ToList();
        Assert.Equal(2, hits.Count); // 1 physical, 1 magic

        // Base physical: 1000 * 0.7 = 700. With 50 aura: 700 * 1.125 = 787
        // Base magic: 800 * 0.6 = 480. With 50 aura: 480 * 1.125 = 540
        var physHit = hits[0].Value;
        var magicHit = hits[1].Value;
        Assert.InRange(physHit, 780, 795);
        Assert.InRange(magicHit, 535, 545);
    }

    // ==========================================
    // AURA TIER TESTS (Cases 9 - 15)
    // ==========================================

    [Theory]
    [InlineData(0, 0)]
    [InlineData(24, 0)]
    [InlineData(25, 1)]
    [InlineData(49, 1)]
    [InlineData(50, 2)]
    [InlineData(74, 2)]
    [InlineData(75, 3)]
    [InlineData(99, 3)]
    [InlineData(100, 4)]
    public void Case09_to_13_AuraTiers_MatchSpecification(int aura, int expectedTier)
    {
        var tier = ThanhThaiAuraResourceHandler.GetAuraTier(aura);
        Assert.Equal(expectedTier, tier);
    }

    [Fact]
    public void Case14_Reaching100_EmitsFullAuraActivatedOnlyOnce()
    {
        var tt = CreateThanhThai(aura: 90);
        var enemy = CreateEnemy(2, hp: 20000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 2
        });

        var activatedEvents = result.Events
            .Where(e => e.EventType == BattleCodes.FullAuraActivated && e.ActorId == tt.Id)
            .ToList();
        Assert.Single(activatedEvents);
    }

    [Fact]
    public void Case15_DroppingBelow100_RemovesFullAuraFarming()
    {
        // When ultimate is cast at 100 Aura, 75 is consumed leaving 25 (if 0 kills), Full Aura is removed
        var tt = CreateThanhThai(aura: 100, energy: 100);
        var enemy = CreateEnemy(2, hp: 50000);
        enemy.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1));

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var removedEvt = result.Events
            .FirstOrDefault(e => e.EventType == BattleCodes.FullAuraRemoved && e.ActorId == tt.Id);
        Assert.NotNull(removedEvt);
    }

    // ==========================================
    // LOSS OF CONFIDENCE TESTS (Cases 16 - 24)
    // ==========================================

    [Fact]
    public void Case16_LossOfConfidence_OnlyAppliedWhenFullAuraBasicAttackHits()
    {
        // 0 Aura: no status applied
        var tt0 = CreateThanhThai(aura: 0);
        var enemy0 = CreateEnemy(2, hp: 20000);
        var res0 = _engine.Simulate(new BattleSimulationRequest { Combatants = [tt0, enemy0], MaxRounds = 1 });
        Assert.DoesNotContain(res0.Events, e => e.EffectTypeCode == ThanhThaiAuraSkillCodes.StatusLossOfConfidence);

        // 100 Aura: status applied
        var tt100 = CreateThanhThai(aura: 100);
        var enemy100 = CreateEnemy(2, hp: 20000);
        var res100 = _engine.Simulate(new BattleSimulationRequest { Combatants = [tt100, enemy100], MaxRounds = 1 });
        Assert.Contains(res100.Events, e => e.EffectTypeCode == ThanhThaiAuraSkillCodes.StatusLossOfConfidence && e.EventType == "STATUS_APPLIED");
    }

    [Fact]
    public void Case17_LossOfConfidence_Max3Stacks()
    {
        var tt = CreateThanhThai(aura: 100);
        var enemy = CreateEnemy(2, hp: 100000);

        // Manually set 3 stacks on enemy
        enemy.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 3));

        // Thanh Thai hits again in Full Aura
        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var locStatus = result.FinalCombatants.First(c => c.Id == enemy.Id).StatusEffects.FirstOrDefault(s => s.EffectTypeCode == ThanhThaiAuraSkillCodes.StatusLossOfConfidence);
        Assert.NotNull(locStatus);
        Assert.Equal(3, locStatus.Stacks);
    }

    [Fact]
    public void Case18_LossOfConfidence_ReapplicationRefreshesDuration()
    {
        var tt = CreateThanhThai(aura: 100);
        var enemy = CreateEnemy(2, hp: 100000);

        enemy.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1, remainingTurns: 2));

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var refreshEvt = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.StatusRefreshed);
        Assert.NotNull(refreshEvt);
        Assert.Equal(6, refreshEvt.RemainingTurns);
    }

    [Fact]
    public void Case19_LossOfConfidence_OnlyIncreasesDamageFromSourceHero()
    {
        var tt = CreateThanhThai(aura: 0);
        var ally = new BattleCombatant
        {
            Id = 5, SourceHeroId = 205, Team = 0, Position = 3, Name = "Ally",
            MaxHp = 10000, Hp = 10000, Atk = 1000, Def = 100, Spd = 150, MagicDamage = 0, MagicResistance = 50,
            CritChance = 0m, CritDamage = 150m, Energy = 0, MaxEnergy = 100, BasicSkill = CreateDummyBasicSkill()
        };
        var enemy = CreateEnemy(2, hp: 100000, def: 0);

        // Apply 3 stacks of LOC sourced from tt
        enemy.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 3));

        // Ally attacks first (spd 150 vs tt 120)
        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, ally, enemy],
            MaxRounds = 1
        });

        var allyDmg = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.Damage && e.ActorId == ally.Id)?.Value ?? 0;
        // Ally ATK 1000 with 100% scaling, 0 def -> exactly 1000 (no 30% bonus from tt's LOC)
        Assert.Equal(1000, allyDmg);
    }

    [Fact]
    public void Case20_LossOfConfidence_UndispellableByNormalCleanse()
    {
        // Dispel parameter is configured as UNDISPELLABLE = true in skill effect
        var primaryEffect = CreateBasicSkill().Effects[0];
        Assert.True(primaryEffect.GetBool("UNDISPELLABLE", false));
    }

    [Fact]
    public void Case21_Ultimate_Below75Aura_DoesNotDetonate()
    {
        var tt = CreateThanhThai(aura: 50, energy: 100);
        var enemy = CreateEnemy(2, hp: 50000);

        enemy.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 2));

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        Assert.DoesNotContain(result.Events, e => e.EventType == BattleCodes.LossOfConfidenceDetonated);
    }

    [Fact]
    public void Case22_Ultimate_DetonatesOnlyWhenLossOfConfidenceReachesMaxStacks()
    {
        var tt = CreateThanhThai(aura: 75, energy: 100);
        var enemy = CreateEnemy(2, hp: 50000);

        enemy.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 3));

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var detEvt = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.LossOfConfidenceDetonated);
        Assert.NotNull(detEvt);
        Assert.Equal(3, detEvt.PreviousStacks);
        Assert.Equal(0, detEvt.CurrentStacks);
        Assert.DoesNotContain(result.Events, e => e.EventType == BattleCodes.AuraConsumed);
    }

    [Fact]
    public void Case23_Detonation_CannotCrit()
    {
        var primaryEffect = CreateUltimateSkill().Effects[0];
        Assert.False(primaryEffect.GetBool("DETONATION_CAN_CRIT", true));
    }

    [Fact]
    public void Case24_Detonation_CanKillTarget()
    {
        var tt = CreateThanhThai(aura: 80, energy: 100);
        // Enemy has high HP so primary hit won't kill, but detonation will
        var enemy = CreateEnemy(2, hp: 1800, def: 0);

        enemy.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 3));

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, enemy],
            MaxRounds = 1
        });

        var deathEvt = result.Events.FirstOrDefault(e => e.EventType == "DEATH" && e.TargetId == enemy.Id);
        Assert.NotNull(deathEvt);
    }

    // ==========================================
    // ULTIMATE TESTS (Cases 25 - 35)
    // ==========================================

    [Fact]
    public void Case25_Ultimate_BelowFullAura_ChainsMax3Targets()
    {
        var tt = CreateThanhThai(position: 1, aura: 50, energy: 100);
        // Place 4 enemies close together in formation
        var e1 = CreateEnemy(2, position: 1, hp: 50000);
        var e2 = CreateEnemy(3, position: 3, hp: 50000);
        var e3 = CreateEnemy(4, position: 5, hp: 50000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1, e2, e3],
            MaxRounds = 1
        });

        var hitTargets = result.Events
            .Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id)
            .Select(e => e.TargetId)
            .Distinct()
            .ToList();
        Assert.Equal(3, hitTargets.Count);
    }

    [Fact]
    public void Case26_Ultimate_75to99Aura_ChainsMax3Targets_WithoutConsumingAura()
    {
        var tt = CreateThanhThai(position: 1, aura: 80, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 50000);
        var e2 = CreateEnemy(3, position: 3, hp: 50000);
        var e3 = CreateEnemy(4, position: 5, hp: 50000);
        var e4 = CreateEnemy(5, position: 4, hp: 50000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1, e2, e3, e4],
            MaxRounds = 1
        });

        var hitTargets = result.Events
            .Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id)
            .Select(e => e.TargetId)
            .Distinct()
            .ToList();
        Assert.Equal(3, hitTargets.Count);

        Assert.DoesNotContain(result.Events, e => e.EventType == BattleCodes.AuraConsumed);
    }

    [Fact]
    public void Case27_Ultimate_100Aura_ChainsMax4Targets_AndConsumesFullAura()
    {
        var tt = CreateThanhThai(position: 1, aura: 100, energy: 100);
        // Provide 1 enemy with LOC so AI uses ultimate
        var e1 = CreateEnemy(2, position: 1, hp: 50000);
        e1.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1));
        var e2 = CreateEnemy(3, position: 3, hp: 50000);
        var e3 = CreateEnemy(4, position: 5, hp: 50000);
        var e4 = CreateEnemy(5, position: 4, hp: 50000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1, e2, e3, e4],
            MaxRounds = 1
        });

        var hitTargets = result.Events
            .Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id)
            .Select(e => e.TargetId)
            .Distinct()
            .ToList();
        Assert.Equal(4, hitTargets.Count);
        Assert.Contains(result.Events, e => e.EventType == BattleCodes.AuraConsumed && e.Value == 100);
    }

    [Fact]
    public void Case28_Ultimate_DoesNotChainBackToAlreadyHitTargets()
    {
        var tt = CreateThanhThai(position: 1, aura: 100, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 50000);
        e1.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1));
        var e2 = CreateEnemy(3, position: 3, hp: 50000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1, e2],
            MaxRounds = 1
        });

        var damageEvents = result.Events.Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id && e.ExecutionGroup != "DETONATE").ToList();
        // Since each target has physical + magic (2 hits per target), 2 targets total = 4 damage events
        Assert.Equal(4, damageEvents.Count);
        Assert.Equal(2, damageEvents.Select(e => e.TargetId).Distinct().Count());
    }

    [Fact]
    public void Case29_Ultimate_DoesNotChainToTargetsBeyondDistanceThreshold()
    {
        var tt = CreateThanhThai(position: 1, aura: 100, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 50000); // Grid: (1, 0)
        e1.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1));
        // Position 5 is at Grid (1, 2) -> distance is 2.0 > CHAIN_RANGE 1.50
        var e2 = CreateEnemy(3, position: 5, hp: 50000);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1, e2],
            MaxRounds = 1
        });

        var hitTargets = result.Events
            .Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id)
            .Select(e => e.TargetId)
            .Distinct()
            .ToList();
        // Should only hit e1, since e2 is too far
        Assert.Single(hitTargets);
        Assert.Equal(e1.Id, hitTargets[0]);
    }

    [Fact]
    public void Case30_Ultimate_PrioritizesTargetWithMoreLossOfConfidence()
    {
        var tt = CreateThanhThai(position: 1, aura: 50, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 50000);
        var e2 = CreateEnemy(3, position: 3, hp: 50000);

        // Give e2 more LOC stacks
        e2.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 2));

        var targets = ThanhThaiFormationGrid.SelectChainTargets([e1, e2], maxTargets: 1, maxDistance: 1.5);
        Assert.Single(targets);
        Assert.Equal(e2.Id, targets[0].Id);
    }

    [Fact]
    public void Case31_DamageDecays15PercentPerConsecutiveChain()
    {
        var tt = CreateThanhThai(position: 1, aura: 80, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 50000, def: 0);
        var e2 = CreateEnemy(3, position: 3, hp: 50000, def: 0);
        var e3 = CreateEnemy(4, position: 5, hp: 50000, def: 0);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1, e2, e3],
            MaxRounds = 1
        });

        // Physical damage hits on e2 and e3:
        // Target 1: 100% coefficient
        // Target 2: 85% coefficient
        // Target 3: 70% coefficient
        var physHits = result.Events
            .Where(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id && e.DamageSchoolCode == BattleCodes.Physical && e.ExecutionGroup != "DETONATE")
            .ToList();

        Assert.Equal(3, physHits.Count);
        var d1 = (decimal)physHits[0].Value;
        var d2 = (decimal)physHits[1].Value;
        var d3 = (decimal)physHits[2].Value;

        Assert.InRange(d2 / d1, 0.83m, 0.87m); // ~85%
        Assert.InRange(d3 / d1, 0.68m, 0.72m); // ~70%
    }

    [Fact]
    public void Case32_AuraDamageBonus_IsSnapshotBeforeConsumption()
    {
        // With one reachable target, all three decayed chain allocations are consolidated on it.
        var tt = CreateThanhThai(position: 1, aura: 80, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 50000, def: 0);
        e1.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1));

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1],
            MaxRounds = 1
        });

        var physHit = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.Damage && e.ActorId == tt.Id && e.DamageSchoolCode == BattleCodes.Physical);
        Assert.NotNull(physHit);
        // 1188 per base hit * (100% + 85% + 70%) = ~3029.
        Assert.InRange(physHit.Value, 3000, 3060);
    }

    [Fact]
    public void Case33_KillRestores15AuraPerTarget()
    {
        var tt = CreateThanhThai(position: 1, aura: 80, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 10); // Dies on hit
        e1.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1));

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1],
            MaxRounds = 1
        });

        // Below Full Aura there is no consumption: 80 + 15 = 95.
        var finalAura = result.Events
            .LastOrDefault(e => e.EventType == BattleCodes.ResourceChanged && e.ActorId == tt.Id);
        Assert.NotNull(finalAura);
        Assert.Equal(95, finalAura.CurrentValue);
    }

    [Fact]
    public void Case34_From100Aura_Killing4Targets_EndsAt85Aura()
    {
        var tt = CreateThanhThai(position: 1, aura: 100, energy: 100);
        var e1 = CreateEnemy(2, position: 1, hp: 10);
        e1.StatusEffects.Add(CreateLocStatus(tt.Id, tt.BasicSkill.Id, stacks: 1));
        var e2 = CreateEnemy(3, position: 3, hp: 10);
        var e3 = CreateEnemy(4, position: 5, hp: 10);
        var e4 = CreateEnemy(5, position: 4, hp: 10);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            Combatants = [tt, e1, e2, e3, e4],
            MaxRounds = 1
        });

        // Full Aura is fully consumed: 100 - 100 + 4 * 15 = 60.
        var finalAura = result.Events
            .LastOrDefault(e => e.EventType == BattleCodes.ResourceChanged && e.ActorId == tt.Id);
        Assert.NotNull(finalAura);
        Assert.Equal(60, finalAura.CurrentValue);
    }

    [Fact]
    public void Case35_Replay_SameRandomSeed_ProducesIdenticalResults()
    {
        var request1 = new BattleSimulationRequest
        {
            RandomSeed = 12345,
            Combatants = [CreateThanhThai(aura: 60), CreateEnemy(2, hp: 10000)],
            MaxRounds = 5
        };
        var request2 = new BattleSimulationRequest
        {
            RandomSeed = 12345,
            Combatants = [CreateThanhThai(aura: 60), CreateEnemy(2, hp: 10000)],
            MaxRounds = 5
        };

        var res1 = _engine.Simulate(request1);
        var res2 = _engine.Simulate(request2);

        Assert.Equal(res1.Events.Count, res2.Events.Count);
        for (int i = 0; i < res1.Events.Count; i++)
        {
            Assert.Equal(res1.Events[i].EventType, res2.Events[i].EventType);
            Assert.Equal(res1.Events[i].Value, res2.Events[i].Value);
            Assert.Equal(res1.Events[i].ActorId, res2.Events[i].ActorId);
            Assert.Equal(res1.Events[i].TargetId, res2.Events[i].TargetId);
        }
    }
}
