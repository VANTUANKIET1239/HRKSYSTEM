using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class SixRareHeroesTests
{
    private readonly BattleEffectHandlerRegistry _effects = BattleEffectHandlerRegistry.CreateDefault();
    private readonly BattleTargetSelectorRegistry _selectors = BattleTargetSelectorRegistry.CreateDefault();
    private readonly DefaultSkillHandler _defaultHandler;

    public SixRareHeroesTests()
    {
        _defaultHandler = new DefaultSkillHandler(_effects, _selectors);
    }

    #region 1. Selector Tests

    [Fact]
    public void EnemyRandomDistinctN_Selects_Distinct_Targets_Up_To_N()
    {
        var selector = new EnemyRandomDistinctNTargetSelector();
        var actor = Hero(1, 0, 1);
        var enemies = new[] { Hero(10, 1, 1), Hero(11, 1, 2), Hero(12, 1, 3), Hero(13, 1, 4), Hero(14, 1, 5) };

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            TargetTypeCode = BattleCodes.EnemyRandomDistinctN,
            Parameters = new Dictionary<string, BattleSkillEffectParameter>
            {
                ["TARGET_COUNT"] = new("TARGET_COUNT", null, 3, null, null)
            }
        };

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor],
            Enemies = enemies,
            Random = new Random(42),
            Effect = effect
        });

        Assert.Equal(3, selected.Count);
        Assert.Equal(3, selected.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public void EnemyRandomDistinctN_When_Fewer_Enemies_Than_N_Returns_All_Living()
    {
        var selector = new EnemyRandomDistinctNTargetSelector();
        var actor = Hero(1, 0, 1);
        var enemies = new[] { Hero(10, 1, 1), Hero(11, 1, 2) };

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            TargetTypeCode = BattleCodes.EnemyRandomDistinctN,
            Parameters = new Dictionary<string, BattleSkillEffectParameter>
            {
                ["TARGET_COUNT"] = new("TARGET_COUNT", null, 3, null, null)
            }
        };

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor],
            Enemies = enemies,
            Random = new Random(42),
            Effect = effect
        });

        Assert.Equal(2, selected.Count);
    }

    [Fact]
    public void EnemyRandomDistinctN_Deterministic_With_Same_Seed()
    {
        var selector = new EnemyRandomDistinctNTargetSelector();
        var actor = Hero(1, 0, 1);
        var enemies1 = new[] { Hero(10, 1, 1), Hero(11, 1, 2), Hero(12, 1, 3), Hero(13, 1, 4), Hero(14, 1, 5) };
        var enemies2 = new[] { Hero(10, 1, 1), Hero(11, 1, 2), Hero(12, 1, 3), Hero(13, 1, 4), Hero(14, 1, 5) };

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Damage,
            TargetTypeCode = BattleCodes.EnemyRandomDistinctN,
            Parameters = new Dictionary<string, BattleSkillEffectParameter>
            {
                ["TARGET_COUNT"] = new("TARGET_COUNT", null, 3, null, null)
            }
        };

        var selected1 = selector.Select(new BattleTargetContext
        {
            Actor = actor, Allies = [actor], Enemies = enemies1, Random = new Random(12345), Effect = effect
        });
        var selected2 = selector.Select(new BattleTargetContext
        {
            Actor = actor, Allies = [actor], Enemies = enemies2, Random = new Random(12345), Effect = effect
        });

        Assert.Equal(selected1.Select(x => x.Id), selected2.Select(x => x.Id));
    }

    [Fact]
    public void AllyLowestEnergy_Selects_Living_Ally_With_Lowest_Energy_Including_Caster()
    {
        var selector = new AllyLowestEnergyTargetSelector();
        var caster = Hero(1, 0, 1);
        caster.Energy = 10;
        var ally2 = Hero(2, 0, 2);
        ally2.Energy = 40;
        var ally3 = Hero(3, 0, 3);
        ally3.Energy = 80;

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = caster,
            Allies = [caster, ally2, ally3],
            Enemies = [Hero(10, 1, 1)],
            Random = new Random(1)
        });

        Assert.Single(selected);
        Assert.Equal(caster.Id, selected[0].Id);
    }

    [Fact]
    public void AllyLowestEnergy_TieBreaks_Consistently_On_Energy_HpPercent_Position_Id()
    {
        var selector = new AllyLowestEnergyTargetSelector();
        var caster = Hero(1, 0, 2, maxHp: 1000);
        caster.Energy = 50;
        caster.Hp = 800; // 80%

        var ally1 = Hero(2, 0, 1, maxHp: 1000);
        ally1.Energy = 50;
        ally1.Hp = 500; // 50% (lower HP%)

        var ally2 = Hero(3, 0, 3, maxHp: 1000);
        ally2.Energy = 50;
        ally2.Hp = 500; // 50% but position 3 > position 1

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = caster,
            Allies = [caster, ally1, ally2],
            Enemies = [Hero(10, 1, 1)],
            Random = new Random(1)
        });

        Assert.Single(selected);
        Assert.Equal(ally1.Id, selected[0].Id);
    }

    [Fact]
    public void AllyLowestEnergy_Ignores_Dead_Allies()
    {
        var selector = new AllyLowestEnergyTargetSelector();
        var caster = Hero(1, 0, 1);
        caster.Energy = 50;
        var deadAlly = Hero(2, 0, 2);
        deadAlly.Energy = 0;
        deadAlly.Hp = 0; // dead

        var livingAlly = Hero(3, 0, 3);
        livingAlly.Energy = 20;

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = caster,
            Allies = [caster, deadAlly, livingAlly],
            Enemies = [Hero(10, 1, 1)],
            Random = new Random(1)
        });

        Assert.Single(selected);
        Assert.Equal(livingAlly.Id, selected[0].Id);
    }

    #endregion

    #region 2. Effect Handler Tests

    [Fact]
    public void ActionBarChange_Decreases_Energy_And_Emits_Event()
    {
        var handler = new ActionBarChangeEffectHandler();
        var actor = Hero(1, 0, 1);
        var target = Hero(2, 1, 1);
        target.Energy = 50;

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.ActionBarChange,
            TargetTypeCode = BattleCodes.EnemySingle,
            BaseValue = -10,
            Parameters = new Dictionary<string, BattleSkillEffectParameter>
            {
                ["ACTION_BAR_DELTA"] = new("ACTION_BAR_DELTA", null, -10, null, null)
            }
        };

        var events = handler.Apply(new BattleEffectContext
        {
            Actor = actor,
            Target = target,
            Skill = new BattleSkill { Id = "TEST", Name = "Test", SkillTypeCode = BattleCodes.Normal, Effects = [] },
            Effect = effect,
            Combatants = [actor, target],
            SelectedTargets = [target],
            Random = new Random(1)
        });

        Assert.Equal(40, target.Energy);
        var evt = Assert.Single(events);
        Assert.Equal(BattleCodes.ActionBarChanged, evt.EventType);
        Assert.Equal(-10, evt.Value);
        Assert.Equal(50, evt.PreviousValue);
        Assert.Equal(40, evt.CurrentValue);
    }

    [Fact]
    public void ActionBarChange_Clamps_At_Zero()
    {
        var handler = new ActionBarChangeEffectHandler();
        var actor = Hero(1, 0, 1);
        var target = Hero(2, 1, 1);
        target.Energy = 5;

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.ActionBarChange,
            TargetTypeCode = BattleCodes.EnemySingle,
            BaseValue = -10
        };

        var events = handler.Apply(new BattleEffectContext
        {
            Actor = actor,
            Target = target,
            Skill = new BattleSkill { Id = "TEST", Name = "Test", SkillTypeCode = BattleCodes.Normal, Effects = [] },
            Effect = effect,
            Combatants = [actor, target],
            SelectedTargets = [target],
            Random = new Random(1)
        });

        Assert.Equal(0, target.Energy);
        var evt = Assert.Single(events);
        Assert.Equal(-5, evt.Value);
        Assert.Equal(0, evt.CurrentValue);
    }

    #endregion

    #region 3. Hero Skill Execution Tests

    [Fact]
    public void TrongChuaNo_Basic_Deals_105_Percent_Physical_Damage()
    {
        var actor = Hero(1, 0, 1, atk: 200);
        var target = Hero(2, 1, 1, def: 0);

        var skill = new BattleSkill
        {
            Id = "TRONG_CHUA_NO_BASIC",
            Name = "Rút Chốt Thử Thôi",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.05m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        var dmgEvent = Assert.Single(execution.Events, e => e.EventType == "DAMAGE");
        Assert.Equal((int)Math.Round(200 * 1.05m), dmgEvent.Value);
    }

    [Fact]
    public void TrongChuaNo_Energy_Deals_175_Percent_And_Can_Apply_Panic()
    {
        var actor = Hero(1, 0, 1, atk: 200);
        var target = Hero(2, 1, 1, def: 0);

        var skill = new BattleSkill
        {
            Id = "TRONG_CHUA_NO_THREE_SECONDS",
            Name = "Ba Giây Chưa Nổ",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.75m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Panic,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DurationTurns = 1,
                    ChancePercent = 100 // guaranteed for test
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        var dmg = execution.Events.Single(e => e.EventType == "DAMAGE");
        Assert.Equal((int)Math.Round(200 * 1.75m), dmg.Value);

        var panic = execution.Events.Single(e => e.EventType == "STATUS_APPLIED" && e.EffectTypeCode == BattleCodes.Panic);
        Assert.Equal(1, panic.RemainingTurns);
        Assert.Contains(target.StatusEffects, s => s.EffectTypeCode == BattleCodes.Panic);
    }

    [Fact]
    public void QuangVinhTHCS_Basic_Buffs_Self_Def_10_Percent()
    {
        var actor = Hero(1, 0, 1, atk: 100);
        var target = Hero(2, 1, 1, def: 0);

        var skill = new BattleSkill
        {
            Id = "QUANG_VINH_THCS_BASIC",
            Name = "Trực Nhật Kiên Cường",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 0.90m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatBuff,
                    TargetTypeCode = BattleCodes.Self,
                    DurationTurns = 1,
                    BaseValue = 10,
                    StatModifiers = [new BattleStatModifier("DEF", "PERCENT", 10m)]
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        Assert.Contains(actor.StatusEffects, s => s.EffectTypeCode == BattleCodes.StatBuff);
        var buffEvent = execution.Events.Single(e => e.EventType == "STATUS_APPLIED" && e.TargetId == actor.Id);
        Assert.Equal(1, buffEvent.RemainingTurns);
    }

    [Fact]
    public void QuangVinhTHCS_Energy_Shields_All_Allies_By_10_Percent_MaxHp()
    {
        var actor = Hero(1, 0, 1, maxHp: 1000, atk: 100);
        actor.Hp = 1000;

        var ally = Hero(2, 0, 2, maxHp: 2000);
        ally.Hp = 2000;

        var enemy1 = Hero(10, 1, 1);
        var enemy2 = Hero(11, 1, 2);

        var skill = new BattleSkill
        {
            Id = "QUANG_VINH_THCS_HONOR_BARRIER",
            Name = "Hàng Rào Danh Dự",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemyAll,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 0.65m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Shield,
                    TargetTypeCode = BattleCodes.AllyAll,
                    DurationTurns = 2,
                    Scalings = [new BattleEffectScaling("HP", 0.10m)]
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, ally, enemy1, enemy2],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        // 2 damage events on enemy1 and enemy2
        var dmgEvents = execution.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(2, dmgEvents.Count);

        // 2 shield events on actor and ally, shield value = 10% of actor's MaxHp (100)
        var shieldEvents = execution.Events.Where(e => e.EventType == "SHIELD_APPLIED").ToList();
        Assert.Equal(2, shieldEvents.Count);
        Assert.All(shieldEvents, s => Assert.Equal(100, s.Value));
    }

    [Fact]
    public void NguyenXamLon_Energy_Hits_Front_Row_And_Buffs_Self_Atk()
    {
        var actor = Hero(1, 0, 1, atk: 200);

        // Front row enemies: positions 1, 3, 5
        var frontEnemy1 = Hero(10, 1, 1);
        var frontEnemy2 = Hero(11, 1, 3);
        // Back row enemy: position 2
        var backEnemy = Hero(12, 1, 2);

        var skill = new BattleSkill
        {
            Id = "NGUYEN_XAM_LON_BOSS_ENTERS",
            Name = "Đại Ca Xuống Sân",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemyFrontRow,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.05m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatBuff,
                    TargetTypeCode = BattleCodes.Self,
                    DurationTurns = 2,
                    BaseValue = 10,
                    StatModifiers = [new BattleStatModifier("ATK", "PERCENT", 10m)]
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, frontEnemy1, frontEnemy2, backEnemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        var dmgEvents = execution.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(2, dmgEvents.Count);
        Assert.Contains(dmgEvents, e => e.TargetId == frontEnemy1.Id);
        Assert.Contains(dmgEvents, e => e.TargetId == frontEnemy2.Id);
        Assert.DoesNotContain(dmgEvents, e => e.TargetId == backEnemy.Id);

        var buff = execution.Events.Single(e => e.EventType == "STATUS_APPLIED" && e.TargetId == actor.Id);
        Assert.Equal(2, buff.RemainingTurns);
    }

    [Fact]
    public void TienDungXuan_Energy_Deals_70_Percent_Magic_AoE_And_Debuffs_MagicRes()
    {
        var actor = Hero(1, 0, 1, magicDamage: 200);

        var enemy1 = Hero(10, 1, 1);
        var enemy2 = Hero(11, 1, 2);

        var skill = new BattleSkill
        {
            Id = "TIEN_DUNG_XUAN_TEN_THOUSAND_GLYPHS",
            Name = "Vạn Tự Khai Hoa",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemyAll,
                    DamageSchoolCode = BattleCodes.Magic,
                    Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 0.70m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatDebuff,
                    TargetTypeCode = BattleCodes.EnemyAll,
                    DurationTurns = 2,
                    ChancePercent = 100, // guaranteed in test
                    BaseValue = -15,
                    StatModifiers = [new BattleStatModifier("MAGIC_RESISTANCE", "PERCENT", -15m)]
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, enemy1, enemy2],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        var dmg = execution.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(2, dmg.Count);
        Assert.All(dmg, d => Assert.Equal(BattleCodes.Magic, d.DamageSchoolCode));

        var debuffs = execution.Events.Where(e => e.EventType == "STATUS_APPLIED").ToList();
        Assert.Equal(2, debuffs.Count);
    }

    [Fact]
    public void VanTrongDienVang_ChainLightning_Hits_Up_To_3_Distinct_And_Stuns()
    {
        var actor = Hero(1, 0, 1, magicDamage: 200);

        var enemies = new[]
        {
            Hero(10, 1, 1), Hero(11, 1, 2), Hero(12, 1, 3), Hero(13, 1, 4), Hero(14, 1, 5)
        };

        var skill = new BattleSkill
        {
            Id = "VAN_TRONG_DIEN_VANG_CHAIN_LIGHTNING",
            Name = "Điện Vàng Liên Hoàn",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemyRandomDistinctN,
                    DamageSchoolCode = BattleCodes.Magic,
                    Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 0.85m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["TARGET_COUNT"] = new("TARGET_COUNT", null, 3, null, null),
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Stun,
                    TargetTypeCode = BattleCodes.EnemyRandomDistinctN,
                    DurationTurns = 1,
                    ChancePercent = 100, // guaranteed in test
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["TARGET_COUNT"] = new("TARGET_COUNT", null, 3, null, null)
                    }
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, ..enemies],
            Random = new Random(42),
            Round = 1,
            Turn = 1
        });

        var dmg = execution.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(3, dmg.Count);
        Assert.Equal(3, dmg.Select(d => d.TargetId).Distinct().Count());

        var stuns = execution.Events.Where(e => e.EventType == "STATUS_APPLIED" && e.EffectTypeCode == BattleCodes.Stun).ToList();
        Assert.Equal(3, stuns.Count);
        Assert.Equal(dmg.Select(d => d.TargetId).OrderBy(x => x), stuns.Select(s => s.TargetId).OrderBy(x => x));
    }

    [Fact]
    public void TuongLongCap3_Basic_Restores_10_Energy_To_Lowest_Energy_Ally()
    {
        var actor = Hero(1, 0, 1, atk: 100);
        actor.Energy = 50;

        var allyLowEnergy = Hero(2, 0, 2);
        allyLowEnergy.Energy = 10;

        var target = Hero(10, 1, 1);

        var skill = new BattleSkill
        {
            Id = "TUONG_LONG_CAP_3_BASIC",
            Name = "Giao Bài Tận Nơi",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 0.90m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, false, null)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.EnergyChange,
                    TargetTypeCode = BattleCodes.AllyLowestEnergy,
                    BaseValue = 10,
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["ENERGY_GAIN"] = new("ENERGY_GAIN", null, 10, null, null)
                    }
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = actor,
            Skill = skill,
            Combatants = [actor, allyLowEnergy, target],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        Assert.Equal(20, allyLowEnergy.Energy);
        var energyEvent = execution.Events.Single(e => e.EventType == BattleCodes.EnergyChanged);
        Assert.Equal(allyLowEnergy.Id, energyEvent.TargetId);
        Assert.Equal(10, energyEvent.Value);
    }

    [Fact]
    public void TuongLongCap3_Energy_Heals_All_Allies_By_16_Percent_MaxHp_And_Buffs_Speed()
    {
        var caster = Hero(1, 0, 1, maxHp: 1000);
        caster.Hp = 800;

        var ally = Hero(2, 0, 2, maxHp: 2000);
        ally.Hp = 1500;

        var enemy = Hero(10, 1, 1);

        var skill = new BattleSkill
        {
            Id = "TUONG_LONG_CAP_3_CLASS_BELL",
            Name = "Chuông Vào Tiết",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Heal,
                    TargetTypeCode = BattleCodes.AllyAll,
                    Scalings = [new BattleEffectScaling("HP", 0.16m)]
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatBuff,
                    TargetTypeCode = BattleCodes.AllyAll,
                    DurationTurns = 1,
                    BaseValue = 10,
                    StatModifiers = [new BattleStatModifier("SPD", "PERCENT", 10m)]
                }
            ]
        };

        var execution = _defaultHandler.Execute(new SkillExecutionContext
        {
            Actor = caster,
            Skill = skill,
            Combatants = [caster, ally, enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        // 16% of caster MaxHp (1000) = 160 heal
        var heals = execution.Events.Where(e => e.EventType == "HEAL").ToList();
        Assert.Equal(2, heals.Count);
        Assert.All(heals, h => Assert.Equal(160, h.Value));
        Assert.Equal(960, caster.Hp);
        Assert.Equal(1660, ally.Hp);

        var speedBuffs = execution.Events.Where(e => e.EventType == "STATUS_APPLIED" && e.EffectTypeCode == BattleCodes.StatBuff).ToList();
        Assert.Equal(2, speedBuffs.Count);
    }

    #endregion

    private static BattleCombatant Hero(
        long id,
        int team,
        int position,
        int maxHp = 1000,
        int atk = 100,
        int def = 50,
        int spd = 100,
        int magicDamage = 100,
        int magicResistance = 50) => new()
    {
        Id = id,
        SourceHeroId = id,
        Team = team,
        Position = position,
        Name = $"Hero_{id}",
        MaxHp = maxHp,
        Hp = maxHp,
        Atk = atk,
        Def = def,
        Spd = spd,
        MagicDamage = magicDamage,
        MagicResistance = magicResistance,
        CritChance = 10m,
        CritDamage = 150m,
        Energy = 0,
        MaxEnergy = 100,
        BasicSkill = new BattleSkill
        {
            Id = "DUMMY_BASIC",
            Name = "Dummy",
            SkillTypeCode = BattleCodes.Normal,
            Effects = []
        }
    };
}
