using Core.Common.Errors;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using GAME.Application.Services;
using Xunit;

namespace GAME.Domain.Tests;
public partial class TowerReliabilityTests
{
    private sealed class Factory : IEquipmentInstanceFactory
    {
        public int Created { get; private set; }

        public Task<EquipmentInstanceCreationResult> CreateAsync(long playerId, HrkItemTemplate template, EquipmentAcquisitionContext context, CancellationToken cancellationToken = default)
        {
            Created++;
            return Task.FromResult(new EquipmentInstanceCreationResult { InventoryItem = new() { Id = Created, PlayerId = playerId, ItemTemplateId = template.Id, Count = 1, IsActive = true }, DroppedDto = new() { InventoryItemId = Created, Name = template.Name } });
        }
    }
}
