using Core.Persistence.Repositories;
using Microsoft.Data.SqlClient;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using GAME.Application.Common;
using GAME.Application.Events;
using Core.Messaging.Contracts;
using Core.TransactionalMessaging.Outbox;
using System.Text.Json;

namespace GAME.Application.Services;

public sealed record EventChestSnapshot(int Id, int FloorNumber, string? RewardsJson,
    string ChestName = "Milestone chest", string? ChestIcon = null, string? Description = null)
{
    public HrkTowerChestConfig ToConfiguration(int eventId) => new()
    {
        Id = Id, EventId = eventId, FloorNumber = FloorNumber, RewardsJson = RewardsJson,
        ChestName = ChestName, ChestIcon = ChestIcon, Description = Description, IsActive = true
    };
}
