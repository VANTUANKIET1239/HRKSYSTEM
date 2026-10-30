namespace GAME.Domain.Battle;

public static class BattleCodes
{
    public const string Normal = "NORMAL";
    public const string Energy = "ENERGY";
    public const string Damage = "DAMAGE";
    public const string Heal = "HEAL";
    public const string StatBuff = "STAT_BUFF";
    public const string StatDebuff = "STAT_DEBUFF";
    public const string Stun = "STUN";
    public const string Shield = "SHIELD";
    public const string Mark = "MARK";
    public const string PositionSwap = "POSITION_SWAP";
    public const string Silence = "SILENCE";
    public const string DamageReduction = "DAMAGE_REDUCTION";
    public const string Taunt = "TAUNT";
    public const string DamageReflection = "DAMAGE_REFLECTION";
    public const string Physical = "PHYSICAL";
    public const string Magic = "MAGIC";
    public const string True = "TRUE";
    public const string Self = "SELF";
    public const string AllyRandom = "ALLY_RANDOM";
    public const string AllyAll = "ALLY_ALL";
    public const string AllyRandom2 = "ALLY_RANDOM_2";
    public const string EnemySingle = "ENEMY_SINGLE";
    public const string EnemyAll = "ENEMY_ALL";
    public const string EnemyRandom = "ENEMY_RANDOM";
    public const string EnemyRandom4 = "ENEMY_RANDOM_4";
    public const string EnemyStraightFrontRow = "ENEMY_STRAIGHT_FRONT_ROW";
    public const string EnemyFrontRow = "ENEMY_FRONT_ROW";
    public const string EnemyBackRow = "ENEMY_BACK_ROW";
    public const string EnemySameLaneBackRow = "ENEMY_SAME_LANE_BACK_ROW";
    public const string LowestHpPercent = "LOWEST_HP_PERCENT";
    public const string Bleed = "BLEED";
    public const string Panic = "PANIC";
    public const string ShieldBlock = "SHIELD_BLOCK";
    public const string BleedDamage = "BLEED_DAMAGE";
    public const string BleedDetonate = "BLEED_DETONATE";
    public const string BleedDetonated = "BLEED_DETONATED";
    public const string EnergyChange = "ENERGY_CHANGE";
    public const string EnergyChanged = "ENERGY_CHANGED";
    public const string ActionBarChange = "ACTION_BAR_CHANGE";
    public const string ActionBarChanged = "ACTION_BAR_CHANGED";
    public const string EnemyRandomDistinctN = "ENEMY_RANDOM_DISTINCT_N";
    public const string AllyLowestEnergy = "ALLY_LOWEST_ENERGY";
    public const string StatusRefreshed = "STATUS_REFRESHED";
    public const string StatusStackChanged = "STATUS_STACK_CHANGED";
    public const string StatusRemoved = "STATUS_REMOVED";
    public const string Ricardo = "RICARDO";
    public const string RicardoRageReady = "RICARDO_RAGE_READY";
    public const string RicardoApplied = "RICARDO_APPLIED";
    public const string RicardoConsumed = "RICARDO_CONSUMED";
    public const string ChuanMenBasic = "CHUAN_MEN_BASIC";
    public const string RicardoMilos = "RICARDO_MILOS";
    public const string ThanhThaiAura = "THANH_THAI_AURA";
    public const string ThanhThaiSweep = "THANH_THAI_DISRESPECTFUL_SWEEP";
    public const string ThanhThaiLightning = "THANH_THAI_CRIMSON_BROOM_LIGHTNING";
    public const string Aura = "AURA";
    public const string FullAuraFarming = "FULL_AURA_FARMING";
    public const string LossOfConfidence = "LOSS_OF_CONFIDENCE";
    public const string LossOfConfidenceDetonated = "LOSS_OF_CONFIDENCE_DETONATED";
    public const string ResourceChanged = "RESOURCE_CHANGED";
    public const string AuraGained = "AURA_GAINED";
    public const string AuraConsumed = "AURA_CONSUMED";
    public const string FullAuraActivated = "FULL_AURA_ACTIVATED";
    public const string FullAuraRemoved = "FULL_AURA_REMOVED";
    public const string ThanhThaiEmpoweredCast = "THANH_THAI_EMPOWERED_CAST";
    public const string NghiaPhucPrime = "NGHIA_PHUC_PRIME";
    public const string PrimeShieldWarranty = "PRIME_SHIELD_WARRANTY";
    public const string PrimeFortressCharge = "PRIME_FORTRESS_CHARGE";
    public const string PrimeFortitude = "PRIME_FORTITUDE";
    public const string PrimeGuardian = "PRIME_GUARDIAN";
    public const string PrimePressure = "PRIME_PRESSURE";
    public const string PrimeStagger = "PRIME_STAGGER";
    public const string PrimeBrokenMorale = "PRIME_BROKEN_MORALE";

    // Siba Thiên Thần (Mythic)
    public const string SibaHero = "SIBA_THIEN_THAN";
    public const string SibaAngelGentleWing = "SIBA_ANGEL_GENTLE_WING";
    public const string SibaCelestialProtection = "SIBA_CELESTIAL_PROTECTION";
    public const string SibaEmpoweredCast = "SIBA_EMPOWERED_CAST";
    public const string AllyLowestHpPreferWithoutStatus = "ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS";
    public const string DispelDebuff = "DISPEL_DEBUFF";
    public const string EncouragementOffense = "ENCOURAGEMENT_OFFENSE";
    public const string EncouragementDefense = "ENCOURAGEMENT_DEFENSE";
    public const string CelestialProtection = "CELESTIAL_PROTECTION";
    public const string AngelBlessing = "ANGEL_BLESSING";

    // 5 Legendary Heroes Statuses
    public const string PhongAn = "PHONG_AN";
    public const string TinChiDanhDu = "TIN_CHI_DANH_DU";
    public const string LuanDiem = "LUAN_DIEM";
    public const string CatScratch = "CAT_SCRATCH";
    public const string DeepCatScratch = "DEEP_CAT_SCRATCH";
    public const string CatCompanion = "CAT_COMPANION";
    public const string ChayNgayDi = "CHAY_NGAY_DI";

    // Target Selectors for Legendary Heroes
    public const string EnemySameVerticalLane = "ENEMY_SAME_VERTICAL_LANE";
    public const string EnemyFrontRowWithBackRowFallback = "ENEMY_FRONT_ROW_WITH_BACK_ROW_FALLBACK";
    public const string RandomEligibleAlliesN = "RANDOM_ELIGIBLE_ALLIES_N";

    // 5 Legendary Heroes Skills
    public const string KietMaiXeoBasic = "KIET_MAI_XEO_BASIC";
    public const string KietMaiXeoEnergy = "KIET_MAI_XEO_ENERGY";
    public const string TruongKietGraduationBasic = "TRUONG_KIET_GRADUATION_BASIC";
    public const string TruongKietGraduationEnergy = "TRUONG_KIET_GRADUATION_ENERGY";
    public const string QuocNhanGraduationBasic = "QUOC_NHAN_GRADUATION_BASIC";
    public const string QuocNhanGraduationEnergy = "QUOC_NHAN_GRADUATION_ENERGY";
    public const string LongLeCatScratchBasic = "LONG_LE_CAT_SCRATCH_BASIC";
    public const string LongLeCatCompanions = "LONG_LE_CAT_COMPANIONS";
    public const string QuocNhanRunNowBasic = "QUOC_NHAN_RUN_NOW_BASIC";
    public const string QuocNhanRunNowEnergy = "QUOC_NHAN_RUN_NOW_ENERGY";
}
