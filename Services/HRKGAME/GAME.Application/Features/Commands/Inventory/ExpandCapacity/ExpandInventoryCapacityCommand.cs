using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Configuration;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.Extensions.Options;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Commands.Inventory.ExpandCapacity;

public record ExpandInventoryCapacityCommand(int SlotsToAdd, Guid RequestId = default) : ICommand<BaseResponse<PlayerWalletDto>>, IPlayerPowerChangeCommand
{
    string? IPlayerPowerChangeCommand.UserId => null;
}
