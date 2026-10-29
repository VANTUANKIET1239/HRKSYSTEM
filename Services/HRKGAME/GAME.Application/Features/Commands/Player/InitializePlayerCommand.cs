using System.Security.Cryptography;
using System.Text;
using Core.Application.CQRS;
using Core.Messaging.Contracts;
using Core.Persistence.Repositories;
using Core.TransactionalMessaging.Inbox;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.Player;

public sealed record InitializePlayerCommand(AccountRegisteredV1 Account) : ICommand<bool>;

public sealed class InitializePlayerCommandHandler(IUnitOfWork unitOfWork, IInboxExecutor inbox,
    ILevelExperienceService levels, NewPlayerHeroFactory heroes, GameRewardGranter rewards,
    IPlayerActivityEvents activities, IFormationPowerQueryService? formationPower = null) : ICommandHandler<InitializePlayerCommand, bool>
{
    public async Task<bool> Handle(InitializePlayerCommand request, CancellationToken ct)
    {
        var message = request.Account;
        if (message.AppCode != AccountContractNames.GameAppCode) return false;
        if (message.EventId == Guid.Empty || string.IsNullOrWhiteSpace(message.UserId) || message.UserId.Length > 450 ||
            string.IsNullOrWhiteSpace(message.DisplayName) || message.DisplayName.Length > 60)
            throw new ArgumentException("Invalid account registration event.");

        // Pin the selected config before any grant. A later retry must use this version.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await LockUserAsync(message.UserId, ct);
            if (await unitOfWork.ReadOnlyRepository<HrkPlayerInitialization>().Query().AnyAsync(x => x.UserId == message.UserId, ct)) return;
            var existing = await unitOfWork.ReadOnlyRepository<HrkPlayer>().Query().SingleOrDefaultAsync(x => x.UserId == message.UserId, ct);
            var configId = existing is null
                ? await unitOfWork.ReadOnlyRepository<HrkPlayerInitializationConfig>().Query()
                    .Where(x => x.Code == "DEFAULT" && x.IsActive).Select(x => (long?)x.Id).SingleOrDefaultAsync(ct)
                    ?? throw new InvalidOperationException("No active DEFAULT player initialization config.")
                : (long?)null;
            await unitOfWork.Repository<HrkPlayerInitialization>().AddAsync(new()
            {
                UserId = message.UserId, ConfigId = configId, RegistrationEventId = message.EventId,
                PlayerId = existing?.Id, Status = existing is null ? "PENDING" : "COMPLETED",
                CompletedAtUtc = existing is null ? null : DateTime.UtcNow
            });
        }, ct);

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await LockUserAsync(message.UserId, ct);
            await inbox.ExecuteInCurrentTransactionAsync("game.account.registered.v1", message.EventId.ToString("N"), async token =>
            {
                var receipt = await unitOfWork.Repository<HrkPlayerInitialization>().Query().SingleAsync(x => x.UserId == message.UserId, token);
                if (receipt.Status == "COMPLETED") return true;
                // Legacy players created between delivery attempts must also never receive another grant.
                var existing = await unitOfWork.ReadOnlyRepository<HrkPlayer>().Query().SingleOrDefaultAsync(x => x.UserId == message.UserId, token);
                var playerId = existing?.Id ?? await CreatePlayerAsync(message, receipt, token);
                receipt.PlayerId = playerId;
                receipt.Status = "COMPLETED";
                receipt.CompletedAtUtc = DateTime.UtcNow;
                return true;
            }, ct);
        }, ct);
        return true;
    }

    private Task<int> LockUserAsync(string userId, CancellationToken ct)
    {
        var resource = "player-initialize:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userId)));
        return unitOfWork.ExecuteSqlAsync($"DECLARE @result int; EXEC @result = sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @result < 0 THROW 51000, 'Player initialization lock unavailable.', 1;", ct);
    }

    private async Task<long> CreatePlayerAsync(AccountRegisteredV1 account, HrkPlayerInitialization receipt, CancellationToken ct)
    {
        var config = await unitOfWork.ReadOnlyRepository<HrkPlayerInitializationConfig>().Query()
            .SingleAsync(x => x.Id == receipt.ConfigId, ct);
        var starterHeroes = await unitOfWork.ReadOnlyRepository<HrkPlayerInitializationHero>().Query()
            .Where(x => x.ConfigId == config.Id).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        if (config.InitialLevel < 1 || config.MaxStamina < 1 || config.InitialStamina < 0 || config.InitialStamina > config.MaxStamina ||
            config.InitialGold < 0 || config.InitialDiamonds < 0 || config.InitialUpgradeMaterials < 0 || config.InventoryCapacity < 1 ||
            starterHeroes.Count == 0 || starterHeroes.Any(x => x.InitialLevel != 1 || x.InitialStars != 1 || x.FormationPosition is < 1 or > 5))
            throw new InvalidOperationException("Invalid player initialization configuration (starter heroes currently require level 1/star 1).");
        var formationTemplate = await unitOfWork.ReadOnlyRepository<HrkFormationTemplate>().Query()
            .SingleOrDefaultAsync(x => x.Id == config.DefaultFormationTemplateId && x.IsEnabled, ct)
            ?? throw new InvalidOperationException("Default formation is unavailable.");
        var slots = await unitOfWork.ReadOnlyRepository<HrkFormationSlotTemplate>().Query()
            .Where(x => x.FormationTemplateId == formationTemplate.Id && x.IsEnabled).Select(x => x.Slot).ToListAsync(ct);
        if (starterHeroes.Any(x => x.FormationPosition.HasValue && !slots.Contains(x.FormationPosition.Value)))
            throw new InvalidOperationException("A starter hero uses an unavailable formation slot.");
        if (config.DefaultAvatarTemplateId.HasValue && !await unitOfWork.ReadOnlyRepository<HrkAvatarTemplate>().Query()
            .AnyAsync(x => x.Id == config.DefaultAvatarTemplateId && x.IsEnabled, ct))
            throw new InvalidOperationException("Default avatar is unavailable.");
        var requirement = await levels.GetPlayerRequirementAsync(config.InitialLevel, ct);
        // The numeric player ID suffix makes names unique, even for simultaneous identical display names.
        var player = new HrkPlayer
        {
            UserId = account.UserId, PlayerName = "new-" + Guid.NewGuid().ToString("N"), Level = config.InitialLevel,
            MaxExp = requirement.IsMaxLevel ? 0 : requirement.RequiredExp,
            Stamina = config.InitialStamina, MaxStamina = config.MaxStamina, AvatarTemplateId = config.DefaultAvatarTemplateId
        };
        await unitOfWork.Repository<HrkPlayer>().AddAsync(player);
        await unitOfWork.SaveChangesAsync(ct);
        player.PlayerName = $"{account.DisplayName.Trim()} #{player.Id}";
        await unitOfWork.Repository<HrkPlayerWallet>().AddAsync(new()
        {
            PlayerId = player.Id, Gold = config.InitialGold, Diamonds = config.InitialDiamonds,
            UpgradeMaterials = config.InitialUpgradeMaterials, MaxCapacity = config.InventoryCapacity
        });
        var positioned = new Dictionary<int, HrkPlayerHero>();
        foreach (var entry in starterHeroes)
        {
            var template = await unitOfWork.ReadOnlyRepository<HrkHeroTemplate>().Query().SingleAsync(x => x.Id == entry.HeroTemplateId, ct);
            var hero = await heroes.CreateAsync(player.Id, template, ct);
            if (entry.FormationPosition is int position) positioned.Add(position, hero);
            await unitOfWork.Repository<HrkHeroAcquisitionHistory>().AddAsync(new()
            {
                RequestId = Guid.NewGuid(), PlayerId = player.Id, HeroTemplateId = template.Id,
                SourceType = "INITIALIZATION", SourceReferenceId = $"{receipt.RegistrationEventId:D}:{entry.Id}"
            });
        }
        await unitOfWork.SaveChangesAsync(ct);
        var formation = new HrkPlayerFormation
        {
            PlayerId = player.Id, FormationTemplateId = formationTemplate.Id, FormationName = formationTemplate.Name,
            IsSelected = true, 
            Position1 = positioned.GetValueOrDefault(1)?.Id, 
            Position2 = positioned.GetValueOrDefault(2)?.Id,
            Position3 = positioned.GetValueOrDefault(3)?.Id, 
            Position4 = positioned.GetValueOrDefault(4)?.Id,
            Position5 = positioned.GetValueOrDefault(5)?.Id, 
            TotalPower = positioned.Values.Sum(x => x.Power)
        };
        await unitOfWork.Repository<HrkPlayerFormation>().AddAsync(formation);
        var items = await unitOfWork.ReadOnlyRepository<HrkPlayerInitializationItem>().Query()
            .Where(x => x.ConfigId == config.Id).OrderBy(x => x.DisplayOrder).ThenBy(x => x.Id).ToListAsync(ct);
        var granted = await rewards.GrantAsync(player.Id, items.Select(x => new GenericRewardItemDto
        {
            Type = "ITEM", ItemTemplateId = x.ItemTemplateId, Quantity = x.Quantity
        }).ToList(), "INITIALIZATION", ct);
        if (granted.isBagFull) throw new InvalidOperationException("Starter inventory exceeds configured capacity.");
        await unitOfWork.SaveChangesAsync(ct);
        if (formationPower is not null)
            formation.TotalPower = (await formationPower.GetDefaultFormationPowerAsync(player.Id, ct)).TotalPower;
        activities.Raise(new PlayerInitializedEvent(player.Id, account.UserId, receipt.RegistrationEventId, config.Id, config.Version,
            new { config.Code, config.InitialGold, config.InitialDiamonds, config.InitialUpgradeMaterials,
                HeroCount = starterHeroes.Count, ItemTypeCount = items.Count }));
        return player.Id;
    }
}
