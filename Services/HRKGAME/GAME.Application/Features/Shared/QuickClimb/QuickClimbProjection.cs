using GAME.Application.DTOs;
using GAME.Application.Services;
using GAME.Domain.Entities;
using System.Text.Json;

namespace GAME.Application.Features.Shared.QuickClimb;

public static class QuickClimbProjection
{
    public static QuickClimbJobStatusDto MapJobToDto(HrkTowerQuickClimbJob job) => new()
    {
        JobId = job.JobId,
        Status = job.Status,
        StopReason = job.StopReason,
        StartFloor = job.StartFloor,
        CurrentFloor = job.CurrentFloor,
        TargetFloor = job.TargetFloor,
        InitialLives = job.InitialLives,
        RemainingLives = job.RemainingLives,
        ClearedFloorsCount = job.ClearedFloorsCount,
        DailyRunNumber = job.DailyRunNumber,
        FailedFloor = job.FailedFloor,
        AccumulatedRewards = ParseRewardsJson(job.AccumulatedRewardsJson),
        Logs = ParseLogsJson(job.LogsJson),
        LastBattleId = job.LastBattleId,
        CreatedOnUtc = job.CreatedOnUtc,
        UpdatedOnUtc = job.UpdatedOnUtc,
        CompletedOnUtc = job.CompletedOnUtc,
        Version = job.Version
    };
    public static List<GenericRewardItemDto> ParseRewardsJson(string? json) => TowerRewardService.ParseRewards(json);
    public static List<QuickClimbFloorLogDto> ParseLogsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new();
        try
        {
            return JsonSerializer.Deserialize<List<QuickClimbFloorLogDto>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
