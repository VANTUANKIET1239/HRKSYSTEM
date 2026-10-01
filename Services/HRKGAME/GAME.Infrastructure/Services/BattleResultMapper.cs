using GAME.Application.DTOs;
using GAME.Domain.Battle;

namespace GAME.Infrastructure.Services;

public static class BattleResultMapper
{
    public static StartBattleResultDto Map(string battleId, BattleInitialStateDto initialState,
        BattleSimulationResult simulation, int randomSeed, List<BattleHeroStatisticsDto> heroStats)
    {
        initialState.BattleId = battleId;
        return new StartBattleResultDto
        {
            BattleId = battleId,
            RandomSeed = randomSeed,
            Winner = simulation.Winner,
            InitialState = initialState,
            HeroStatistics = heroStats,
            Events = simulation.Events.Select(e => new BattleEventDto
            {
                Sequence = e.Sequence,
                Round = e.Round,
                Turn = e.Turn,
                EventType = e.EventType,
                ActorId = e.ActorId,
                TargetId = e.TargetId,
                SkillId = e.SkillId,
                EffectTypeCode = e.EffectTypeCode,
                DamageSchoolCode = e.DamageSchoolCode,
                Value = e.Value,
                HpBefore = e.HpBefore,
                HpAfter = e.HpAfter,
                EnergyBefore = e.EnergyBefore,
                EnergyAfter = e.EnergyAfter,
                IsCrit = e.IsCrit,
                RemainingTurns = e.RemainingTurns,
                PreviousStacks = e.PreviousStacks,
                CurrentStacks = e.CurrentStacks,
                MaxStacks = e.MaxStacks,
                CastSequence = e.CastSequence,
                TimelineOffsetMs = e.TimelineOffsetMs,
                PhaseCode = e.PhaseCode,
                ExecutionGroup = e.ExecutionGroup,
                HitIndex = e.HitIndex,
                ResourceCode = e.ResourceCode,
                PreviousValue = e.PreviousValue,
                CurrentValue = e.CurrentValue,
                ReasonCode = e.ReasonCode,
                ActionId = e.ActionId,
                StatusInstanceId = e.StatusInstanceId,
                SourceHeroId = e.SourceHeroId,
                OriginalDamage = e.OriginalDamage,
                RedirectRequested = e.RedirectRequested,
                RedirectActual = e.RedirectActual,
                AllyDamageAfterRedirect = e.AllyDamageAfterRedirect,
                GuardianHpBefore = e.GuardianHpBefore,
                GuardianHpAfter = e.GuardianHpAfter,
                StatModifiers = e.StatModifiers.Select(m => new BattleStatModifierDto
                {
                    AttributeCode = m.AttributeCode,
                    AttributeName = m.AttributeName,
                    ValueType = m.ValueType,
                    Value = m.Value
                }).ToList()
            }).ToList()
        };
    }
}
