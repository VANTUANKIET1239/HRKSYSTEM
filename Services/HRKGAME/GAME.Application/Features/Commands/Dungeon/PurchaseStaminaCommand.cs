using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Dungeon;

namespace GAME.Application.Features.Commands.Dungeon;

public sealed record PurchaseStaminaCommand(string UserId, Guid RequestId = default) : ICommand<BaseResponse<DungeonStaminaDto>>, IPlayerPowerChangeCommand;
