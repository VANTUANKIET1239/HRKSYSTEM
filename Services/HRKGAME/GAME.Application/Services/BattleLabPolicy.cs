using Core.Common.Errors;
using GAME.Application.DTOs;

namespace GAME.Application.Services;

public static class BattleLabPolicy
{
    public static void Validate(BattleLabRequestDto request)
    {
        if (request.Count is < 1 or > 300 || request.MaxRounds is < 1 or > 100 || request.Seed < 0 || request.Seed > int.MaxValue - request.Count || request.DefenseConstant is <= 0 or > 1000000)
            throw new BusinessException("INVALID_REQUEST", "Invalid lab settings: count 1–300, rounds 1–100, positive K, nonnegative seed.", BusinessErrorKind.Validation);
        foreach (var team in new[]
        {
            request.Left,
            request.Right
        }

        )
        {
            if (team == null || team.Count is < 1 or > 5 || team.Any(s => s == null) || team.Select(s => s.Position).Distinct().Count() != team.Count)
                throw new BusinessException("DUPLICATE_HERO", "Each side needs 1–5 heroes with unique positions.", BusinessErrorKind.Validation);
            foreach (var slot in team)
            {
                if (slot.Position is < 1 or > 5)
                    throw new BusinessException("INVALID_REQUEST", "Invalid position.", BusinessErrorKind.Validation);
                if (slot.Mode == "BUILD")
                {
                    var b = slot.Build;
                    if (slot.Stats != null || b == null || b.Level is < 1 or > 1000 || b.Stars is < 1 or > 5 || b.AuraTier is < 1 or > 4 || b.RollSeed < 0 || b.Equipment == null || b.Equipment.Count > 6 || b.Equipment.Any(e => e == null || e.ItemTemplateId <= 0 || e.Enhancement is < 0 or > 15 || e.Stars is < 0 or > 5))
                        throw new BusinessException("INVALID_REQUEST", "BUILD requires build only (no stats), valid level/stars/aura and equipment.", BusinessErrorKind.Validation);
                    continue;
                }

                if (slot.Mode != "CUSTOM" || slot.Build != null)
                    throw new BusinessException("INVALID_REQUEST", "CUSTOM accepts stats only; BUILD accepts build only.", BusinessErrorKind.Validation);
                var s = slot.Stats;
                if (slot.Position is < 1 or > 5 || s == null || s.Hp is < 1 or > 1000000 || s.Spd is < 1 or > 10000 || s.Atk is < 0 or > 100000 || s.Def is < 0 or > 100000 || s.MagicDamage is < 0 or > 100000 || s.MagicResistance is < 0 or > 100000 || s.Crit is < 0 or > 1000 || s.CritDmg is < 0 or > 1000)
                    throw new BusinessException("INVALID_REQUEST", "Invalid hero stats or position in lab.", BusinessErrorKind.Validation);
            }
        }
    }
}
