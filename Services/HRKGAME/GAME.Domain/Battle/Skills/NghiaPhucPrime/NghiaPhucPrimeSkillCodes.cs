namespace GAME.Domain.Battle.Skills.NghiaPhucPrime;

public static class NghiaPhucPrimeSkillCodes
{
    public const string Hero = "NGHIA_PHUC_PRIME";

    // Skills
    public const string Basic = "PRIME_SHIELD_WARRANTY";
    public const string Ultimate = "PRIME_FORTRESS_CHARGE";

    // Status / Effect Types
    public const string Fortitude = "PRIME_FORTITUDE";
    public const string Guardian = "PRIME_GUARDIAN";
    public const string Pressure = "PRIME_PRESSURE";
    public const string Stagger = "PRIME_STAGGER";
    public const string BrokenMorale = "PRIME_BROKEN_MORALE";

    // Battle Events
    public const string EventFortitudeGained = "PRIME_FORTITUDE_GAINED";
    public const string EventFortitudeConsumed = "PRIME_FORTITUDE_CONSUMED";
    public const string EventGuardianApplied = "PRIME_GUARDIAN_APPLIED";
    public const string EventGuardRedirected = "PRIME_GUARD_REDIRECTED";
    public const string EventPressureChanged = "PRIME_PRESSURE_CHANGED";
    public const string EventPressureReleased = "PRIME_PRESSURE_RELEASED";
    public const string EventStaggerApplied = "PRIME_STAGGER_APPLIED";
    public const string EventFortressChargeStarted = "PRIME_FORTRESS_CHARGE_STARTED";
    public const string EventFortressImpact = "PRIME_FORTRESS_IMPACT";
    public const string EventFortressReturned = "PRIME_FORTRESS_RETURNED";

    // Parameters - Basic Skill
    public const string ParamDamageCoefficient = "DAMAGE_COEFFICIENT";
    public const string ParamAllyShieldMaxHpPercent = "ALLY_SHIELD_MAX_HP_PERCENT";
    public const string ParamExistingShieldRestorePercent = "EXISTING_SHIELD_RESTORE_PERCENT";
    public const string ParamShieldDurationTurns = "SHIELD_DURATION_TURNS";
    public const string ParamFortitudeMaxStacks = "FORTITUDE_MAX_STACKS";
    public const string ParamFortitudeDefPercentPerStack = "FORTITUDE_DEF_PERCENT_PER_STACK";
    public const string ParamFortitudeMagicResistancePercentPerStack = "FORTITUDE_MAGIC_RESISTANCE_PERCENT_PER_STACK";
    public const string ParamSelfShieldOnMaxStackPercent = "SELF_SHIELD_ON_MAX_STACK_PERCENT";

    // Parameters - Ultimate Skill
    public const string ParamFortressDamageCoefficient = "FORTRESS_DAMAGE_COEFFICIENT";
    public const string ParamCanCrit = "CAN_CRIT";
    public const string ParamStaggerDurationTurns = "STAGGER_DURATION_TURNS";
    public const string ParamActionBarReduction = "ACTION_BAR_REDUCTION";
    public const string ParamSpeedReductionPercent = "SPEED_REDUCTION_PERCENT";
    public const string ParamBrokenMoraleTargetCount = "BROKEN_MORALE_TARGET_COUNT";
    public const string ParamOutgoingDamageReductionPercent = "OUTGOING_DAMAGE_REDUCTION_PERCENT";
    public const string ParamBrokenMoraleDurationTurns = "BROKEN_MORALE_DURATION_TURNS";
    public const string ParamTeamShieldCasterMaxHpPercent = "TEAM_SHIELD_CASTER_MAX_HP_PERCENT";
    public const string ParamTeamShieldCasterDefPercent = "TEAM_SHIELD_CASTER_DEF_PERCENT";
    public const string ParamTeamShieldTargetMaxHpCapPercent = "TEAM_SHIELD_TARGET_MAX_HP_CAP_PERCENT";
    public const string ParamTeamShieldDurationTurns = "TEAM_SHIELD_DURATION_TURNS";
    public const string ParamGuardianDurationTurns = "GUARDIAN_DURATION_TURNS";
    public const string ParamDamageRedirectPercent = "DAMAGE_REDIRECT_PERCENT";
    public const string ParamGuardianMinHp = "GUARDIAN_MIN_HP";
    public const string ParamPressureMaxStacks = "PRESSURE_MAX_STACKS";
    public const string ParamPressureHealMaxHpPercentPerStack = "PRESSURE_HEAL_MAX_HP_PERCENT_PER_STACK";
    public const string ParamPressureDamageDefPercentPerStack = "PRESSURE_DAMAGE_DEF_PERCENT_PER_STACK";
    public const string ParamPressureCanCrit = "PRESSURE_CAN_CRIT";
    public const string ParamPressureReleaseAtMax = "PRESSURE_RELEASE_AT_MAX";

    // Internal identifier for shield instance source
    public const string ShieldSourceBasic = "PRIME_SHIELD_BASIC";
    public const string ShieldSourceTeam = "PRIME_SHIELD_TEAM";
}
