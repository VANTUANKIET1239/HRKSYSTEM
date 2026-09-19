using Core.Common.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using CoreEngine.CQRS;
using GAME.Application.DTOs;

namespace GAME.Application.Features.Commands.Inventory.EnhanceEquipment
{
    public record EnhanceEquipmentCommand(EnhanceEquipmentRequestDto Request) : ICommand<BaseResponse<EnhanceEquipmentResultDto>>;
}

