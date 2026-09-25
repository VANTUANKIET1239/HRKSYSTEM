using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.HaiLastSmile;
using GAME.Domain.Battle.Targets;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class HaiLastSmileSkillHandlerTests
{
    private readonly BattleSimulationEngine _engine = new();

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

    private static BattleSkill CreateBugSlashSkill() => new()
    {
        Id = HaiLastSmileSkillCodes.Basic,
        Name = "Dao Rạch Bug",
        SkillTypeCode = BattleCodes.Normal,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 1.0m)]
            },
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                EffectTypeCode = BattleCodes.Bleed,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DurationTurns = 2,
                ChancePercent = 30m,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.18m)],
                Parameters = Params(
                    DecimalParam("ARMOR_IGNORE_PERCENT", 30m),
                    BoolParam("CAN_CRIT", false),
                    BoolParam("CAN_KILL", false))
            }
        ]
    };

    private static BattleSkill CreateLastLaughSkill() => new()
    {
        Id = HaiLastSmileSkillCodes.Ultimate,
        Name = "Cười Đi, Sắp Hết Lượt Rồi",
        SkillTypeCode = BattleCodes.Energy,
        EnergyCost = 100,
        Effects =
        [
            new BattleSkillEffect
            {
                DisplayOrder = 1,
                EffectTypeCode = BattleCodes.Panic,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DurationTurns = 1,
                ChancePercent = 100m,
                StatModifiers = [new BattleStatModifier("DEF", "PERCENT", -20m, "giảm giáp")]
            },
            new BattleSkillEffect
            {
                DisplayOrder = 2,
                EffectTypeCode = BattleCodes.BleedDetonate,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                Parameters = Params(
                    StringParam("REQUIRED_STATUS_CODE", "BLEED"),
                    DecimalParam("DETONATION_MULTIPLIER", 1.30m),
                    DecimalParam("ARMOR_IGNORE_PERCENT", 30m),
                    BoolParam("CAN_CRIT", false),
                    BoolParam("REMOVE_STATUS_AFTER_EXECUTION", true))
            },
            new BattleSkillEffect
            {
                DisplayOrder = 3,
                ExecutionGroup = "HIT_1",
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.80m)]
            },
            new BattleSkillEffect
            {
                DisplayOrder = 4,
                ExecutionGroup = "HIT_1",
                EffectTypeCode = BattleCodes.Bleed,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DurationTurns = 2,
                ChancePercent = 30m,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.18m)],
                Parameters = Params(
                    DecimalParam("ARMOR_IGNORE_PERCENT", 30m),
                    BoolParam("CAN_CRIT", false),
                    BoolParam("CAN_KILL", false))
            },
            new BattleSkillEffect
            {
                DisplayOrder = 5,
                ExecutionGroup = "HIT_2",
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.90m)]
            },
            new BattleSkillEffect
            {
                DisplayOrder = 6,
                ExecutionGroup = "HIT_2",
                EffectTypeCode = BattleCodes.Bleed,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DurationTurns = 2,
                ChancePercent = 30m,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.18m)],
                Parameters = Params(
                    DecimalParam("ARMOR_IGNORE_PERCENT", 30m),
                    BoolParam("CAN_CRIT", false),
                    BoolParam("CAN_KILL", false))
            },
            new BattleSkillEffect
            {
                DisplayOrder = 7,
                ExecutionGroup = "HIT_3",
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 1.30m)]
            },
            new BattleSkillEffect
            {
                DisplayOrder = 8,
                ExecutionGroup = "HIT_3",
                EffectTypeCode = BattleCodes.Bleed,
                TargetTypeCode = BattleCodes.LowestHpPercent,
                DurationTurns = 2,
                ChancePercent = 30m,
                DamageSchoolCode = BattleCodes.Physical,
                Scalings = [new BattleEffectScaling("ATK", 0.18m)],
                Parameters = Params(
                    DecimalParam("ARMOR_IGNORE_PERCENT", 30m),
                    BoolParam("CAN_CRIT", false),
                    BoolParam("CAN_KILL", false))
            },
            new BattleSkillEffect
            {
                DisplayOrder = 9,
                ConditionCode = "TARGET_DEFEATED",
                EffectTypeCode = BattleCodes.EnergyChange,
                TargetTypeCode = BattleCodes.Self,
                BaseValue = 25m,
                Parameters = Params(IntParam("ENERGY_GAIN", 25))
            },
            new BattleSkillEffect
            {
                DisplayOrder = 10,
                ConditionCode = "TARGET_DEFEATED",
                EffectTypeCode = BattleCodes.DamageReduction,
                TargetTypeCode = BattleCodes.Self,
                DurationTurns = 1,
                BaseValue = 40m,
                StatModifiers = [new BattleStatModifier("DAMAGE_REDUCTION", "PERCENT", 40m, "giảm sát thương nhận vào")],
                Parameters = Params(BoolParam("CONSUME_ON_HIT", true))
            },
            new BattleSkillEffect
            {
                DisplayOrder = 11,
                ConditionCode = "TARGET_SURVIVED",
                EffectTypeCode = BattleCodes.StatDebuff,
                TargetTypeCode = BattleCodes.Self,
                DurationTurns = 1,
                BaseValue = 15m,
                StatModifiers = [new BattleStatModifier("DEF", "PERCENT", -15m, "giảm giáp")]
            }
        ]
    };

    private static BattleCombatant CreateHero(
        long id, int team, int hp = 1000, int attack = 100, int defense = 0,
        int speed = 100, int energy = 0, decimal critChance = 0m,
        BattleSkill? basicSkill = null, BattleSkill? energySkill = null) => new()
        {
            Id = id,
            SourceHeroId = id,
            Team = team,
            Position = (int)id,
            Name = $"Hero {id}",
            MaxHp = hp,
            Hp = hp,
            Atk = attack,
            Def = defense,
            Spd = speed,
            CritChance = critChance,
            CritDamage = 150m,
            Energy = energy,
            BasicSkill = basicSkill ?? CreateBugSlashSkill(),
            EnergySkill = energySkill
        };

    // 1. Đánh thường gây đúng 100% ATK damage (defense = 0, no crit)
    [Fact]
    public void Basic_attack_deals_exact_100_percent_attack_damage()
    {
        var attacker = CreateHero(1, 0, attack: 200, defense: 0, speed: 200);
        var target = CreateHero(2, 1, hp: 1000, defense: 0, speed: 50);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            RandomSeed = 42,
            MaxRounds = 1,
            Combatants = [attacker, target]
        });

        var damageEvent = result.Events.First(x => x.EventType == "DAMAGE" && x.ActorId == 1);
        Assert.Equal(200, damageEvent.Value);
        Assert.Equal(800, damageEvent.HpAfter);
    }

    // 2. BLEED chỉ được áp dụng khi roll thành công (30%)
    [Fact]
    public void Bleed_is_applied_only_on_successful_roll_chance()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));

        var skill = CreateBugSlashSkill();
        var actor = CreateHero(1, 0, attack: 100, speed: 200);

        var appliedCount = 0;
        for (var seed = 0; seed < 100; seed++)
        {
            var testTarget = CreateHero(2, 1, hp: 1000, speed: 50);
            var context = new SkillExecutionContext
            {
                Skill = skill,
                Actor = actor,
                Combatants = [actor, testTarget],
                Random = new Random(seed),
                Round = 1,
                Turn = 1
            };
            var execution = handler.Execute(context);
            if (testTarget.StatusEffects.Any(x => x.EffectTypeCode == BattleCodes.Bleed))
                appliedCount++;
        }

        Assert.True(appliedCount > 15 && appliedCount < 45, $"Applied count was {appliedCount}");
    }

    // 3. Đòn không gây damage không gây BLEED
    [Fact]
    public void No_bleed_applied_if_attack_does_not_deal_damage()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));

        var skill = CreateBugSlashSkill();
        var actor = CreateHero(1, 0, attack: 0, speed: 200);

        for (var seed = 0; seed < 20; seed++)
        {
            var testTarget = CreateHero(2, 1, hp: 1000, speed: 50);
            var context = new SkillExecutionContext
            {
                Skill = skill,
                Actor = actor,
                Combatants = [actor, testTarget],
                Random = new Random(seed),
                Round = 1,
                Turn = 1
            };
            handler.Execute(context);
            Assert.DoesNotContain(testTarget.StatusEffects, x => x.EffectTypeCode == BattleCodes.Bleed);
        }
    }

    // 4. BLEED kích hoạt đúng một lần ở đầu mỗi lượt
    [Fact]
    public void Bleed_ticks_exactly_once_at_start_of_each_turn()
    {
        var target = CreateHero(2, 1, hp: 1000, defense: 0, speed: 100);
        target.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "1:HAI_BUG_SLASH:BLEED:2",
            EffectTypeCode = BattleCodes.Bleed,
            SourceHeroId = 1,
            SourceSkillId = HaiLastSmileSkillCodes.Basic,
            RemainingTurns = 2,
            AppliedTurn = 0,
            Value = 50m,
            StatModifiers = []
        });

        var slowerHero = CreateHero(1, 0, speed: 50);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            RandomSeed = 1,
            MaxRounds = 1,
            Combatants = [slowerHero, target]
        });

        var bleedDamageEvents = result.Events.Where(x => x.EventType == BattleCodes.BleedDamage && x.TargetId == target.Id).ToList();
        Assert.Single(bleedDamageEvents);
        var turnStartIndex = result.Events.ToList().FindIndex(x => x.EventType == "TURN_START" && x.ActorId == target.Id);
        var bleedIndex = result.Events.ToList().FindIndex(x => x.EventType == BattleCodes.BleedDamage && x.TargetId == target.Id);
        Assert.True(bleedIndex > turnStartIndex);
    }

    // 5. BLEED tồn tại đúng 2 lượt
    [Fact]
    public void Bleed_persists_for_exact_2_turns()
    {
        var target = CreateHero(2, 1, hp: 5000, defense: 0, speed: 200);
        target.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "1:HAI_BUG_SLASH:BLEED:2",
            EffectTypeCode = BattleCodes.Bleed,
            SourceHeroId = 1,
            SourceSkillId = HaiLastSmileSkillCodes.Basic,
            RemainingTurns = 2,
            AppliedTurn = 0,
            Value = 50m,
            StatModifiers = []
        });

        var dummy = CreateHero(1, 0, speed: 50);

        var result = _engine.Simulate(new BattleSimulationRequest
        {
            RandomSeed = 1,
            MaxRounds = 3,
            Combatants = [dummy, target]
        });

        var bleedTicks = result.Events.Where(x => x.EventType == BattleCodes.BleedDamage && x.TargetId == target.Id).ToList();
        Assert.Equal(2, bleedTicks.Count);

        var expiredEvent = result.Events.FirstOrDefault(x => x.EventType == "STATUS_EXPIRED" && x.EffectTypeCode == BattleCodes.Bleed);
        Assert.NotNull(expiredEvent);
    }

    // 6. BLEED không cộng dồn và được refresh
    [Fact]
    public void Bleed_does_not_stack_and_refreshes_duration()
    {
        var handler = new BleedEffectHandler();
        var actor = CreateHero(1, 0, attack: 200);
        var target = CreateHero(2, 1, hp: 1000);
        var skill = CreateBugSlashSkill();

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Bleed,
            TargetTypeCode = BattleCodes.LowestHpPercent,
            DurationTurns = 2,
            Scalings = [new BattleEffectScaling("ATK", 0.18m)]
        };

        var events1 = handler.Apply(new BattleEffectContext
        {
            Effect = effect, Skill = skill, Actor = actor, Target = target,
            SelectedTargets = [target], Combatants = [actor, target],
            Random = new Random(1), Round = 1, Turn = 1
        });

        Assert.Single(target.StatusEffects);
        Assert.Equal(2, target.StatusEffects[0].RemainingTurns);
        Assert.Equal(1, target.StatusEffects[0].Stacks);
        Assert.Equal("STATUS_APPLIED", events1[0].EventType);

        target.StatusEffects[0].RemainingTurns = 1;

        var events2 = handler.Apply(new BattleEffectContext
        {
            Effect = effect, Skill = skill, Actor = actor, Target = target,
            SelectedTargets = [target], Combatants = [actor, target],
            Random = new Random(1), Round = 1, Turn = 2
        });

        Assert.Single(target.StatusEffects);
        Assert.Equal(2, target.StatusEffects[0].RemainingTurns);
        Assert.Equal(BattleCodes.StatusRefreshed, events2[0].EventType);
    }

    // 7. BLEED không chí mạng và bỏ qua 30% phòng thủ
    [Fact]
    public void Bleed_cannot_crit_and_ignores_30_percent_defense()
    {
        var handler = new BleedEffectHandler();
        var target = CreateHero(2, 1, hp: 1000, defense: 100);
        var status = new BattleStatusEffect
        {
            InstanceId = "1:BLEED:2",
            EffectTypeCode = BattleCodes.Bleed,
            SourceHeroId = 1,
            SourceSkillId = "SKILL",
            RemainingTurns = 2,
            Value = 100m,
            ArmorIgnorePercent = 30m,
            CanCrit = false,
            StatModifiers = []
        };

        var events = handler.OnTurnStart(status, target, 1, 1, [target]);
        Assert.Single(events);
        var tick = events[0];

        Assert.False(tick.IsCrit);
        Assert.Equal(59, tick.Value);
    }

    // 8. BLEED không hạ HP xuống dưới 1
    [Fact]
    public void Bleed_does_not_reduce_hp_below_1()
    {
        var handler = new BleedEffectHandler();
        var target = CreateHero(2, 1, hp: 10, defense: 0);
        var status = new BattleStatusEffect
        {
            InstanceId = "1:BLEED:2",
            EffectTypeCode = BattleCodes.Bleed,
            SourceHeroId = 1,
            SourceSkillId = "SKILL",
            RemainingTurns = 2,
            Value = 500m,
            StatModifiers = []
        };

        var events = handler.OnTurnStart(status, target, 1, 1, [target]);
        Assert.Single(events);
        Assert.Equal(1, target.Hp);
        Assert.Equal(1, events[0].HpAfter);
    }

    // 9. Ultimate chọn mục tiêu có phần trăm HP thấp nhất
    [Fact]
    public void Ultimate_targets_living_enemy_with_lowest_hp_percentage()
    {
        var targetSelector = new LowestHpPercentTargetSelector();
        var actor = CreateHero(1, 0);

        var enemy1 = new BattleCombatant
        {
            Id = 2, Team = 1, Position = 1, Name = "E1", MaxHp = 1000, Hp = 500,
            Atk = 100, Def = 0, Spd = 100, BasicSkill = CreateBugSlashSkill()
        };
        var enemy2 = new BattleCombatant
        {
            Id = 3, Team = 1, Position = 2, Name = "E2", MaxHp = 1000, Hp = 200,
            Atk = 100, Def = 0, Spd = 100, BasicSkill = CreateBugSlashSkill()
        };
        var enemy3 = new BattleCombatant
        {
            Id = 4, Team = 1, Position = 3, Name = "E3", MaxHp = 500, Hp = 150,
            Atk = 100, Def = 0, Spd = 100, BasicSkill = CreateBugSlashSkill()
        };

        var selected = targetSelector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor],
            Enemies = [enemy1, enemy2, enemy3],
            Random = new Random(1)
        });

        Assert.Single(selected);
        Assert.Equal(enemy2.Id, selected[0].Id);
    }

    // 10. Ultimate phát đúng ba hit với hệ số 80%, 90%, 130%
    [Fact]
    public void Ultimate_deals_three_hits_with_coefficients_80_90_130_percent()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));

        var actor = CreateHero(1, 0, attack: 1000, defense: 0, speed: 200);
        var target = CreateHero(2, 1, hp: 10000, defense: 0, speed: 50);
        var ult = CreateLastLaughSkill();

        var context = new SkillExecutionContext
        {
            Skill = ult,
            Actor = actor,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        var hits = result.Events.Where(x => x.EventType == "DAMAGE" && x.ActorId == 1).ToList();
        Assert.Equal(3, hits.Count);
        Assert.Equal(800, hits[0].Value);  // 80% of 1000
        Assert.Equal(900, hits[1].Value);  // 90% of 1000
        Assert.Equal(1300, hits[2].Value); // 130% of 1000
    }

    // 11. BLEED có sẵn được kích nổ với hệ số 130%
    [Fact]
    public void Preexisting_bleed_is_detonated_for_130_percent_remaining_damage()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));

        var actor = CreateHero(1, 0, attack: 1000, defense: 0);
        var target = CreateHero(2, 1, hp: 10000, defense: 0);

        target.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "1:BLEED:2",
            EffectTypeCode = BattleCodes.Bleed,
            SourceHeroId = 1,
            SourceSkillId = "SKILL",
            RemainingTurns = 2,
            Value = 100m,
            StatModifiers = []
        });

        var context = new SkillExecutionContext
        {
            Skill = CreateLastLaughSkill(),
            Actor = actor,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        var detonateEvent = result.Events.FirstOrDefault(x => x.EventType == BattleCodes.BleedDetonated);
        Assert.NotNull(detonateEvent);
        // (2 turns * 100) * 1.30 = 260
        Assert.Equal(260, detonateEvent.Value);
        Assert.DoesNotContain(target.StatusEffects, x => x.InstanceId == "1:BLEED:2");
    }

    // 12. BLEED tạo trong chính ultimate không bị kích nổ ngay
    [Fact]
    public void Bleed_inflicted_during_ultimate_is_not_detonated_in_same_turn()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));

        var actor = CreateHero(1, 0, attack: 1000, defense: 0);
        var target = CreateHero(2, 1, hp: 10000, defense: 0);

        var context = new SkillExecutionContext
        {
            Skill = CreateLastLaughSkill(),
            Actor = actor,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);
        Assert.DoesNotContain(result.Events, x => x.EventType == BattleCodes.BleedDetonated);
    }

    // 13. Kết liễu thành công tăng 25% action bar (ENERGY_CHANGE) và nhận effect phòng thủ
    [Fact]
    public void Successful_kill_grants_25_percent_action_bar_and_defense_protection_buff()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));

        var actor = CreateHero(1, 0, attack: 1000, defense: 0, energy: 0);
        var target = CreateHero(2, 1, hp: 500, defense: 0);

        var context = new SkillExecutionContext
        {
            Skill = CreateLastLaughSkill(),
            Actor = actor,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        Assert.Equal(0, target.Hp);
        Assert.Equal(25, actor.Energy);
        Assert.Contains(result.Events, x => x.EventType == BattleCodes.EnergyChanged && x.Value == 25);
        Assert.Contains(actor.StatusEffects, x => x.EffectTypeCode == BattleCodes.DamageReduction && x.Value == 40m);
    }

    // 14. Kết liễu thất bại gây giảm 15% DEF
    [Fact]
    public void Failed_kill_applies_15_percent_defense_reduction_to_actor()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));

        var actor = CreateHero(1, 0, attack: 100, defense: 100, energy: 0);
        var target = CreateHero(2, 1, hp: 10000, defense: 0);

        var context = new SkillExecutionContext
        {
            Skill = CreateLastLaughSkill(),
            Actor = actor,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        };

        var result = handler.Execute(context);

        Assert.True(target.Hp > 0);
        Assert.Equal(0, actor.Energy);
        var debuff = actor.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.StatDebuff && x.Value == 15m);
        Assert.NotNull(debuff);
        Assert.Contains(result.Events, x => x.EventType == "STATUS_APPLIED" && x.EffectTypeCode == BattleCodes.StatDebuff && x.Value == 15);
    }

    // 15. Replay cùng random seed phải tạo cùng kết quả
    [Fact]
    public void Deterministic_replay_with_same_random_seed_produces_identical_events()
    {
        var attacker1 = CreateHero(1, 0, attack: 200, critChance: 30m, speed: 200, energy: 100, energySkill: CreateLastLaughSkill());
        var target1 = CreateHero(2, 1, hp: 3000, defense: 50, speed: 100);

        var attacker2 = CreateHero(1, 0, attack: 200, critChance: 30m, speed: 200, energy: 100, energySkill: CreateLastLaughSkill());
        var target2 = CreateHero(2, 1, hp: 3000, defense: 50, speed: 100);

        var result1 = _engine.Simulate(new BattleSimulationRequest { RandomSeed = 9999, MaxRounds = 5, Combatants = [attacker1, target1] });
        var result2 = _engine.Simulate(new BattleSimulationRequest { RandomSeed = 9999, MaxRounds = 5, Combatants = [attacker2, target2] });

        Assert.Equal(result1.Events.Count, result2.Events.Count);
        for (var i = 0; i < result1.Events.Count; i++)
        {
            var e1 = result1.Events[i];
            var e2 = result2.Events[i];
            Assert.Equal(e1.EventType, e2.EventType);
            Assert.Equal(e1.Value, e2.Value);
            Assert.Equal(e1.ActorId, e2.ActorId);
            Assert.Equal(e1.TargetId, e2.TargetId);
            Assert.Equal(e1.IsCrit, e2.IsCrit);
        }
    }

    // 16. Handler được resolve đúng qua DI, không xuất hiện constructor ambiguous
    [Fact]
    public void Handlers_are_resolved_cleanly_via_DI_without_ambiguous_constructors()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IBattleEffectHandler, DamageEffectHandler>();
        services.AddSingleton<IBattleEffectHandler, HealEffectHandler>();
        services.AddSingleton<IBattleEffectHandler, BleedEffectHandler>();
        services.AddSingleton<IBattleEffectHandler, PanicEffectHandler>();
        services.AddSingleton<IBattleEffectHandler, BleedDetonateEffectHandler>();
        services.AddSingleton<IBattleEffectHandler, EnergyChangeEffectHandler>();
        services.AddSingleton<IBattleEffectHandler, DamageReductionEffectHandler>();
        services.AddSingleton<IBattleEffectHandler, StatDebuffEffectHandler>();
        services.AddSingleton<BattleEffectHandlerRegistry>();

        services.AddSingleton<IBattleTargetSelector, LowestHpPercentTargetSelector>();
        services.AddSingleton<IBattleTargetSelector, SelfTargetSelector>();
        services.AddSingleton<BattleTargetSelectorRegistry>();

        services.AddSingleton<DefaultSkillHandler>();
        services.AddSingleton<ISkillHandler, HaiLastSmileSkillHandler>();
        services.AddSingleton<SkillHandlerRegistry>();
        services.AddSingleton<IBattleSimulationEngine, BattleSimulationEngine>();

        var provider = services.BuildServiceProvider();

        var engine = provider.GetRequiredService<IBattleSimulationEngine>();
        Assert.NotNull(engine);

        var skillHandlers = provider.GetRequiredService<SkillHandlerRegistry>();
        Assert.NotNull(skillHandlers);

        var resolved = skillHandlers.Resolve(CreateLastLaughSkill());
        Assert.IsType<HaiLastSmileSkillHandler>(resolved);
    }

    // 17. Fail-fast validation khi thiếu parameter hoặc scaling bắt buộc
    [Fact]
    public void Missing_required_configuration_throws_invalid_operation_exception()
    {
        var handler = HaiLastSmileSkillHandler.Create(new DefaultSkillHandler(
            BattleEffectHandlerRegistry.CreateDefault(), BattleTargetSelectorRegistry.CreateDefault()));
        var actor = CreateHero(1, 0);
        var target = CreateHero(2, 1);

        // Case 1: Bleed effect thiếu scaling và BaseValue
        var invalidBleedSkill = new BattleSkill
        {
            Id = HaiLastSmileSkillCodes.Basic,
            Name = "Invalid Bleed",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.LowestHpPercent,
                    Scalings = [new BattleEffectScaling("ATK", 1.0m)]
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Bleed,
                    TargetTypeCode = BattleCodes.LowestHpPercent,
                    DurationTurns = 2,
                    ChancePercent = 100m
                }
            ]
        };

        var exBleed = Assert.Throws<InvalidOperationException>(() =>
            handler.Execute(new SkillExecutionContext
            {
                Skill = invalidBleedSkill,
                Actor = actor,
                Combatants = [actor, target],
                Random = new Random(1),
                Round = 1,
                Turn = 1
            }));
        Assert.Contains("BLEED", exBleed.Message);

        // Case 2: BleedDetonate effect thiếu DETONATION_MULTIPLIER
        target.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "1:BLEED:2",
            EffectTypeCode = BattleCodes.Bleed,
            SourceHeroId = 1,
            SourceSkillId = "SKILL",
            RemainingTurns = 2,
            Value = 100m,
            StatModifiers = []
        });

        var invalidDetonateSkill = new BattleSkill
        {
            Id = HaiLastSmileSkillCodes.Ultimate,
            Name = "Invalid Detonate",
            SkillTypeCode = BattleCodes.Energy,
            Effects =
            [
                new BattleSkillEffect
                {
                    DisplayOrder = 1,
                    EffectTypeCode = BattleCodes.BleedDetonate,
                    TargetTypeCode = BattleCodes.LowestHpPercent
                }
            ]
        };

        var exDetonate = Assert.Throws<InvalidOperationException>(() =>
            handler.Execute(new SkillExecutionContext
            {
                Skill = invalidDetonateSkill,
                Actor = actor,
                Combatants = [actor, target],
                Random = new Random(1),
                Round = 1,
                Turn = 1
            }));
        Assert.Contains("DETONATION_MULTIPLIER", exDetonate.Message);
    }
}
