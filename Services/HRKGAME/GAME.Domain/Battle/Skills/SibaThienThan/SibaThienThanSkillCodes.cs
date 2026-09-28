namespace GAME.Domain.Battle.Skills.SibaThienThan;

public static class SibaThienThanSkillCodes
{
    public const string HeroCode = "siba-thien-than";
    public const string Basic = "SIBA_ANGEL_GENTLE_WING";
    public const string Ultimate = "SIBA_CELESTIAL_PROTECTION";

    // Resources
    public const string ResourceBlessing = "ANGEL_BLESSING";

    // Status codes & groups
    public const string StatusGroupEncouragement = "ENCOURAGEMENT";
    public const string StatusOffense = "ENCOURAGEMENT_OFFENSE";
    public const string StatusDefense = "ENCOURAGEMENT_DEFENSE";
    public const string StatusCelestialProtection = "CELESTIAL_PROTECTION";

    // Custom events
    public const string EmpoweredCast = "SIBA_EMPOWERED_CAST";

    // Target selector codes
    public const string TargetAllyLowestHpPreferWithoutStatus = "ALLY_LOWEST_HP_PREFER_WITHOUT_STATUS";

    // Parameter codes
    public const string ParamCanCrit = "CAN_CRIT";
    public const string ParamTargetCount = "TARGET_COUNT";
    public const string ParamPreferredMissingStatusGroup = "PREFERRED_MISSING_STATUS_GROUP";
    public const string ParamStatusGroup = "STATUS_GROUP";
    public const string ParamBuffPercent = "BUFF_PERCENT";
    public const string ParamDurationTurns = "DURATION_TURNS";
    public const string ParamEnergyDelta = "ENERGY_DELTA";
    public const string ParamEnergyGain = "ENERGY_GAIN";
    public const string ParamMaxBlessingStacks = "MAX_BLESSING_STACKS";
    public const string ParamBlessingGainPerBasic = "BLESSING_GAIN_PER_BASIC";
    public const string ParamBlessingCostForEmpowered = "BLESSING_COST_FOR_EMPOWERED";
    public const string ParamDispelCount = "DISPEL_COUNT";
    public const string ParamSelectionMode = "SELECTION_MODE";
    public const string ParamDispellableOnly = "DISPELLABLE_ONLY";
    public const string ParamRefreshDuration = "REFRESH_DURATION";
    public const string ParamEmpoweredEnergyGain = "EMPOWERED_ENERGY_GAIN";
}
