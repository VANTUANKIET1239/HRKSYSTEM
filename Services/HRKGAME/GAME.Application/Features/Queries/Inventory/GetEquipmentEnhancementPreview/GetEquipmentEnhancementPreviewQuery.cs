using GAME.Application.Templates;
using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Inventory.GetEquipmentEnhancementPreview
{
    public record GetEquipmentEnhancementPreviewQuery(long InventoryItemId) : IQuery<BaseResponse<EquipmentEnhancementPreviewDto>>;
}
