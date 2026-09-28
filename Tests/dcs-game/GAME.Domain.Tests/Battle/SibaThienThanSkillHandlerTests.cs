using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.SibaThienThan;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class SibaThienThanSkillHandlerTests
{
    private static BattleSkillEffectParameter DecimalParam(string code, decimal val) =>
        new(code, val, (int)val, null, null);
    private static BattleSkillEffectParameter IntParam(string code, int val) =>
        new(code, val, val, null, null);
    private static BattleSkillEffectParameter BoolParam(string code, bool val) =>
        new(code, null, null, val, null);
    private static BattleSkillEffectParameter StringParam(string code, string val) =>
        new(code, null, null, null, val);

    private static Dictionary<string, BattleSkillEffectParameter> Params(params BattleSkillEffectParameter[] list) =>
        list.ToDictionary(x => x.ParameterCode, StringComparer.OrdinalIgnoreCase);

    public static BattleSkill CreateBasicSkill() => new()
    {
        Id = SibaThienThanSkillCodes.Basic,
        Name = "Cánh Nhẹ Cổ Vũ",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            // Effect 1: Heal 140% Magic Damage, AllyLowestHpPreferWithoutStatus
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Heal,
                TargetTypeCode = SibaThienThanSkillCodes.TargetAllyLowestHpPreferWithoutStatus,
                Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 1.40m)],
                Parameters = Params(
                    BoolParam(SibaThienThanSkillCodes.ParamCanCrit, false),
                    StringParam(SibaThienThanSkillCodes.ParamPreferredMissingStatusGroup, SibaThienThanSkillCodes.StatusGroupEncouragement),
                    IntParam(SibaThienThanSkillCodes.ParamTargetCount, 1)
                )
            },
            // Effect 2: Energy Change +10
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                EffectTypeCode = BattleCodes.EnergyChange,
                TargetTypeCode = SibaThienThanSkillCodes.TargetAllyLowestHpPreferWithoutStatus,
                BaseValue = 10,
                Parameters = Params(
                    IntParam(SibaThienThanSkillCodes.ParamEnergyDelta, 10)
                )
            },
            // Effect 3: Encouragement (50% Chance, 2 turns, 20% buff)
            new BattleSkillEffect
            {
                DisplayOrder = 3,
                EffectTypeCode = BattleCodes.StatBuff,
                TargetTypeCode = SibaThienThanSkillCodes.TargetAllyLowestHpPreferWithoutStatus,
                BaseValue = 20,
                DurationTurns = 2,
                ChancePercent = 50.00m,
                Parameters = Params(
                    StringParam(SibaThienThanSkillCodes.ParamStatusGroup, SibaThienThanSkillCodes.StatusGroupEncouragement),
                    IntParam(SibaThienThanSkillCodes.ParamBuffPercent, 20),
                    IntParam(SibaThienThanSkillCodes.ParamDurationTurns, 2)
                )
            },
            // Effect 4: Resource Ân Phúc (+1 stack, max 5)
            new BattleSkillEffect
            {
                DisplayOrder = 4,
                EffectTypeCode = BattleCodes.StatBuff,
                TargetTypeCode = BattleCodes.Self,
                BaseValue = 1,
                Parameters = Params(
                    StringParam("RESOURCE_CODE", SibaThienThanSkillCodes.ResourceBlessing),
                    IntParam(SibaThienThanSkillCodes.ParamMaxBlessingStacks, 5),
                    IntParam(SibaThienThanSkillCodes.ParamBlessingGainPerBasic, 1)
                )
            }
        ]
    };

    public static BattleSkill CreateUltimateSkill() => new()
    {
        Id = SibaThienThanSkillCodes.Ultimate,
        Name = "Thiên Hộ Giáng Thế",
        SkillTypeCode = BattleCodes.Energy,
        EnergyCost = 100,
        Effects =
        [
            // Effect 1: CELESTIAL_PROTECTION (+20% SPD, +20% RESISTANCE, 2 turns)
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.CelestialProtection,
                TargetTypeCode = BattleCodes.AllyAll,
                BaseValue = 20,
                DurationTurns = 2,
                StatModifiers =
                [
                    new BattleStatModifier("SPD", "PERCENT", 20m),
                    new BattleStatModifier("RESISTANCE", "PERCENT", 20m)
                ],
                Parameters = Params(
                    StringParam("STATUS_CODE", SibaThienThanSkillCodes.StatusCelestialProtection),
                    IntParam(SibaThienThanSkillCodes.ParamDurationTurns, 2)
                )
            },
            // Effect 2: DISPEL_DEBUFF (Up to 1 random debuff)
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                EffectTypeCode = BattleCodes.DispelDebuff,
                TargetTypeCode = BattleCodes.AllyAll,
                BaseValue = 1,
                Parameters = Params(
                    IntParam(SibaThienThanSkillCodes.ParamDispelCount, 1),
                    StringParam(SibaThienThanSkillCodes.ParamSelectionMode, "RANDOM"),
                    BoolParam(SibaThienThanSkillCodes.ParamDispellableOnly, true)
                )
            },
            // Effect 3: Threshold 5 stacks
            new BattleSkillEffect
            {
                DisplayOrder = 3,
                EffectTypeCode = BattleCodes.StatBuff,
                TargetTypeCode = BattleCodes.Self,
                BaseValue = 5,
                Parameters = Params(
                    StringParam("RESOURCE_CODE", SibaThienThanSkillCodes.ResourceBlessing),
                    IntParam(SibaThienThanSkillCodes.ParamBlessingCostForEmpowered, 5)
                )
            },
            // Effect 4 (EMPOWERED): Heal 110% Magic Damage
            new BattleSkillEffect
            {
                DisplayOrder = 4,
                ExecutionGroup = "EMPOWERED",
                EffectTypeCode = BattleCodes.Heal,
                TargetTypeCode = BattleCodes.AllyAll,
                Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 1.10m)],
                Parameters = Params(
                    BoolParam(SibaThienThanSkillCodes.ParamCanCrit, false)
                )
            },
            // Effect 5 (EMPOWERED): Refresh Encouragement to 2 turns & grant +15 Energy
            new BattleSkillEffect
            {
                DisplayOrder = 5,
                ExecutionGroup = "EMPOWERED",
                EffectTypeCode = BattleCodes.EnergyChange,
                TargetTypeCode = BattleCodes.AllyAll,
                BaseValue = 15,
                Parameters = Params(
                    StringParam(SibaThienThanSkillCodes.ParamStatusGroup, SibaThienThanSkillCodes.StatusGroupEncouragement),
                    IntParam(SibaThienThanSkillCodes.ParamRefreshDuration, 2),
                    IntParam(SibaThienThanSkillCodes.ParamEmpoweredEnergyGain, 15)
                )
            }
        ]
    };

    public static BattleCombatant CreateSiba(long id = 1, int team = 0, int position = 4, int magicDmg = 200, int hp = 1000, int maxHp = 1000, int energy = 0) => new()
    {
        Id = id,
        SourceHeroId = id,
        Team = team,
        Position = position,
        Name = "Siba Thiên Thần",
        Hp = hp,
        MaxHp = maxHp,
        Atk = 45,
        Def = 75,
        Spd = 118,
        MagicDamage = magicDmg,
        MagicResistance = 150,
        Energy = energy,
        MaxEnergy = 100,
        BasicSkill = CreateBasicSkill(),
        EnergySkill = CreateUltimateSkill()
    };

    public static BattleCombatant CreateAlly(long id, int position, int hp = 500, int maxHp = 1000, int energy = 0) => new()
    {
        Id = id,
        SourceHeroId = id,
        Team = 0,
        Position = position,
        Name = $"Ally {id}",
        Hp = hp,
        MaxHp = maxHp,
        Atk = 100,
        Def = 100,
        Spd = 100,
        MagicDamage = 100,
        MagicResistance = 100,
        Energy = energy,
        MaxEnergy = 100,
        BasicSkill = CreateBasicSkill()
    };

    private static SibaThienThanSkillHandler CreateHandler()
    {
        var effectRegistry = BattleEffectHandlerRegistry.CreateDefault();
        var targetRegistry = BattleTargetSelectorRegistry.CreateDefault();
        var defaultHandler = new DefaultSkillHandler(effectRegistry, targetRegistry);
        return SibaThienThanSkillHandler.Create(defaultHandler, effectRegistry, targetRegistry);
    }

    [Fact]
    public void TargetSelector_PrefersAllyWithoutEncouragement()
    {
        var handler = CreateHandler();
        var siba = CreateSiba();
        // Ally 2 has lower % HP (40%) but already has Encouragement
        var allyWithBuff = CreateAlly(2, position: 2, hp: 400, maxHp: 1000);
        allyWithBuff.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "enc1",
            EffectTypeCode = SibaThienThanSkillCodes.StatusOffense,
            StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
            SourceSkillId = "test",
            RemainingTurns = 2
        });

        // Ally 3 has higher % HP (60%) but lacks Encouragement
        var allyWithoutBuff = CreateAlly(3, position: 1, hp: 600, maxHp: 1000);

        var context = new SkillExecutionContext
        {
            Skill = CreateBasicSkill(),
            Actor = siba,
            Combatants = [siba, allyWithBuff, allyWithoutBuff],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        var result = handler.Execute(context);

        // Must target allyWithoutBuff (id 3) because it prefers without Encouragement
        Assert.Single(result.TargetedCombatantIds);
        Assert.Equal(3, result.TargetedCombatantIds[0]);
    }

    [Fact]
    public void TargetSelector_SelectsLowestHpPercentWhenAllHaveEncouragement()
    {
        var handler = CreateHandler();
        var siba = CreateSiba();
        siba.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "enc_siba",
            EffectTypeCode = SibaThienThanSkillCodes.StatusOffense,
            StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
            SourceSkillId = "test",
            RemainingTurns = 2
        });

        var allyA = CreateAlly(2, position: 2, hp: 800, maxHp: 1000); // 80%
        allyA.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "enc_a",
            EffectTypeCode = SibaThienThanSkillCodes.StatusOffense,
            StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
            SourceSkillId = "test",
            RemainingTurns = 2
        });

        var allyB = CreateAlly(3, position: 1, hp: 300, maxHp: 1000); // 30%
        allyB.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "enc_b",
            EffectTypeCode = SibaThienThanSkillCodes.StatusDefense,
            StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
            SourceSkillId = "test",
            RemainingTurns = 2
        });

        var context = new SkillExecutionContext
        {
            Skill = CreateBasicSkill(),
            Actor = siba,
            Combatants = [siba, allyA, allyB],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        var result = handler.Execute(context);

        // When all have Encouragement, target with lowest % HP (Ally B, 30%) must be chosen
        Assert.Single(result.TargetedCombatantIds);
        Assert.Equal(3, result.TargetedCombatantIds[0]);
    }

    [Fact]
    public void BasicSkill_Heals140PercentMagicDamage_AndGrants10Energy()
    {
        var handler = CreateHandler();
        var siba = CreateSiba(magicDmg: 200); // 140% of 200 = 280 heal
        var ally = CreateAlly(2, position: 1, hp: 500, maxHp: 1000, energy: 20);

        var context = new SkillExecutionContext
        {
            Skill = CreateBasicSkill(),
            Actor = siba,
            Combatants = [siba, ally],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        var result = handler.Execute(context);

        // Heal: 500 + 280 = 780
        Assert.Equal(780, ally.Hp);
        var healEvt = result.Events.FirstOrDefault(e => e.EventType == "HEAL");
        Assert.NotNull(healEvt);
        Assert.Equal(280, healEvt.Value);
        Assert.Equal(480, healEvt.TimelineOffsetMs);

        // Energy: 20 + 10 = 30
        Assert.Equal(30, ally.Energy);
        var energyEvt = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.EnergyChanged);
        Assert.NotNull(energyEvt);
        Assert.Equal(10, energyEvt.Value);
        Assert.Equal(620, energyEvt.TimelineOffsetMs);

        // Siba gains 1 stack of Ân Phúc
        Assert.Equal(1, siba.GetResource(SibaThienThanSkillCodes.ResourceBlessing, 0));
        var resEvt = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.ResourceChanged);
        Assert.NotNull(resEvt);
        Assert.Equal(1, resEvt.Value);
        Assert.Equal(900, resEvt.TimelineOffsetMs);
    }

    [Fact]
    public void BasicSkill_AppliesCorrectEncouragementVariant_BasedOnRow()
    {
        var handler = CreateHandler();
        var siba = CreateSiba();

        // 1. Back row ally (position 2 or 4) -> should get ENCOURAGEMENT_OFFENSE
        var backRowAlly = CreateAlly(2, position: 4, hp: 500, maxHp: 1000);
        // Use Mock random where Next(1, 101) returns 1 (<= 50, guaranteed proc)
        var contextBack = new SkillExecutionContext
        {
            Skill = CreateBasicSkill(),
            Actor = siba,
            Combatants = [siba, backRowAlly],
            Random = new DeterministicRandom(1),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        handler.Execute(contextBack);
        Assert.Contains(backRowAlly.StatusEffects, s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusOffense);
        Assert.DoesNotContain(backRowAlly.StatusEffects, s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusDefense);

        // 2. Front row ally (position 1, 3, 5) -> should get ENCOURAGEMENT_DEFENSE
        var frontRowAlly = CreateAlly(3, position: 3, hp: 500, maxHp: 1000);
        var contextFront = new SkillExecutionContext
        {
            Skill = CreateBasicSkill(),
            Actor = siba,
            Combatants = [siba, frontRowAlly],
            Random = new DeterministicRandom(1),
            Round = 1,
            Turn = 1,
            ActionId = "act2"
        };

        handler.Execute(contextFront);
        Assert.Contains(frontRowAlly.StatusEffects, s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusDefense);
        Assert.DoesNotContain(frontRowAlly.StatusEffects, s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusOffense);
    }

    [Fact]
    public void BasicSkill_ReplacesOldVariant_WhenTargetChangesRow()
    {
        var handler = CreateHandler();
        var siba = CreateSiba();
        siba.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "enc_siba",
            EffectTypeCode = SibaThienThanSkillCodes.StatusOffense,
            StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
            SourceSkillId = "test",
            RemainingTurns = 2
        });
        var ally = CreateAlly(2, position: 2, hp: 500, maxHp: 1000); // Back row initially

        // Give old OFFENSE variant with 1 turn remaining
        ally.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "off1",
            EffectTypeCode = SibaThienThanSkillCodes.StatusOffense,
            StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
            SourceSkillId = "test",
            RemainingTurns = 1
        });

        // Target moved to front row (position 1)
        ally.Position = 1;

        var context = new SkillExecutionContext
        {
            Skill = CreateBasicSkill(),
            Actor = siba,
            Combatants = [siba, ally],
            Random = new DeterministicRandom(1),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        handler.Execute(context);

        // Must now only have DEFENSE variant with 2 turns remaining, and no OFFENSE variant
        Assert.DoesNotContain(ally.StatusEffects, s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusOffense);
        var defStatus = ally.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusDefense);
        Assert.NotNull(defStatus);
        Assert.Equal(2, defStatus.RemainingTurns);
    }

    [Fact]
    public void BasicSkill_CapsBlessingAt5Stacks()
    {
        var handler = CreateHandler();
        var siba = CreateSiba();
        siba.SetResource(SibaThienThanSkillCodes.ResourceBlessing, 5); // already 5

        var ally = CreateAlly(2, position: 1, hp: 500, maxHp: 1000);

        var context = new SkillExecutionContext
        {
            Skill = CreateBasicSkill(),
            Actor = siba,
            Combatants = [siba, ally],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        var result = handler.Execute(context);

        Assert.Equal(5, siba.GetResource(SibaThienThanSkillCodes.ResourceBlessing, 0));
        Assert.DoesNotContain(result.Events, e => e.EventType == BattleCodes.ResourceChanged);
    }

    [Fact]
    public void EnergySkill_NormalVersion_DoesNotHeal_AppliesCelestialProtectionAndDispelsDebuff()
    {
        var handler = CreateHandler();
        var siba = CreateSiba(magicDmg: 200);
        siba.SetResource(SibaThienThanSkillCodes.ResourceBlessing, 3); // < 5 stacks -> Normal version

        var ally1 = CreateAlly(2, position: 1, hp: 500, maxHp: 1000);
        // Add a dispellable debuff to ally1
        ally1.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "bleed1",
            EffectTypeCode = BattleCodes.Bleed,
            SourceSkillId = "enemy_skill",
            RemainingTurns = 2,
            Dispellable = true
        });

        // Add an undispellable effect to ally1
        ally1.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "boss1",
            EffectTypeCode = BattleCodes.LossOfConfidence,
            SourceSkillId = "boss_skill",
            RemainingTurns = 3,
            Dispellable = false
        });

        var context = new SkillExecutionContext
        {
            Skill = CreateUltimateSkill(),
            Actor = siba,
            Combatants = [siba, ally1],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        var result = handler.Execute(context);

        // Normal version must NOT heal
        Assert.DoesNotContain(result.Events, e => e.EventType == "HEAL");
        Assert.Equal(500, ally1.Hp);

        // Must apply CELESTIAL_PROTECTION to all living allies
        Assert.Contains(ally1.StatusEffects, s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusCelestialProtection && s.RemainingTurns == 2);
        Assert.Contains(siba.StatusEffects, s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusCelestialProtection && s.RemainingTurns == 2);

        // Must dispel the dispellable debuff (Bleed) but keep LossOfConfidence
        Assert.DoesNotContain(ally1.StatusEffects, s => s.EffectTypeCode == BattleCodes.Bleed);
        Assert.Contains(ally1.StatusEffects, s => s.EffectTypeCode == BattleCodes.LossOfConfidence);
        Assert.Contains(result.Events, e => e.EventType == BattleCodes.StatusRemoved && e.EffectTypeCode == BattleCodes.Bleed);

        // Does NOT consume Ân Phúc stacks
        Assert.Equal(3, siba.GetResource(SibaThienThanSkillCodes.ResourceBlessing, 0));
        Assert.DoesNotContain(result.Events, e => e.EventType == BattleCodes.ResourceChanged);
    }

    [Fact]
    public void EnergySkill_EmpoweredVersion_HealsTeam_RefreshesEncouragement_Grants15Energy_AndConsumes5Stacks()
    {
        var handler = CreateHandler();
        var siba = CreateSiba(magicDmg: 200, hp: 500, maxHp: 1000); // 110% of 200 = 220 heal
        siba.SetResource(SibaThienThanSkillCodes.ResourceBlessing, 5); // 5 stacks -> Empowered!

        // Ally 1 has Encouragement with 1 turn left and 10 energy
        var ally1 = CreateAlly(2, position: 2, hp: 500, maxHp: 1000, energy: 10);
        ally1.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "enc1",
            EffectTypeCode = SibaThienThanSkillCodes.StatusOffense,
            StatusGroup = SibaThienThanSkillCodes.StatusGroupEncouragement,
            SourceSkillId = "test",
            RemainingTurns = 1
        });

        // Ally 2 does NOT have Encouragement
        var ally2 = CreateAlly(3, position: 1, hp: 600, maxHp: 1000, energy: 10);

        var context = new SkillExecutionContext
        {
            Skill = CreateUltimateSkill(),
            Actor = siba,
            Combatants = [siba, ally1, ally2],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        var result = handler.Execute(context);

        // Must emit empowered cast event
        Assert.Contains(result.Events, e => e.EventType == SibaThienThanSkillCodes.EmpoweredCast);

        // 1. Both allies and Siba healed for 110% Magic Damage (220)
        Assert.Equal(720, ally1.Hp);
        Assert.Equal(820, ally2.Hp);
        var healEvents = result.Events.Where(e => e.EventType == "HEAL").ToList();
        Assert.Equal(3, healEvents.Count); // Siba, ally1, ally2
        Assert.All(healEvents, h => Assert.Equal(220, h.Value));

        // 2. Only Ally 1 (having Encouragement) gets Encouragement refreshed to 2 turns
        var encStatus = ally1.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == SibaThienThanSkillCodes.StatusOffense);
        Assert.NotNull(encStatus);
        Assert.Equal(2, encStatus.RemainingTurns);
        Assert.Contains(result.Events, e => e.EventType == BattleCodes.StatusRefreshed && e.TargetId == ally1.Id && e.EffectTypeCode == SibaThienThanSkillCodes.StatusOffense);

        // Ally 2 did NOT get Encouragement applied
        Assert.DoesNotContain(ally2.StatusEffects, s => s.StatusGroup == SibaThienThanSkillCodes.StatusGroupEncouragement);

        // 3. Only Ally 1 receives +15 Energy (10 -> 25)
        Assert.Equal(25, ally1.Energy);
        Assert.Equal(10, ally2.Energy); // Unchanged
        var energyEvt = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.EnergyChanged && e.TargetId == ally1.Id);
        Assert.NotNull(energyEvt);
        Assert.Equal(15, energyEvt.Value);

        // 4. Consumes 5 Ân Phúc (5 -> 0)
        Assert.Equal(0, siba.GetResource(SibaThienThanSkillCodes.ResourceBlessing, 0));
        var resEvt = result.Events.FirstOrDefault(e => e.EventType == BattleCodes.ResourceChanged && e.ResourceCode == SibaThienThanSkillCodes.ResourceBlessing);
        Assert.NotNull(resEvt);
        Assert.Equal(-5, resEvt.Value);
    }

    [Fact]
    public void DeadAllies_DoNotReceiveHealOrBuffs()
    {
        var handler = CreateHandler();
        var siba = CreateSiba();
        siba.SetResource(SibaThienThanSkillCodes.ResourceBlessing, 5);

        var deadAlly = CreateAlly(2, position: 1, hp: 0, maxHp: 1000); // Dead
        var liveAlly = CreateAlly(3, position: 2, hp: 500, maxHp: 1000);

        var context = new SkillExecutionContext
        {
            Skill = CreateUltimateSkill(),
            Actor = siba,
            Combatants = [siba, deadAlly, liveAlly],
            Random = new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "act1"
        };

        var result = handler.Execute(context);

        // Dead ally must not have been targeted or received heal
        Assert.DoesNotContain(deadAlly.Id, result.TargetedCombatantIds);
        Assert.DoesNotContain(result.Events, e => e.TargetId == deadAlly.Id);
        Assert.Equal(0, deadAlly.Hp);
    }

    private sealed class DeterministicRandom : Random
    {
        private readonly int _fixedValue;
        public DeterministicRandom(int fixedValue) => _fixedValue = fixedValue;
        public override int Next(int minValue, int maxValue) => _fixedValue;
    }
}
