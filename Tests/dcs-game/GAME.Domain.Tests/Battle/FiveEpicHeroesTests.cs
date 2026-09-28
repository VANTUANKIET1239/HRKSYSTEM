using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class FiveEpicHeroesTests
{
    private readonly BattleEffectHandlerRegistry _effects = BattleEffectHandlerRegistry.CreateDefault();
    private readonly BattleTargetSelectorRegistry _selectors = BattleTargetSelectorRegistry.CreateDefault();
    private readonly DefaultSkillHandler _defaultHandler;

    public FiveEpicHeroesTests()
    {
        _defaultHandler = new DefaultSkillHandler(_effects, _selectors);
    }

    #region Helper Methods

    private static BattleSkillEffectParameter StringParam(string code, string val) => new(code, null, null, null, val);
    private static BattleSkillEffectParameter BoolParam(string code, bool val) => new(code, null, null, val, null);
    private static BattleSkillEffectParameter IntParam(string code, int val) => new(code, null, val, null, null);
    private static BattleSkillEffectParameter DecimalParam(string code, decimal val) => new(code, val, null, null, null);

    private static BattleCombatant Hero(
        long id, int team, int position,
        int hp = 1000, int atk = 100, int def = 50, int spd = 100,
        int magicDmg = 50, int magicRes = 50,
        decimal critChance = 0m, decimal critDmg = 150m)
    {
        return new BattleCombatant
        {
            Id = id,
            SourceHeroId = id,
            Team = team,
            Position = position,
            Name = $"Hero_{id}",
            Hp = hp,
            MaxHp = hp,
            Atk = atk,
            Def = def,
            Spd = spd,
            MagicDamage = magicDmg,
            MagicResistance = magicRes,
            CritChance = critChance,
            CritDamage = critDmg,
            Energy = 100,
            MaxEnergy = 100,
            BasicSkill = new BattleSkill
            {
                Id = "DUMMY_BASIC",
                Name = "Dummy Basic",
                SkillTypeCode = BattleCodes.Normal,
                Effects = []
            }
        };
    }

    private SkillExecutionContext Context(BattleCombatant actor, List<BattleCombatant> combatants, BattleSkill skill, Random? rnd = null) =>
        new()
        {
            Actor = actor,
            Combatants = combatants,
            Skill = skill,
            Random = rnd ?? new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = "action_1"
        };

    #endregion

    #region 1. Target Selector Tests: LOWEST_HP_PERCENT and ENEMY_BACK_ROW

    [Fact]
    public void LowestHpPercent_Selects_Living_Ally_With_Lowest_Hp_Percentage_For_Beneficial_Skill()
    {
        var selector = new LowestHpPercentTargetSelector();
        var actor = Hero(1, 0, 1, hp: 1000); // 100%
        var ally1 = Hero(2, 0, 2, hp: 1000);
        ally1.Hp = 500; // 50%
        var ally2 = Hero(3, 0, 3, hp: 1000);
        ally2.Hp = 800; // 80%

        var beneficialEffect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Heal,
            TargetTypeCode = BattleCodes.LowestHpPercent,
            Parameters = new Dictionary<string, BattleSkillEffectParameter>
            {
                ["TARGET_SIDE"] = StringParam("TARGET_SIDE", "ALLY")
            }
        };

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor, ally1, ally2],
            Enemies = [Hero(10, 1, 1)],
            Random = new Random(42),
            Effect = beneficialEffect
        });

        Assert.Single(selected);
        Assert.Equal(ally1.Id, selected[0].Id);
    }

    [Fact]
    public void LowestHpPercent_Does_Not_Select_Dead_Combatants()
    {
        var selector = new LowestHpPercentTargetSelector();
        var actor = Hero(1, 0, 1, hp: 1000);
        var allyDead = Hero(2, 0, 2, hp: 1000);
        allyDead.Hp = 0; // Dead
        var allyAlive = Hero(3, 0, 3, hp: 1000);
        allyAlive.Hp = 600; // 60%

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Heal,
            TargetTypeCode = BattleCodes.LowestHpPercent
        };

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor, allyDead, allyAlive],
            Enemies = [],
            Random = new Random(42),
            Effect = effect
        });

        Assert.Single(selected);
        Assert.Equal(allyAlive.Id, selected[0].Id);
    }

    [Fact]
    public void LowestHpPercent_Stable_Tie_Break_By_Position_Then_Id()
    {
        var selector = new LowestHpPercentTargetSelector();
        var actor = Hero(1, 0, 1, hp: 1000);
        var allyA = Hero(2, 0, 4, hp: 1000);
        allyA.Hp = 400; // 40%, Pos 4
        var allyB = Hero(3, 0, 2, hp: 1000);
        allyB.Hp = 400; // 40%, Pos 2

        var effect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Heal,
            TargetTypeCode = BattleCodes.LowestHpPercent
        };

        var selected = selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor, allyA, allyB],
            Enemies = [],
            Random = new Random(42),
            Effect = effect
        });

        Assert.Single(selected);
        Assert.Equal(allyB.Id, selected[0].Id); // Pos 2 wins over Pos 4
    }

    [Fact]
    public void EnemyBackRow_Selects_Living_Back_Row_Enemies_And_Falls_Back_To_Front_Row()
    {
        var selector = new EnemyBackRowTargetSelector();
        var actor = Hero(1, 0, 1);
        var frontEnemy = Hero(10, 1, 1, hp: 1000); // Pos 1 (front)
        var backEnemy1 = Hero(11, 1, 2, hp: 1000); // Pos 2 (back)
        var backEnemy2 = Hero(12, 1, 4, hp: 1000); // Pos 4 (back)

        // When back row exists
        var selected = selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor],
            Enemies = [frontEnemy, backEnemy1, backEnemy2],
            Random = new Random(42)
        });
        Assert.Equal(2, selected.Count);
        Assert.Contains(selected, x => x.Id == backEnemy1.Id);
        Assert.Contains(selected, x => x.Id == backEnemy2.Id);

        // When back row is eliminated, falls back to front row
        var fallbackSelected = selector.Select(new BattleTargetContext
        {
            Actor = actor,
            Allies = [actor],
            Enemies = [frontEnemy],
            Random = new Random(42)
        });
        Assert.Single(fallbackSelected);
        Assert.Equal(frontEnemy.Id, fallbackSelected[0].Id);
    }

    #endregion

    #region 2. Kiệt Bác Sĩ Tests

    [Fact]
    public void KietBacSi_Basic_Heals_Lowest_Hp_Percent_Ally_For_14_Percent_Caster_Max_Hp_And_Buffs_Resistance()
    {
        var kiet = Hero(1, 0, 2, hp: 1000); // MaxHp 1000 => 14% is 140
        var allyWounded = Hero(2, 0, 1, hp: 1000);
        allyWounded.Hp = 400; // 40%
        var enemy = Hero(10, 1, 1, hp: 1000);

        var basicSkill = new BattleSkill
        {
            Id = "KIET_BAC_SI_BASIC",
            Name = "Chẩn Mạch Từ Xa",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Heal,
                    TargetTypeCode = BattleCodes.LowestHpPercent,
                    Scalings = [new BattleEffectScaling("HP", 0.14m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["TARGET_SIDE"] = StringParam("TARGET_SIDE", "ALLY"),
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatBuff,
                    TargetTypeCode = BattleCodes.LowestHpPercent,
                    BaseValue = 10m,
                    DurationTurns = 1,
                    StatModifiers = [new BattleStatModifier("RESISTANCE", "PERCENT", 10m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["TARGET_SIDE"] = StringParam("TARGET_SIDE", "ALLY"),
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(kiet, [kiet, allyWounded, enemy], basicSkill));

        Assert.Equal(540, allyWounded.Hp); // 400 + 140 = 540
        var buff = allyWounded.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.StatBuff);
        Assert.NotNull(buff);
        Assert.Equal(1, buff.RemainingTurns);
        Assert.Contains(result.Events, e => e.EventType == "HEAL" && e.Value == 140 && e.TargetId == allyWounded.Id);
    }

    [Fact]
    public void KietBacSi_EmergencyProtocol_Heals_All_Allies_For_20_Percent_Caster_Max_Hp_And_Buffs_Resistance_2_Turns()
    {
        var kiet = Hero(1, 0, 2, hp: 1000); // 20% of 1000 = 200
        var ally1 = Hero(2, 0, 1, hp: 1000);
        ally1.Hp = 500;
        var ally2 = Hero(3, 0, 3, hp: 1000);
        ally2.Hp = 700;

        var ultSkill = new BattleSkill
        {
            Id = "KIET_BAC_SI_EMERGENCY_PROTOCOL",
            Name = "Phác Đồ Cấp Cứu",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Heal,
                    TargetTypeCode = BattleCodes.AllyAll,
                    Scalings = [new BattleEffectScaling("HP", 0.20m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatBuff,
                    TargetTypeCode = BattleCodes.AllyAll,
                    BaseValue = 15m,
                    DurationTurns = 2,
                    StatModifiers = [new BattleStatModifier("RESISTANCE", "PERCENT", 15m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(kiet, [kiet, ally1, ally2], ultSkill));

        Assert.Equal(700, ally1.Hp); // 500 + 200 = 700
        Assert.Equal(900, ally2.Hp); // 700 + 200 = 900
        Assert.Equal(1000, kiet.Hp); // full HP cap

        var ally1Buff = ally1.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.StatBuff);
        Assert.NotNull(ally1Buff);
        Assert.Equal(2, ally1Buff.RemainingTurns);
    }

    #endregion

    #region 3. Trường Kiệt Chu Mỏ Tests

    [Fact]
    public void TruongKiet_Basic_Deals_115_Percent_Physical_Damage_And_Can_Apply_Accuracy_Debuff()
    {
        var truongKiet = Hero(1, 0, 2, atk: 200); // 115% of 200 = 230 raw damage
        var enemy = Hero(10, 1, 1, hp: 1000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = "TRUONG_KIET_CHU_MO_BASIC",
            Name = "Hôn Gió Cảnh Cáo",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.15m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatDebuff,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    BaseValue = -15m,
                    DurationTurns = 1,
                    ChancePercent = 100m, // Force 100% to test application
                    StatModifiers = [new BattleStatModifier("ACCURACY", "PERCENT", -15m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(truongKiet, [truongKiet, enemy], basicSkill));

        Assert.Equal(770, enemy.Hp); // 1000 - 230 = 770
        var debuff = enemy.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.StatDebuff);
        Assert.NotNull(debuff);
        Assert.Equal(1, debuff.RemainingTurns);
    }

    [Fact]
    public void TruongKiet_SoulKiss_Deals_205_Percent_Damage_And_Applies_Panic_Blocking_Shields()
    {
        var truongKiet = Hero(1, 0, 2, atk: 200); // 205% of 200 = 410 raw damage
        var enemy = Hero(10, 1, 1, hp: 1000, def: 0);

        var ultSkill = new BattleSkill
        {
            Id = "TRUONG_KIET_SOUL_KISS",
            Name = "Nụ Hôn Đoạt Hồn",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 2.05m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Panic,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DurationTurns = 1,
                    ChancePercent = 100m, // Force 100% to test effect
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(truongKiet, [truongKiet, enemy], ultSkill));

        Assert.Equal(590, enemy.Hp); // 1000 - 410 = 590
        var panic = enemy.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.Panic);
        Assert.NotNull(panic);
        Assert.Equal(1, panic.RemainingTurns);

        // Try applying shield on enemy with Panic: Shield should be blocked!
        var shieldEffect = new BattleSkillEffect
        {
            EffectTypeCode = BattleCodes.Shield,
            TargetTypeCode = BattleCodes.EnemySingle,
            BaseValue = 200m,
            DurationTurns = 2
        };
        var shieldHandler = new ShieldEffectHandler();
        var shieldEvents = shieldHandler.Apply(new BattleEffectContext
        {
            Actor = enemy,
            Target = enemy,
            Effect = shieldEffect,
            Skill = ultSkill,
            SelectedTargets = [enemy],
            Combatants = [enemy],
            Random = new Random(1),
            Round = 1,
            Turn = 1
        });

        Assert.Empty(shieldEvents); // Blocked by Panic
        Assert.DoesNotContain(enemy.StatusEffects, x => x.EffectTypeCode == BattleCodes.Shield);
    }

    #endregion

    #region 4. Tiến Dũng Tổng Đài Tests

    [Fact]
    public void TienDung_Basic_Deals_105_Percent_Magic_Damage_And_Can_Silence()
    {
        var tienDung = Hero(1, 0, 2, magicDmg: 200); // 105% of 200 = 210 raw damage
        var enemy = Hero(10, 1, 1, hp: 1000, magicRes: 0);

        var basicSkill = new BattleSkill
        {
            Id = "TIEN_DUNG_TONG_DAI_BASIC",
            Name = "Ping Cuộc Gọi",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Magic,
                    Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 1.05m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Silence,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DurationTurns = 1,
                    ChancePercent = 100m,
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(tienDung, [tienDung, enemy], basicSkill));

        Assert.Equal(790, enemy.Hp); // 1000 - 210 = 790
        var silence = enemy.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.Silence);
        Assert.NotNull(silence);
        Assert.Equal(1, silence.RemainingTurns);
    }

    [Fact]
    public void TienDung_EmergencyConference_Deals_90_Percent_Magic_AoE_And_Rolls_Action_Bar_Change_Independently()
    {
        var tienDung = Hero(1, 0, 2, magicDmg: 200); // 90% of 200 = 180 raw damage
        var enemy1 = Hero(10, 1, 1, hp: 1000, magicRes: 0);
        enemy1.Energy = 50;
        var enemy2 = Hero(11, 1, 2, hp: 1000, magicRes: 0);
        enemy2.Energy = 10;

        var ultSkill = new BattleSkill
        {
            Id = "TIEN_DUNG_EMERGENCY_CONFERENCE",
            Name = "Hội Nghị Khẩn Cấp",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemyAll,
                    DamageSchoolCode = BattleCodes.Magic,
                    Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 0.90m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.ActionBarChange,
                    TargetTypeCode = BattleCodes.EnemyAll,
                    BaseValue = -15m,
                    ChancePercent = 100m, // 100% to verify clamp and delta
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["ACTION_BAR_DELTA"] = IntParam("ACTION_BAR_DELTA", -15),
                        ["ROLL_ONCE_PER_EFFECT"] = BoolParam("ROLL_ONCE_PER_EFFECT", false)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(tienDung, [tienDung, enemy1, enemy2], ultSkill));

        Assert.Equal(820, enemy1.Hp); // 1000 - 180
        Assert.Equal(820, enemy2.Hp);
        Assert.Equal(35, enemy1.Energy); // 50 - 15 = 35
        Assert.Equal(0, enemy2.Energy);  // Clamped at 0 (10 - 15 = -5 => 0)

        Assert.Contains(result.Events, e => e.EventType == BattleCodes.ActionBarChanged && e.TargetId == enemy1.Id && e.Value == -15);
        Assert.Contains(result.Events, e => e.EventType == BattleCodes.ActionBarChanged && e.TargetId == enemy2.Id && e.Value == -10); // actual delta
    }

    #endregion

    #region 5. Quốc Nhân Giả Diện Tests

    [Fact]
    public void QuocNhan_Basic_Deals_120_Percent_Physical_Damage_And_Applies_Bleed()
    {
        var quocNhan = Hero(1, 0, 2, atk: 200); // 120% of 200 = 240 raw damage
        var enemy = Hero(10, 1, 1, hp: 1000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = "QUOC_NHAN_GIA_DIEN_BASIC",
            Name = "Vết Cắt Ngụy Trang",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.20m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Bleed,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DurationTurns = 2,
                    ChancePercent = 100m,
                    Scalings = [new BattleEffectScaling("ATK", 0.30m)], // 30% of 200 = 60
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true),
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(quocNhan, [quocNhan, enemy], basicSkill));

        Assert.Equal(760, enemy.Hp); // 1000 - 240 = 760
        var bleed = enemy.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.Bleed);
        Assert.NotNull(bleed);
        Assert.Equal(2, bleed.RemainingTurns);
        Assert.Equal(60m, bleed.Value);

        // Turn start tick should damage enemy for 60
        var turnStartHandler = (ITurnStartEffectHandler)_effects.GetRequired(BattleCodes.Bleed);
        var tickEvents = turnStartHandler.OnTurnStart(bleed, enemy, 1, 2, [quocNhan, enemy]);

        Assert.Equal(700, enemy.Hp); // 760 - 60 = 700
        Assert.Contains(tickEvents, e => e.EventType == BattleCodes.BleedDamage && e.Value == 60 && e.RemainingTurns == 1);
    }

    [Fact]
    public void QuocNhan_NightPhantoms_Hits_All_Back_Row_Enemies_And_Buffs_Caster_Crit_15_Percent()
    {
        var quocNhan = Hero(1, 0, 2, atk: 200); // 125% of 200 = 250 raw damage
        var frontEnemy = Hero(10, 1, 1, hp: 1000, def: 0); // Pos 1
        var backEnemy1 = Hero(11, 1, 2, hp: 1000, def: 0); // Pos 2
        var backEnemy2 = Hero(12, 1, 4, hp: 1000, def: 0); // Pos 4

        var ultSkill = new BattleSkill
        {
            Id = "QUOC_NHAN_NIGHT_PHANTOMS",
            Name = "Dạ Hành Phân Ảnh",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemyBackRow,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.25m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatBuff,
                    TargetTypeCode = BattleCodes.Self,
                    BaseValue = 15m,
                    DurationTurns = 2,
                    StatModifiers = [new BattleStatModifier("CRIT", "PERCENT", 15m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(quocNhan, [quocNhan, frontEnemy, backEnemy1, backEnemy2], ultSkill));

        Assert.Equal(1000, frontEnemy.Hp); // Front row untouched
        Assert.Equal(750, backEnemy1.Hp);  // 1000 - 250 = 750
        Assert.Equal(750, backEnemy2.Hp);  // 1000 - 250 = 750

        var critBuff = quocNhan.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.StatBuff);
        Assert.NotNull(critBuff);
        Assert.Equal(2, critBuff.RemainingTurns);
    }

    #endregion

    #region 6. Cậu Vàng Mặt Lạnh Tests

    [Fact]
    public void CauVang_Basic_Deals_100_Percent_Damage_And_Buffs_Caster_Def_15_Percent()
    {
        var cauVang = Hero(1, 0, 1, atk: 150); // 100% of 150 = 150
        var enemy = Hero(10, 1, 1, hp: 1000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = "CAU_VANG_MAT_LANH_BASIC",
            Name = "Ngồi Im Phán Xét",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.00m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = BoolParam("CAN_CRIT", false)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.StatBuff,
                    TargetTypeCode = BattleCodes.Self,
                    BaseValue = 15m,
                    DurationTurns = 2,
                    StatModifiers = [new BattleStatModifier("DEF", "PERCENT", 15m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(cauVang, [cauVang, enemy], basicSkill));

        Assert.Equal(850, enemy.Hp);
        var defBuff = cauVang.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.StatBuff);
        Assert.NotNull(defBuff);
        Assert.Equal(2, defBuff.RemainingTurns);
    }

    [Fact]
    public void CauVang_CalmGuard_Shields_All_Allies_For_12_Percent_Max_Hp_And_Gives_Damage_Reduction()
    {
        var cauVang = Hero(1, 0, 1, hp: 2000); // 12% of 2000 = 240 shield
        var ally = Hero(2, 0, 2, hp: 1000);

        var ultSkill = new BattleSkill
        {
            Id = "CAU_VANG_CALM_GUARD",
            Name = "Bình Thản Che Chở",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Shield,
                    TargetTypeCode = BattleCodes.AllyAll,
                    DurationTurns = 2,
                    Scalings = [new BattleEffectScaling("HP", 0.12m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                },
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.DamageReduction,
                    TargetTypeCode = BattleCodes.AllyAll,
                    BaseValue = 12m,
                    DurationTurns = 2,
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["REFRESH_ON_REAPPLY"] = BoolParam("REFRESH_ON_REAPPLY", true)
                    }
                }
            ]
        };

        var result = _defaultHandler.Execute(Context(cauVang, [cauVang, ally], ultSkill));

        var allyShield = ally.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.Shield);
        Assert.NotNull(allyShield);
        Assert.Equal(240, allyShield.ShieldRemaining);
        Assert.Equal(2, allyShield.RemainingTurns);

        var allyReduction = ally.StatusEffects.FirstOrDefault(x => x.EffectTypeCode == BattleCodes.DamageReduction);
        Assert.NotNull(allyReduction);
        Assert.Equal(12m, allyReduction.Value);
        Assert.Equal(2, allyReduction.RemainingTurns);

        // Reapplying does not stack shield infinitely, it refreshes/replaces
        _defaultHandler.Execute(Context(cauVang, [cauVang, ally], ultSkill));
        Assert.Single(ally.StatusEffects, x => x.EffectTypeCode == BattleCodes.Shield);
        Assert.Equal(240, ally.StatusEffects.First(x => x.EffectTypeCode == BattleCodes.Shield).ShieldRemaining);
    }

    #endregion
}
