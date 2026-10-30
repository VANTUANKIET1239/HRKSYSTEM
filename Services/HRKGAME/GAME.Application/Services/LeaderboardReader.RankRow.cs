using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Interfaces;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;
public sealed partial class LeaderboardReader
{
    private sealed record RankRow
    {
        public long PlayerId { get; init; }
        public string UserId { get; init; } = "";
        public string PlayerName { get; init; } = "";
        public int Level { get; init; }
        public string AvatarType { get; init; } = "";
        public int? AvatarTemplateId { get; init; }
        public DateTime UpdatedOn { get; init; }
        public long Score { get; init; }
        public DateTime AchievedAt { get; init; }
    }
}
