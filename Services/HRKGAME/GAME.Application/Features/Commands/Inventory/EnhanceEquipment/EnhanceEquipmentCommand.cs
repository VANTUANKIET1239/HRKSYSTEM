using GAME.Application.Interfaces;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;

namespace GAME.Application.Features.Commands.Inventory.EnhanceEquipment
{
    public record EnhanceEquipmentCommand(EnhanceEquipmentRequestDto Request) : ICommand<BaseResponse<EnhanceEquipmentResultDto>>, IPlayerPowerChangeCommand
    {
        string? IPlayerPowerChangeCommand.UserId => null;
    }
}
