using GAME.Application.DTOs;
using GAME.Domain.Battle;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GAME.Infrastructure.Services
{
    public static class BattleStatisticsCalculator
    {
        /// <summary>
        /// Maps damage school code to either PHYSICAL or MAGIC.
        /// TRUE damage is currently mapped to PHYSICAL per game specification.
        /// In the future, this helper can be expanded if TRUE damage is split into its own school.
        /// </summary>
        public static string NormalizeDamageSchool(string? damageSchoolCode)
        {
            if (string.Equals(damageSchoolCode, BattleCodes.Magic, StringComparison.OrdinalIgnoreCase))
                return BattleCodes.Magic;

            // TRUE damage and any unspecified school is mapped to PHYSICAL per specification rule 10
            return BattleCodes.Physical;
        }

        public static List<BattleHeroStatisticsDto> Calculate(
            BattleInitialStateDto initialState,
            IReadOnlyList<BattleEvent> events)
        {
            var result = new List<BattleHeroStatisticsDto>();
            if (initialState == null) return result;

            // Map combatants
            // Left team: Team = 0, CombatantId = hero.Id
            // Right team: Team = 1, CombatantId = -hero.Id
            var statsByCombatantId = new Dictionary<long, BattleHeroStatisticsDto>();
            var statsBySourceHeroIdAndTeam = new Dictionary<(long HeroId, int Team), BattleHeroStatisticsDto>();

            foreach (var hero in initialState.LeftTeam)
            {
                var stat = new BattleHeroStatisticsDto
                {
                    CombatantId = hero.Id,
                    SourceHeroId = hero.Id,
                    Team = 0,
                    HeroName = hero.Name,
                    Avatar = hero.Avatar
                };
                result.Add(stat);
                statsByCombatantId[stat.CombatantId] = stat;
                statsBySourceHeroIdAndTeam[(hero.Id, 0)] = stat;
            }

            foreach (var hero in initialState.RightTeam)
            {
                var combatantId = -hero.Id;
                var stat = new BattleHeroStatisticsDto
                {
                    CombatantId = combatantId,
                    SourceHeroId = hero.Id,
                    Team = 1,
                    HeroName = hero.Name,
                    Avatar = hero.Avatar
                };
                result.Add(stat);
                statsByCombatantId[combatantId] = stat;
                statsBySourceHeroIdAndTeam[(hero.Id, 1)] = stat;
            }

            if (events == null || events.Count == 0)
                return result;

            foreach (var evt in events)
            {
                // 1. Calculate Damage
                if (evt.HpBefore.HasValue && evt.HpAfter.HasValue && evt.HpBefore.Value > evt.HpAfter.Value)
                {
                    long actualDamage = evt.HpBefore.Value - evt.HpAfter.Value;
                    if (actualDamage > 0)
                    {
                        var school = NormalizeDamageSchool(evt.DamageSchoolCode);

                        // Target takes damage
                        if (evt.TargetId.HasValue && statsByCombatantId.TryGetValue(evt.TargetId.Value, out var targetStat))
                        {
                            if (school == BattleCodes.Magic)
                                targetStat.MagicDamageTaken += actualDamage;
                            else
                                targetStat.PhysicalDamageTaken += actualDamage;
                        }

                        // Source deals damage
                        // Determine source: Prioritize SourceHeroId (for DoT, Reflection, Status), fallback to ActorId
                        BattleHeroStatisticsDto? sourceStat = null;
                        if (evt.SourceHeroId.HasValue)
                        {
                            if (statsByCombatantId.TryGetValue(evt.SourceHeroId.Value, out var s))
                            {
                                sourceStat = s;
                            }
                            else if (evt.TargetId.HasValue && statsByCombatantId.TryGetValue(evt.TargetId.Value, out var tgt))
                            {
                                int opposingTeam = tgt.Team == 0 ? 1 : 0;
                                statsBySourceHeroIdAndTeam.TryGetValue((evt.SourceHeroId.Value, opposingTeam), out sourceStat);
                            }

                            if (sourceStat == null)
                            {
                                statsBySourceHeroIdAndTeam.TryGetValue((evt.SourceHeroId.Value, 0), out sourceStat);
                                if (sourceStat == null)
                                    statsBySourceHeroIdAndTeam.TryGetValue((evt.SourceHeroId.Value, 1), out sourceStat);
                            }
                        }

                        if (sourceStat == null && evt.ActorId.HasValue)
                        {
                            statsByCombatantId.TryGetValue(evt.ActorId.Value, out sourceStat);
                        }

                        if (sourceStat != null)
                        {
                            if (school == BattleCodes.Magic)
                                sourceStat.MagicDamageDealt += actualDamage;
                            else
                                sourceStat.PhysicalDamageDealt += actualDamage;
                        }
                    }
                }

                // 2. Calculate Healing
                // Only count actual healing where HP increased
                if (evt.HpBefore.HasValue && evt.HpAfter.HasValue && evt.HpAfter.Value > evt.HpBefore.Value)
                {
                    long actualHealing = evt.HpAfter.Value - evt.HpBefore.Value;
                    if (actualHealing > 0)
                    {
                        BattleHeroStatisticsDto? healerStat = null;
                        if (evt.SourceHeroId.HasValue)
                        {
                            if (statsByCombatantId.TryGetValue(evt.SourceHeroId.Value, out var s))
                            {
                                healerStat = s;
                            }
                            else if (evt.TargetId.HasValue && statsByCombatantId.TryGetValue(evt.TargetId.Value, out var tgt))
                            {
                                statsBySourceHeroIdAndTeam.TryGetValue((evt.SourceHeroId.Value, tgt.Team), out healerStat);
                            }

                            if (healerStat == null)
                            {
                                statsBySourceHeroIdAndTeam.TryGetValue((evt.SourceHeroId.Value, 0), out healerStat);
                                if (healerStat == null)
                                    statsBySourceHeroIdAndTeam.TryGetValue((evt.SourceHeroId.Value, 1), out healerStat);
                            }
                        }

                        if (healerStat == null && evt.ActorId.HasValue)
                        {
                            statsByCombatantId.TryGetValue(evt.ActorId.Value, out healerStat);
                        }

                        if (healerStat == null && evt.TargetId.HasValue)
                        {
                            statsByCombatantId.TryGetValue(evt.TargetId.Value, out healerStat);
                        }

                        if (healerStat != null)
                        {
                            healerStat.HealingDone += actualHealing;
                        }
                    }
                }
            }

            return result;
        }
    }
}
