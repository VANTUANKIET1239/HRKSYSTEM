using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.DTOs.Leaderboard;
using GAME.Application.Interfaces;
using GAME.Application.Templates;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Services;

public sealed partial class LeaderboardReader(IUnitOfWork uow, IGameTemplateCatalog templates,
    IFormationSnapshotService formations, IPlayerReadModels players) : ILeaderboardReader
{
    public async Task<LeaderboardResponse> GetAsync(string userId, string type, int page, int pageSize, CancellationToken ct)
    {
        if (type is not ("power" or "diamonds-spent") || page < 1 || pageSize is < 1 or > 100 || (long)(page - 1) * pageSize > int.MaxValue - pageSize)
            throw new BusinessException("INVALID_LEADERBOARD_REQUEST", "Loại bảng xếp hạng hoặc phân trang không hợp lệ.", BusinessErrorKind.Validation);
        var source = uow.ReadOnlyRepository<HrkPlayerRankingStats>().Query().Where(x => x.Player.IsActive);
        // Use direct indexed columns in each category; a parameterized CASE in ORDER BY
        // would force a sort instead of allowing the dedicated ranking index to serve pages.
        var query = type == "power"
            ? source.Where(x => x.PowerInitialized).Select(x => new RankRow
            {
                PlayerId = x.PlayerId, UserId = x.Player.UserId, PlayerName = x.Player.PlayerName,
                Level = x.Player.Level, AvatarType = x.Player.AvatarType, AvatarTemplateId = x.Player.AvatarTemplateId,
                UpdatedOn = x.Player.UpdatedOn,
                Score = x.PeakFormationPower, AchievedAt = x.PeakPowerAchievedAtUtc
            })
            : source.Select(x => new RankRow
            {
                PlayerId = x.PlayerId, UserId = x.Player.UserId, PlayerName = x.Player.PlayerName,
                Level = x.Player.Level, AvatarType = x.Player.AvatarType, AvatarTemplateId = x.Player.AvatarTemplateId,
                UpdatedOn = x.Player.UpdatedOn,
                Score = x.TotalDiamondsSpent, AchievedAt = x.DiamondsSpentAchievedAtUtc
            });
        var count = await query.LongCountAsync(ct);
        var offset = checked((page - 1) * pageSize);
        var rows = await query.OrderByDescending(x => x.Score).ThenBy(x => x.AchievedAt).ThenBy(x => x.PlayerId)
            .Skip(offset).Take(pageSize).ToListAsync(ct);
        var avatars = await templates.GetAllAsync<HrkAvatarTemplate>(ct);
        var items = rows.Select((x, i) => Map(x, (long)offset + i + 1, userId, avatars)).ToList();
        var own = await query.SingleOrDefaultAsync(x => x.UserId == userId, ct);
        LeaderboardEntryResponse? me = null;
        if (own is not null)
        {
            var ahead = await query.LongCountAsync(x => x.Score > own.Score ||
                (x.Score == own.Score && (x.AchievedAt < own.AchievedAt ||
                 (x.AchievedAt == own.AchievedAt && x.PlayerId < own.PlayerId))), ct);
            me = Map(own, ahead + 1, userId, avatars);
        }
        return new(type, page, pageSize, count, items, me, DateTimeOffset.UtcNow);
    }

    public async Task<PublicPlayerProfileResponse> GetProfileAsync(long playerId, CancellationToken ct)
    {
        var player = await RequiredPlayerAsync(playerId, ct);
        var stats = await uow.ReadOnlyRepository<HrkPlayerRankingStats>().Query().SingleOrDefaultAsync(x => x.PlayerId == playerId, ct);
        var formation = await formations.BuildAsync(playerId, null, null, ct);
        var heroes = formation.Heroes.Select(h => new PublicHeroSummaryResponse(h.Id, h.Name, h.Avatar,
            h.RarityCode, h.Level, h.Stars, formation.HeroPowers.GetValueOrDefault(h.Id, h.Power),
            formation.Slots.FirstOrDefault(s => s.Hero?.Id == h.Id)?.Slot ?? 0)).OrderBy(x => x.Slot).ToList();
        return new(player.Id, player.PlayerName, player.Level, player.AvatarType,
            Avatar(player.AvatarTemplateId, await templates.GetAllAsync<HrkAvatarTemplate>(ct)), Unix(player.UpdatedOn),
            formation.TotalPower, stats?.PeakFormationPower ?? 0, stats is { PowerInitialized: true } ? Utc(stats.PeakPowerAchievedAtUtc) : null,
            stats?.TotalDiamondsSpent ?? 0, stats is null ? null : Utc(stats.TrackingStartedAtUtc), formation.FormationName, heroes);
    }

    public async Task<PublicHeroDetailResponse> GetHeroAsync(long playerId, long heroId, CancellationToken ct)
    {
        var player = await RequiredPlayerAsync(playerId, ct);
        var placed = await uow.ReadOnlyRepository<HrkPlayerFormation>().Query().AnyAsync(f => f.PlayerId == playerId && f.IsActive && f.IsSelected &&
            (f.Position1 == heroId || f.Position2 == heroId || f.Position3 == heroId || f.Position4 == heroId || f.Position5 == heroId), ct);
        if (!placed) throw NotFound();
        // Reuse the existing owned-hero projection internally; its private DTO never leaves this endpoint.
        var hero = await players.GetPlayerHeroDetailAsync(player.UserId, heroId, ct) ?? throw NotFound();
        var equipment = new List<PublicEquipmentResponse>();
        Add("WEAPON", hero.Equipment.Weapon); Add("HELMET", hero.Equipment.Helmet);
        Add("ARMOR", hero.Equipment.Armor); Add("BOOTS", hero.Equipment.Boots);
        Add("RING", hero.Equipment.Ring); Add("ARTIFACT", hero.Equipment.Artifact);
        return new(hero.Id, hero.Name, hero.Avatar, hero.RarityCode, hero.Level, hero.Stars, hero.Power,
            hero.FactionName, hero.ClassName, hero.Stats, hero.Skills, equipment);

        void Add(string slot, InventoryItemDto? item)
        {
            if (item is null) return;
            equipment.Add(new(slot, item.Name, item.ImagePath, item.RarityCode, item.Enhancement, item.Stars,
                (item.Attributes ?? []).Select(a => new PublicAttributeResponse(a.AttributeCode, a.AttributeName, a.Value, a.IsPercentage)).ToList()));
        }
    }

    private async Task<HrkPlayer> RequiredPlayerAsync(long id, CancellationToken ct) =>
        await uow.ReadOnlyRepository<HrkPlayer>().Query().SingleOrDefaultAsync(x => x.Id == id && x.IsActive, ct) ?? throw NotFound();
    private static BusinessException NotFound() => new("PUBLIC_PLAYER_NOT_FOUND", "Không tìm thấy người chơi hoặc võ tướng trong đội hình.", BusinessErrorKind.NotFound);
    private static string? Avatar(int? id, IReadOnlyCollection<HrkAvatarTemplate> avatars) => avatars.Where(x => x.IsEnabled && (x.Id == id || x.IsDefault))
        .OrderByDescending(x => x.Id == id).Select(x => x.ImagePath).FirstOrDefault();
    private static DateTimeOffset Utc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    private static long Unix(DateTime value) => Utc(value).ToUnixTimeSeconds();
    private static LeaderboardEntryResponse Map(RankRow row, long rank, string userId, IReadOnlyCollection<HrkAvatarTemplate> avatars) =>
        new(rank, new(row.PlayerId, row.PlayerName, row.Level, row.AvatarType, Avatar(row.AvatarTemplateId, avatars), Unix(row.UpdatedOn), row.Score, Utc(row.AchievedAt)), row.UserId == userId);
}
