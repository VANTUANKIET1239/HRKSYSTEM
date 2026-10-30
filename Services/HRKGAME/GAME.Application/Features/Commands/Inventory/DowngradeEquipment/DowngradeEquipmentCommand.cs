using Core.Common.Errors;
using GAME.Application.Templates;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Features.Queries.Inventory.GetEquipmentDowngradePreview;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Application.Features.Commands.Inventory.DowngradeEquipment;

public record DowngradeEquipmentCommand(DowngradeEquipmentRequestDto Request) : ICommand<BaseResponse<EquipmentDowngradePreviewDto>>, IPlayerPowerChangeCommand
{
    string? IPlayerPowerChangeCommand.UserId => null;
}
