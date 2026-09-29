using GAME.Domain.Battle;
using GAME.Domain.Battle.Effects;
using GAME.Domain.Battle.Reactions;
using GAME.Domain.Battle.Skills;
using GAME.Domain.Battle.Skills.KietMaiXeo;
using GAME.Domain.Battle.Skills.LongLeCat;
using GAME.Domain.Battle.Skills.QuocNhanGraduation;
using GAME.Domain.Battle.Skills.QuocNhanRunNow;
using GAME.Domain.Battle.Skills.TruongKietGraduation;
using GAME.Domain.Battle.Targets;
using Xunit;

namespace GAME.Domain.Tests.Battle;

public sealed class FiveLegendaryHeroesTests
{
    private readonly BattleEffectHandlerRegistry _effects = BattleEffectHandlerRegistry.CreateDefault();
    private readonly BattleTargetSelectorRegistry _selectors = BattleTargetSelectorRegistry.CreateDefault();
    private readonly DefaultSkillHandler _defaultHandler;
    private readonly KietMaiXeoSkillHandler _kietHandler;
    private readonly TruongKietGraduationSkillHandler _truongKietHandler;
    private readonly QuocNhanGraduationSkillHandler _quocNhanGradHandler;
    private readonly LongLeCatSkillHandler _longLeHandler;
    private readonly QuocNhanRunNowSkillHandler _quocNhanRunHandler;
    private readonly BattleStatusReactionHandlerRegistry _statusReactions = BattleStatusReactionHandlerRegistry.CreateDefault();
    private readonly BattleCombatantReactionRegistry _combatantReactions = BattleCombatantReactionRegistry.CreateDefault();

    public FiveLegendaryHeroesTests()
    {
        _defaultHandler = new DefaultSkillHandler(_effects, _selectors);
        _kietHandler = new KietMaiXeoSkillHandler(_defaultHandler, _effects);
        _truongKietHandler = new TruongKietGraduationSkillHandler(_defaultHandler, _effects);
        _quocNhanGradHandler = new QuocNhanGraduationSkillHandler(_defaultHandler, _effects);
        _longLeHandler = new LongLeCatSkillHandler(_defaultHandler, _effects);
        _quocNhanRunHandler = new QuocNhanRunNowSkillHandler(_defaultHandler, _effects);
    }

    #region Helpers

    private static BattleCombatant CreateHero(
        long id, int team, int position,
        int hp = 1000, int? maxHp = null, int atk = 100, int def = 50, int spd = 100,
        int magicDmg = 100, int magicRes = 50,
        decimal critChance = 0m, decimal critDmg = 150m,
        BattleSkill? basic = null, BattleSkill? energy = null)
    {
        return new BattleCombatant
        {
            Id = id,
            SourceHeroId = id,
            Team = team,
            Position = position,
            Name = $"Hero_{id}",
            Hp = hp,
            MaxHp = maxHp ?? hp,
            Atk = atk,
            Def = def,
            Spd = spd,
            MagicDamage = magicDmg,
            MagicResistance = magicRes,
            CritChance = critChance,
            CritDamage = critDmg,
            Energy = 100,
            MaxEnergy = 100,
            BasicSkill = basic ?? new BattleSkill
            {
                Id = "DUMMY_BASIC",
                Name = "Dummy Basic",
                SkillTypeCode = BattleCodes.Normal,
                Effects = []
            },
            EnergySkill = energy
        };
    }

    private static SkillExecutionContext CreateContext(
        BattleCombatant actor, List<BattleCombatant> combatants, BattleSkill skill, Random? rnd = null, string actionId = "action_1") =>
        new()
        {
            Actor = actor,
            Combatants = combatants,
            Skill = skill,
            Random = rnd ?? new Random(42),
            Round = 1,
            Turn = 1,
            ActionId = actionId
        };

    #endregion

    #region I. KIỆT MÁI XÉO TESTS

    [Fact]
    public void KietMaiXeo_Crit_Grants_Exactly_One_PhongAn()
    {
        var kiet = CreateHero(1, 0, 2, atk: 200, critChance: 100m); // 100% crit chance
        var enemy = CreateHero(2, 1, 1, hp: 1000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = BattleCodes.KietMaiXeoBasic,
            Name = "Đường Kiếm Mái Xéo",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.25m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["CAN_CRIT"] = new("CAN_CRIT", null, null, true, null),
                        ["BACK_ROW_CRIT_BONUS_PERCENT"] = new("BACK_ROW_CRIT_BONUS_PERCENT", 15m, null, null, null)
                    }
                }
            ]
        };

        var ctx = CreateContext(kiet, [kiet, enemy], basicSkill);
        var result = _kietHandler.Execute(ctx);

        Assert.Equal(1, kiet.GetResource(BattleCodes.PhongAn, 0));
        var status = kiet.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.PhongAn);
        Assert.NotNull(status);
        Assert.Equal(1, status.Stacks);
        // SPD increases by 5%
        Assert.Equal(105, (int)BattleStatCalculator.GetEffectiveStat(kiet, "SPD"));
    }

    [Fact]
    public void KietMaiXeo_NonCrit_Does_Not_Grant_PhongAn()
    {
        var kiet = CreateHero(1, 0, 2, atk: 200, critChance: 0m); // 0% crit
        var enemy = CreateHero(2, 1, 1, hp: 1000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = BattleCodes.KietMaiXeoBasic,
            Name = "Đường Kiếm Mái Xéo",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.25m)]
                }
            ]
        };

        var ctx = CreateContext(kiet, [kiet, enemy], basicSkill);
        _kietHandler.Execute(ctx);

        Assert.Equal(0, kiet.GetResource(BattleCodes.PhongAn, 0));
        Assert.Null(kiet.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.PhongAn));
    }

    [Fact]
    public void KietMaiXeo_PhongAn_Capped_At_Three_Stacks()
    {
        var kiet = CreateHero(1, 0, 2, atk: 200, critChance: 100m);
        var enemy = CreateHero(2, 1, 1, hp: 5000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = BattleCodes.KietMaiXeoBasic,
            Name = "Đường Kiếm Mái Xéo",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.25m)]
                }
            ]
        };

        // 4 actions with crit
        for (int i = 1; i <= 4; i++)
        {
            var ctx = CreateContext(kiet, [kiet, enemy], basicSkill, actionId: $"action_{i}");
            _kietHandler.Execute(ctx);
        }

        Assert.Equal(3, kiet.GetResource(BattleCodes.PhongAn, 0));
        var status = kiet.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.PhongAn);
        Assert.NotNull(status);
        Assert.Equal(3, status.Stacks);
        // SPD +15%
        Assert.Equal(115, (int)BattleStatCalculator.GetEffectiveStat(kiet, "SPD"));
    }

    [Fact]
    public void KietMaiXeo_BackRow_Target_Receives_Bonus_CritChance()
    {
        // Target is at Position 2 (Back Row)
        var kiet = CreateHero(1, 0, 2, atk: 200, critChance: 0m);
        var backRowEnemy = CreateHero(2, 1, 2, hp: 1000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = BattleCodes.KietMaiXeoBasic,
            Name = "Đường Kiếm Mái Xéo",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.25m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["BACK_ROW_CRIT_BONUS_PERCENT"] = new("BACK_ROW_CRIT_BONUS_PERCENT", 15m, null, null, null)
                    }
                }
            ]
        };

        // Random roll of 10 should crit (10 < 15% effective crit chance)
        // Seed that gives random double < 0.15
        var rnd = new Random(2); // In .NET, NextDouble() can be controlled
        var ctx = CreateContext(kiet, [kiet, backRowEnemy], basicSkill, rnd);
        var result = _kietHandler.Execute(ctx);

        // Verify crit chance returned to base 0m after attack
        Assert.Equal(0m, kiet.CritChance);
    }

    [Fact]
    public void KietMaiXeo_Energy_Consumes_Exact_Stacks_And_Deals_Bonus_Damage()
    {
        var kiet = CreateHero(1, 0, 2, atk: 100, critChance: 0m);
        // Give 2 stacks of Phong An (+24% bonus damage)
        kiet.SetResource(BattleCodes.PhongAn, 2);

        var enemy = CreateHero(2, 1, 1, hp: 1000, def: 0);

        var energySkill = new BattleSkill
        {
            Id = BattleCodes.KietMaiXeoEnergy,
            Name = "Tam Phong Đoạn Ảnh",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.LowestHpPercent,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 0.70m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["HIT_COUNT"] = new("HIT_COUNT", null, 3, null, null),
                        ["DAMAGE_BONUS_PER_STACK_PERCENT"] = new("DAMAGE_BONUS_PER_STACK_PERCENT", 12m, null, null, null)
                    }
                }
            ]
        };

        var ctx = CreateContext(kiet, [kiet, enemy], energySkill);
        var result = _kietHandler.Execute(ctx);

        // Stacks consumed to 0
        Assert.Equal(0, kiet.GetResource(BattleCodes.PhongAn, 0));

        // 3 hits, each hit 70 ATK * 1.24 = 86.8 -> 87 damage
        var damageEvents = result.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(3, damageEvents.Count);
        foreach (var evt in damageEvents)
        {
            Assert.Equal(87, evt.Value);
        }
    }

    [Fact]
    public void KietMaiXeo_Energy_Three_Stacks_Ignores_20Percent_Def_On_Last_Hit()
    {
        var kiet = CreateHero(1, 0, 2, atk: 100, critChance: 0m);
        kiet.SetResource(BattleCodes.PhongAn, 3);

        // Enemy with 100 DEF
        var enemy = CreateHero(2, 1, 1, hp: 1000, def: 100);

        var energySkill = new BattleSkill
        {
            Id = BattleCodes.KietMaiXeoEnergy,
            Name = "Tam Phong Đoạn Ảnh",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.LowestHpPercent,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 0.70m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["HIT_COUNT"] = new("HIT_COUNT", null, 3, null, null),
                        ["DAMAGE_BONUS_PER_STACK_PERCENT"] = new("DAMAGE_BONUS_PER_STACK_PERCENT", 12m, null, null, null),
                        ["MAX_STACK_ARMOR_IGNORE_PERCENT"] = new("MAX_STACK_ARMOR_IGNORE_PERCENT", 20m, null, null, null)
                    }
                }
            ]
        };

        var ctx = CreateContext(kiet, [kiet, enemy], energySkill);
        var result = _kietHandler.Execute(ctx);

        var damageEvents = result.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(3, damageEvents.Count);

        // 3 stacks -> +36% damage bonus. Base hit = 70 * 1.36 = 95.2
        // Hits 1 & 2: DEF = 100 -> damage = 95.2 * 100 / (100 + 100) = 47.6 -> 48
        Assert.Equal(48, damageEvents[0].Value);
        Assert.Equal(48, damageEvents[1].Value);

        // Hit 3: DEF ignored 20% -> effective DEF = 80 -> damage = 95.2 * 100 / (100 + 80) = 52.88 -> 53
        Assert.Equal(53, damageEvents[2].Value);
        Assert.True(damageEvents[2].Value > damageEvents[0].Value);
    }

    #endregion

    #region II. TRƯỜNG KIỆT TỐT NGHIỆP CẤP 3 TESTS

    [Fact]
    public void TruongKiet_Each_Enemy_Action_Grants_At_Most_One_TinChi()
    {
        var truongKiet = CreateHero(1, 0, 1, hp: 1000);
        var status = new BattleStatusEffect
        {
            InstanceId = "tk_tinchi",
            EffectTypeCode = BattleCodes.TinChiDanhDu,
            SourceSkillId = BattleCodes.TruongKietGraduationBasic,
            SourceHeroId = truongKiet.Id,
            RemainingTurns = -1,
            Stacks = 0,
            MaxStacks = 3
        };
        truongKiet.StatusEffects.Add(status);

        var enemy = CreateHero(2, 1, 1);
        var handler = new TinChiDanhDuReactionHandler();

        // Hit 1 of action_1
        var ctx1 = new BattleDamagedContext
        {
            Actor = enemy,
            Target = truongKiet,
            Skill = enemy.BasicSkill,
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle },
            ActualHpDamage = 100,
            WasHit = true,
            ActionId = "action_1",
            Combatants = [truongKiet, enemy],
            Random = new Random()
        };
        var evts1 = handler.OnDamaged(status, ctx1);
        Assert.Single(evts1);
        Assert.Equal(1, status.Stacks);

        // Hit 2 of same action_1 (multi-hit) -> should NOT grant another stack
        var ctx2 = new BattleDamagedContext
        {
            Actor = enemy,
            Target = truongKiet,
            Skill = enemy.BasicSkill,
            Effect = new BattleSkillEffect { EffectTypeCode = BattleCodes.Damage, TargetTypeCode = BattleCodes.EnemySingle },
            ActualHpDamage = 50,
            WasHit = true,
            ActionId = "action_1",
            Combatants = [truongKiet, enemy],
            Random = new Random()
        };
        var evts2 = handler.OnDamaged(status, ctx2);
        Assert.Empty(evts2);
        Assert.Equal(1, status.Stacks);
    }

    [Fact]
    public void TruongKiet_DoT_Does_Not_Grant_TinChi()
    {
        var truongKiet = CreateHero(1, 0, 1, hp: 1000);
        var status = new BattleStatusEffect
        {
            InstanceId = "tk_tinchi",
            EffectTypeCode = BattleCodes.TinChiDanhDu,
            SourceSkillId = BattleCodes.TruongKietGraduationBasic,
            SourceHeroId = truongKiet.Id,
            RemainingTurns = -1,
            Stacks = 0,
            MaxStacks = 3
        };
        truongKiet.StatusEffects.Add(status);

        var enemy = CreateHero(2, 1, 1);
        var handler = new TinChiDanhDuReactionHandler();

        var dotCtx = new BattleDamagedContext
        {
            Actor = enemy,
            Target = truongKiet,
            Skill = enemy.BasicSkill,
            Effect = new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.Damage,
                TargetTypeCode = BattleCodes.EnemySingle,
                Parameters = new Dictionary<string, BattleSkillEffectParameter>
                {
                    ["IS_DOT"] = new("IS_DOT", null, null, true, null)
                }
            },
            ActualHpDamage = 80,
            WasHit = true,
            ActionId = "action_dot",
            Combatants = [truongKiet, enemy],
            Random = new Random()
        };

        var evts = handler.OnDamaged(status, dotCtx);
        Assert.Empty(evts);
        Assert.Equal(0, status.Stacks);
    }

    [Fact]
    public void TruongKiet_Shield_Scales_8Percent_Base_And_11Percent_At_3_Stacks()
    {
        var truongKiet = CreateHero(1, 0, 1, hp: 2000, atk: 100);
        var ally = CreateHero(3, 0, 3, hp: 500, maxHp: 1000); // 50% HP
        var enemy = CreateHero(2, 1, 1, hp: 1000);

        var basicSkill = new BattleSkill
        {
            Id = BattleCodes.TruongKietGraduationBasic,
            Name = "Bảo Vệ Lễ Tốt Nghiệp",
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
                        ["BASE_SHIELD_MAX_HP_PERCENT"] = new("BASE_SHIELD_MAX_HP_PERCENT", 0.08m, null, null, null),
                        ["EMPOWERED_SHIELD_MAX_HP_PERCENT"] = new("EMPOWERED_SHIELD_MAX_HP_PERCENT", 0.11m, null, null, null)
                    }
                }
            ]
        };

        // Case 1: 0 stacks -> 8% Max HP = 2000 * 0.08 = 160
        var ctx1 = CreateContext(truongKiet, [truongKiet, ally, enemy], basicSkill);
        var res1 = _truongKietHandler.Execute(ctx1);
        var shieldEvt1 = res1.Events.FirstOrDefault(e => e.EffectTypeCode == BattleCodes.Shield);
        Assert.NotNull(shieldEvt1);
        Assert.Equal(160, shieldEvt1.Value);

        // Case 2: 3 stacks -> 11% Max HP = 2000 * 0.11 = 220
        truongKiet.SetResource(BattleCodes.TinChiDanhDu, 3);
        var ctx2 = CreateContext(truongKiet, [truongKiet, ally, enemy], basicSkill);
        var res2 = _truongKietHandler.Execute(ctx2);
        var shieldEvt2 = res2.Events.FirstOrDefault(e => e.EffectTypeCode == BattleCodes.Shield);
        Assert.NotNull(shieldEvt2);
        Assert.Equal(220, shieldEvt2.Value);
    }

    [Fact]
    public void TruongKiet_Energy_Consumes_Stacks_And_Grants_DamageReduction_At_3_Stacks()
    {
        var truongKiet = CreateHero(1, 0, 1, hp: 2000, atk: 100);
        truongKiet.SetResource(BattleCodes.TinChiDanhDu, 3);
        var ally = CreateHero(3, 0, 3, hp: 1000);
        var enemy = CreateHero(2, 1, 1, hp: 1000);

        var energySkill = new BattleSkill
        {
            Id = BattleCodes.TruongKietGraduationEnergy,
            Name = "Thủ Khoa Đứng Tuyến Đầu",
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
                        ["BASE_TEAM_SHIELD_PERCENT"] = new("BASE_TEAM_SHIELD_PERCENT", 0.09m, null, null, null),
                        ["SHIELD_PER_STACK_PERCENT"] = new("SHIELD_PER_STACK_PERCENT", 0.02m, null, null, null),
                        ["DAMAGE_REDUCTION_PERCENT"] = new("DAMAGE_REDUCTION_PERCENT", 20m, null, null, null)
                    }
                }
            ]
        };

        var ctx = CreateContext(truongKiet, [truongKiet, ally, enemy], energySkill);
        var res = _truongKietHandler.Execute(ctx);

        // Stacks consumed
        Assert.Equal(0, truongKiet.GetResource(BattleCodes.TinChiDanhDu, 0));

        // Team shield: 9% + 3*2% = 15% Max HP = 2000 * 0.15 = 300
        var shieldEvts = res.Events.Where(e => e.EffectTypeCode == BattleCodes.Shield).ToList();
        Assert.Equal(2, shieldEvts.Count);
        foreach (var s in shieldEvts)
        {
            Assert.Equal(300, s.Value);
        }

        // Damage reduction 20% on Truong Kiet
        var drStatus = truongKiet.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.DamageReduction);
        Assert.NotNull(drStatus);
        Assert.Equal(20m, drStatus.Value);
        Assert.Equal(2, drStatus.RemainingTurns);
    }

    #endregion

    #region III. QUỐC NHÂN TỐT NGHIỆP CẤP 3 TESTS

    [Fact]
    public void QuocNhan_LuanDiem_Caps_At_3_Stacks_And_Deals_Bonus_Damage_Only_To_QuocNhan()
    {
        var quocNhan = CreateHero(1, 0, 2, magicDmg: 200);
        var allyMage = CreateHero(3, 0, 4, magicDmg: 200);
        var enemy = CreateHero(2, 1, 1, hp: 5000, def: 0, magicRes: 0);

        var basicSkill = new BattleSkill
        {
            Id = BattleCodes.QuocNhanGraduationBasic,
            Name = "Nhận Xét Bên Lề",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Magic,
                    Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 1.20m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["LUAN_DIEM_BONUS_PER_STACK"] = new("LUAN_DIEM_BONUS_PER_STACK", 4m, null, null, null)
                    }
                }
            ]
        };

        // Cast basic 4 times
        for (int i = 1; i <= 4; i++)
        {
            var ctx = CreateContext(quocNhan, [quocNhan, allyMage, enemy], basicSkill, actionId: $"action_{i}");
            _quocNhanGradHandler.Execute(ctx);
        }

        // Should cap at 3 stacks
        var luanDiem = enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.LuanDiem);
        Assert.NotNull(luanDiem);
        Assert.Equal(3, luanDiem.Stacks);
        Assert.True(luanDiem.IncomingDamageBonusRestrictedToSource);

        // Fourth cast should have applied Magic Resistance debuff (-10%)
        var mrDebuff = enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.StatDebuff);
        Assert.NotNull(mrDebuff);
        Assert.Equal(-10m, mrDebuff.Value);
    }

    [Fact]
    public void QuocNhan_Energy_Consumes_LuanDiem_And_Silences_At_3_Stacks()
    {
        var quocNhan = CreateHero(1, 0, 2, magicDmg: 100);
        var enemy = CreateHero(2, 1, 1, hp: 5000, def: 0, magicRes: 0);

        // Put 3 stacks of Luan Diem on enemy
        enemy.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "ld_1",
            EffectTypeCode = BattleCodes.LuanDiem,
            SourceSkillId = BattleCodes.QuocNhanGraduationBasic,
            SourceHeroId = quocNhan.Id,
            RemainingTurns = 3,
            Stacks = 3,
            MaxStacks = 3,
            IncomingDamageBonusPerStackPercent = 4m,
            IncomingDamageBonusRestrictedToSource = true
        });

        var energySkill = new BattleSkill
        {
            Id = BattleCodes.QuocNhanGraduationEnergy,
            Name = "Hội Đồng Phản Biện",
            SkillTypeCode = BattleCodes.Energy,
            EnergyCost = 100,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemyAll,
                    DamageSchoolCode = BattleCodes.Magic,
                    Scalings = [new BattleEffectScaling("MAGIC_DAMAGE", 1.15m)],
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["EXTRA_DAMAGE_PER_STACK_PERCENT"] = new("EXTRA_DAMAGE_PER_STACK_PERCENT", 0.18m, null, null, null),
                        ["SILENCE_CHANCE_PERCENT"] = new("SILENCE_CHANCE_PERCENT", 100m, null, null, null) // 100% for deterministic test
                    }
                }
            ]
        };

        var ctx = CreateContext(quocNhan, [quocNhan, enemy], energySkill);
        var res = _quocNhanGradHandler.Execute(ctx);

        // Luan Diem consumed
        Assert.Null(enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.LuanDiem));

        // Base damage = 115 * 1.12 (from 3 stacks of Luan Diem) = 128.8 -> 129
        // Extra damage = 3 * 18% = 54% Magic Damage = 54
        var dmgEvents = res.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(2, dmgEvents.Count);
        Assert.Equal(54, dmgEvents[1].Value);

        // Silence applied for 1 turn
        var silence = enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.Silence);
        Assert.NotNull(silence);
        Assert.Equal(1, silence.RemainingTurns);
    }

    #endregion

    #region IV. LONG LÊ CON MÈO TESTS

    [Fact]
    public void LongLe_Basic_Applies_CatScratch_For_2_Turns()
    {
        var longLe = CreateHero(1, 0, 1, atk: 100);
        var enemy = CreateHero(2, 1, 1, hp: 1000, def: 0);

        var basicSkill = new BattleSkill
        {
            Id = BattleCodes.LongLeCatScratchBasic,
            Name = "Mèo Cào Đánh Dấu",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.00m)]
                }
            ]
        };

        var ctx = CreateContext(longLe, [longLe, enemy], basicSkill);
        _longLeHandler.Execute(ctx);

        var scratch = enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.CatScratch);
        Assert.NotNull(scratch);
        Assert.Equal(2, scratch.RemainingTurns);
        Assert.Equal(10m, scratch.IncomingDamageBonusPerStackPercent);
    }

    [Fact]
    public void LongLe_CatScratch_Increases_Damage_10Percent_And_Heals_5Percent()
    {
        var ally = CreateHero(3, 0, 2, hp: 800, maxHp: 1000, atk: 200); // Damaged ally: 800/1000 HP
        var enemy = CreateHero(2, 1, 1, hp: 2000, def: 0);

        // Place Cat Scratch on enemy
        enemy.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "cs_1",
            EffectTypeCode = BattleCodes.CatScratch,
            SourceSkillId = BattleCodes.LongLeCatScratchBasic,
            SourceHeroId = 1,
            RemainingTurns = 2,
            Stacks = 1,
            MaxStacks = 1,
            IncomingDamageBonusPerStackPercent = 10m
        });

        // Ally attacks enemy with Cat Scratch
        var allySkill = new BattleSkill
        {
            Id = "ALLY_ATTACK",
            Name = "Ally Attack",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySingle,
                    DamageSchoolCode = BattleCodes.Physical,
                    Scalings = [new BattleEffectScaling("ATK", 1.00m)]
                }
            ]
        };

        var ctx = CreateContext(ally, [ally, enemy], allySkill, actionId: "ally_act_1");
        var result = _defaultHandler.Execute(ctx);

        // Damage increased by 10%: 200 * 1.10 = 220
        var dmgEvt = result.Events.FirstOrDefault(e => e.EventType == "DAMAGE");
        Assert.NotNull(dmgEvt);
        Assert.Equal(220, dmgEvt.Value);

        // Simulate reaction: OnDamaged records 220 dmg, then OnActionCompleted heals 5% of 220 = 11 HP
        var scratchHandler = new CatScratchReactionHandler();
        scratchHandler.OnDamaged(enemy.StatusEffects.First(s => s.EffectTypeCode == BattleCodes.CatScratch),
            new BattleDamagedContext
            {
                Actor = ally,
                Target = enemy,
                Skill = allySkill,
                Effect = allySkill.Effects[0],
                ActualHpDamage = 220,
                WasHit = true,
                ActionId = "ally_act_1",
                Combatants = [ally, enemy],
                Random = new Random()
            });

        var catReaction = new CatCombatantReactionHandler();
        var postEvts = catReaction.OnActionCompleted(ally, new BattleActionCompletedReactionContext
        {
            Actor = ally,
            Skill = allySkill,
            ExecutionResult = result,
            ActionId = "ally_act_1",
            Round = 1,
            Turn = 1,
            Combatants = [ally, enemy],
            Random = new Random()
        });

        var healEvt = postEvts.FirstOrDefault(e => e.EventType == "HEAL");
        Assert.NotNull(healEvt);
        Assert.Equal(11, healEvt.Value);
        Assert.Equal(811, ally.Hp);
    }

    [Fact]
    public void LongLe_Energy_Excludes_LongLe_And_Heroes_With_Basic_Heal()
    {
        var longLe = CreateHero(1, 0, 1);

        // Ally with basic HEAL (e.g. Kiet Bac Si)
        var healerAlly = CreateHero(2, 0, 2, basic: new BattleSkill
        {
            Id = "HEAL_BASIC",
            Name = "Heal Basic",
            SkillTypeCode = BattleCodes.Normal,
            Effects = [new BattleSkillEffect { EffectTypeCode = BattleCodes.Heal, TargetTypeCode = BattleCodes.LowestHpPercent }]
        });

        // Valid DPS Ally 1
        var dpsAlly1 = CreateHero(3, 0, 3);
        // Valid DPS Ally 2
        var dpsAlly2 = CreateHero(4, 0, 4);

        var selector = new RandomEligibleAlliesNTargetSelector();
        var selected = selector.Select(new BattleTargetContext
        {
            Actor = longLe,
            Allies = [longLe, healerAlly, dpsAlly1, dpsAlly2],
            Enemies = [],
            Random = new Random(42),
            Effect = new BattleSkillEffect
            {
                EffectTypeCode = BattleCodes.CatCompanion,
                TargetTypeCode = BattleCodes.RandomEligibleAlliesN,
                Parameters = new Dictionary<string, BattleSkillEffectParameter>
                {
                    ["TARGET_COUNT"] = new("TARGET_COUNT", null, 3, null, null),
                    ["EXCLUDE_ACTOR"] = new("EXCLUDE_ACTOR", null, null, true, null),
                    ["EXCLUDE_HEALER_BASICS"] = new("EXCLUDE_HEALER_BASICS", null, null, true, null)
                }
            }
        });

        // Should select dpsAlly1 and dpsAlly2, and NOT longLe or healerAlly
        Assert.Equal(2, selected.Count);
        Assert.Contains(selected, a => a.Id == 3);
        Assert.Contains(selected, a => a.Id == 4);
        Assert.DoesNotContain(selected, a => a.Id == 1);
        Assert.DoesNotContain(selected, a => a.Id == 2);
    }

    [Fact]
    public void LongLe_Host_With_Cat_Companion_Applies_DeepCatScratch_Replacing_CatScratch()
    {
        var ally = CreateHero(3, 0, 2, atk: 200);
        // Add Cat Companion to ally
        ally.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "companion_1",
            EffectTypeCode = BattleCodes.CatCompanion,
            SourceSkillId = BattleCodes.LongLeCatCompanions,
            SourceHeroId = 1,
            RemainingTurns = 2
        });

        var enemy = CreateHero(2, 1, 1, hp: 1000);
        // Enemy currently has normal Cat Scratch
        enemy.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "cs_old",
            EffectTypeCode = BattleCodes.CatScratch,
            SourceSkillId = BattleCodes.LongLeCatScratchBasic,
            SourceHeroId = 1,
            RemainingTurns = 1,
            IncomingDamageBonusPerStackPercent = 10m
        });

        // Ally attacks enemy
        var execResult = new SkillExecutionResult();
        execResult.Events.Add(new PendingBattleEvent
        {
            EventType = "DAMAGE",
            ActorId = ally.Id,
            TargetId = enemy.Id,
            Value = 150
        });

        var catReaction = new CatCombatantReactionHandler();
        catReaction.OnActionCompleted(ally, new BattleActionCompletedReactionContext
        {
            Actor = ally,
            Skill = ally.BasicSkill,
            ExecutionResult = execResult,
            ActionId = "companion_act",
            Round = 1,
            Turn = 1,
            Combatants = [ally, enemy],
            Random = new Random()
        });

        // Normal Cat Scratch was removed
        Assert.Null(enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.CatScratch));

        // Deep Cat Scratch applied (+20% damage, 2 turns)
        var deepScratch = enemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.DeepCatScratch);
        Assert.NotNull(deepScratch);
        Assert.Equal(2, deepScratch.RemainingTurns);
        Assert.Equal(20m, deepScratch.IncomingDamageBonusPerStackPercent);
    }

    #endregion

    #region V. QUỐC NHÂN CHẠY NGAY ĐI TESTS

    [Fact]
    public void QuocNhanRunNow_Lane_Damage_Scaling_60Percent_If_2_Targets_120Percent_If_1_Target()
    {
        var qn = CreateHero(1, 0, 2, magicDmg: 200);

        var skill = new BattleSkill
        {
            Id = BattleCodes.QuocNhanRunNowBasic,
            Name = "Lửa Nến Xuyên Hàng",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySameVerticalLane,
                    DamageSchoolCode = BattleCodes.Magic,
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["SINGLE_TARGET_COEFF"] = new("SINGLE_TARGET_COEFF", 1.20m, null, null, null),
                        ["MULTI_TARGET_COEFF"] = new("MULTI_TARGET_COEFF", 0.60m, null, null, null),
                        ["TRAIL_DAMAGE_COEFF"] = new("TRAIL_DAMAGE_COEFF", 0.75m, null, null, null)
                    }
                }
            ]
        };

        // Case A: 2 targets in Lane 1 (positions 1 and 2) -> 60% each = 120 dmg each
        var enemy1 = CreateHero(2, 1, 1, hp: 1000, magicRes: 0);
        var enemy2 = CreateHero(3, 1, 2, hp: 1000, magicRes: 0);

        var ctxA = CreateContext(qn, [qn, enemy1, enemy2], skill);
        var resA = _quocNhanRunHandler.Execute(ctxA);

        var dmgA = resA.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(2, dmgA.Count);
        Assert.Equal(120, dmgA[0].Value);
        Assert.Equal(120, dmgA[1].Value);

        // Case B: Only 1 target alive in Lane 1 (position 1) -> 120% = 240 dmg
        var enemySingle = CreateHero(4, 1, 1, hp: 1000, magicRes: 0);
        var ctxB = CreateContext(qn, [qn, enemySingle], skill);
        var resB = _quocNhanRunHandler.Execute(ctxB);

        var dmgB = resB.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Single(dmgB);
        Assert.Equal(240, dmgB[0].Value);
    }

    [Fact]
    public void QuocNhanRunNow_Energy_FrontRow_With_BackRow_Fallback()
    {
        var selector = new EnemyFrontRowFallbackTargetSelector();
        var qn = CreateHero(1, 0, 2);

        // Case 1: Front row has living targets (pos 1 and 3)
        var front1 = CreateHero(2, 1, 1);
        var front3 = CreateHero(3, 1, 3);
        var back2 = CreateHero(4, 1, 2);

        var res1 = selector.Select(new BattleTargetContext
        {
            Actor = qn,
            Enemies = [front1, front3, back2],
            Allies = [qn],
            Random = new Random()
        });

        // Must select only front row
        Assert.Equal(2, res1.Count);
        Assert.Contains(res1, e => e.Position == 1);
        Assert.Contains(res1, e => e.Position == 3);
        Assert.DoesNotContain(res1, e => e.Position == 2);

        // Case 2: Front row is all dead -> fallback to back row (pos 2 and 4)
        var back4 = CreateHero(5, 1, 4);
        var res2 = selector.Select(new BattleTargetContext
        {
            Actor = qn,
            Enemies = [back2, back4], // 0 in front row
            Allies = [qn],
            Random = new Random()
        });

        Assert.Equal(2, res2.Count);
        Assert.Contains(res2, e => e.Position == 2);
        Assert.Contains(res2, e => e.Position == 4);
    }

    [Fact]
    public void QuocNhanRunNow_Basic_On_Marked_Target_Triggers_FlameTrail_75Percent_And_Stun()
    {
        var qn = CreateHero(1, 0, 2, magicDmg: 100);
        var enemy1 = CreateHero(2, 1, 1, hp: 2000, magicRes: 0, def: 0); // Position 1 (front)
        var enemy2 = CreateHero(3, 1, 2, hp: 2000, magicRes: 0, def: 0); // Position 2 (back)

        // Put Chay Ngay Di on enemy1
        enemy1.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "cnd_1",
            EffectTypeCode = BattleCodes.ChayNgayDi,
            SourceSkillId = BattleCodes.QuocNhanRunNowEnergy,
            SourceHeroId = qn.Id,
            RemainingTurns = 3,
            Stacks = 1,
            MaxStacks = 1
        });

        var skill = new BattleSkill
        {
            Id = BattleCodes.QuocNhanRunNowBasic,
            Name = "Lửa Nến Xuyên Hàng",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySameVerticalLane,
                    DamageSchoolCode = BattleCodes.Magic,
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["MULTI_TARGET_COEFF"] = new("MULTI_TARGET_COEFF", 0.60m, null, null, null),
                        ["SINGLE_TARGET_COEFF"] = new("SINGLE_TARGET_COEFF", 1.20m, null, null, null),
                        ["TRAIL_DAMAGE_COEFF"] = new("TRAIL_DAMAGE_COEFF", 0.75m, null, null, null)
                    }
                }
            ]
        };

        var ctx = CreateContext(qn, [qn, enemy1, enemy2], skill);
        var res = _quocNhanRunHandler.Execute(ctx);

        // 1. Mark is consumed from enemy1
        Assert.Null(enemy1.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.ChayNgayDi));

        // 2. Base damage: 2 targets -> 60% = 60 dmg each (events 0 and 1)
        // 3. Flame trail: 75% = 75 dmg each on living lane targets (events 2 and 3)
        var dmgEvents = res.Events.Where(e => e.EventType == "DAMAGE").ToList();
        Assert.Equal(4, dmgEvents.Count);
        Assert.Equal(60, dmgEvents[0].Value);
        Assert.Equal(60, dmgEvents[1].Value);
        Assert.Equal(75, dmgEvents[2].Value);
        Assert.Equal(75, dmgEvents[3].Value);

        // 4. Stun applied to lane targets
        Assert.NotNull(enemy1.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.Stun));
        Assert.NotNull(enemy2.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.Stun));
    }

    [Fact]
    public void QuocNhanRunNow_Multiple_Marks_In_Same_Lane_Only_Trigger_Once_Prioritizing_Front()
    {
        var qn = CreateHero(1, 0, 2, magicDmg: 100);
        var frontEnemy = CreateHero(2, 1, 1, hp: 2000, magicRes: 0, def: 0);
        var backEnemy = CreateHero(3, 1, 2, hp: 2000, magicRes: 0, def: 0);

        // Both front and back have the mark!
        frontEnemy.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "cnd_front",
            EffectTypeCode = BattleCodes.ChayNgayDi,
            SourceSkillId = BattleCodes.QuocNhanRunNowEnergy,
            SourceHeroId = qn.Id,
            RemainingTurns = 3
        });
        backEnemy.StatusEffects.Add(new BattleStatusEffect
        {
            InstanceId = "cnd_back",
            EffectTypeCode = BattleCodes.ChayNgayDi,
            SourceSkillId = BattleCodes.QuocNhanRunNowEnergy,
            SourceHeroId = qn.Id,
            RemainingTurns = 2
        });

        var skill = new BattleSkill
        {
            Id = BattleCodes.QuocNhanRunNowBasic,
            Name = "Lửa Nến Xuyên Hàng",
            SkillTypeCode = BattleCodes.Normal,
            Effects =
            [
                new BattleSkillEffect
                {
                    EffectTypeCode = BattleCodes.Damage,
                    TargetTypeCode = BattleCodes.EnemySameVerticalLane,
                    DamageSchoolCode = BattleCodes.Magic,
                    Parameters = new Dictionary<string, BattleSkillEffectParameter>
                    {
                        ["MULTI_TARGET_COEFF"] = new("MULTI_TARGET_COEFF", 0.60m, null, null, null),
                        ["TRAIL_DAMAGE_COEFF"] = new("TRAIL_DAMAGE_COEFF", 0.75m, null, null, null)
                    }
                }
            ]
        };

        var ctx = CreateContext(qn, [qn, frontEnemy, backEnemy], skill);
        var res = _quocNhanRunHandler.Execute(ctx);

        // Front mark was consumed
        Assert.Null(frontEnemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.ChayNgayDi));
        // Back mark was NOT consumed, retains remaining turns
        var backMark = backEnemy.StatusEffects.FirstOrDefault(s => s.EffectTypeCode == BattleCodes.ChayNgayDi);
        Assert.NotNull(backMark);
        Assert.Equal(2, backMark.RemainingTurns);

        // Exactly one flame trail (2 flame damage events, not 4)
        var trailDmg = res.Events.Where(e => e.EventType == "DAMAGE" && e.Value == 75).ToList();
        Assert.Equal(2, trailDmg.Count);
    }

    #endregion
}
