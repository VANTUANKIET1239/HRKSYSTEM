namespace GAME.Domain.Battle.Skills.ThanhThaiAura;

public static class ThanhThaiFormationGrid
{
    // Formation positions:
    // Pos 1: Front Row, Top Lane  -> (Col 1, Row 0)
    // Pos 2: Back Row,  Top Lane  -> (Col 0, Row 0)
    // Pos 3: Front Row, Mid Lane  -> (Col 1, Row 1)
    // Pos 4: Back Row,  Bot Lane  -> (Col 0, Row 2)
    // Pos 5: Front Row, Bot Lane  -> (Col 1, Row 2)
    public static (int Col, int Row) GetCoordinates(int position) => position switch
    {
        1 => (1, 0),
        2 => (0, 0),
        3 => (1, 1),
        4 => (0, 2),
        5 => (1, 2),
        _ => (1, 1)
    };

    public static double GetDistance(int posA, int posB)
    {
        var (c1, r1) = GetCoordinates(posA);
        var (c2, r2) = GetCoordinates(posB);
        var dc = c1 - c2;
        var dr = r1 - r2;
        return Math.Sqrt(dc * dc + dr * dr);
    }

    public static int GetLossOfConfidenceStacks(BattleCombatant combatant)
    {
        var status = combatant.StatusEffects.FirstOrDefault(x =>
            x.EffectTypeCode.Equals(ThanhThaiAuraSkillCodes.StatusLossOfConfidence, StringComparison.OrdinalIgnoreCase) &&
            (x.RemainingTurns > 0 || x.RemainingTurns == -1));
        return status?.Stacks ?? 0;
    }

    public static BattleCombatant? SelectPrimaryTarget(IEnumerable<BattleCombatant> enemies)
    {
        return enemies
            .Where(e => e.IsAlive)
            .OrderByDescending(GetLossOfConfidenceStacks)
            .ThenBy(e => (decimal)e.Hp / Math.Max(1, e.MaxHp))
            .ThenBy(e => e.Position)
            .ThenBy(e => e.Id)
            .FirstOrDefault();
    }

    public static BattleCombatant? SelectNextChainTarget(
        BattleCombatant previousTarget,
        IEnumerable<BattleCombatant> candidates,
        double maxDistance = 1.50)
    {
        return candidates
            .Where(e => e.IsAlive)
            .Select(e => new { Candidate = e, Distance = GetDistance(previousTarget.Position, e.Position) })
            .Where(x => x.Distance <= maxDistance)
            .OrderByDescending(x => GetLossOfConfidenceStacks(x.Candidate))
            .ThenBy(x => x.Distance)
            .ThenBy(x => (decimal)x.Candidate.Hp / Math.Max(1, x.Candidate.MaxHp))
            .ThenBy(x => x.Candidate.Position)
            .ThenBy(x => x.Candidate.Id)
            .Select(x => x.Candidate)
            .FirstOrDefault();
    }

    public static List<BattleCombatant> SelectChainTargets(
        IEnumerable<BattleCombatant> enemies,
        int maxTargets,
        double maxDistance = 1.50)
    {
        var result = new List<BattleCombatant>();
        var livingEnemies = enemies.Where(e => e.IsAlive).ToList();
        if (livingEnemies.Count == 0 || maxTargets <= 0) return result;

        var primary = SelectPrimaryTarget(livingEnemies);
        if (primary == null) return result;
        result.Add(primary);

        var current = primary;
        while (result.Count < maxTargets)
        {
            var alreadyHitIds = result.Select(x => x.Id).ToHashSet();
            var remainingCandidates = livingEnemies.Where(e => !alreadyHitIds.Contains(e.Id) && e.IsAlive).ToList();
            if (remainingCandidates.Count == 0) break;

            var next = SelectNextChainTarget(current, remainingCandidates, maxDistance);
            if (next == null) break;

            result.Add(next);
            current = next;
        }

        return result;
    }
}
