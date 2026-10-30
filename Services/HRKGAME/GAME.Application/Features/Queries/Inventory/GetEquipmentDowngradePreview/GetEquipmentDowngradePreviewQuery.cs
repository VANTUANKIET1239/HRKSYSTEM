using GAME.Application.Common;
using GAME.Application.Templates;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Inventory.GetEquipmentDowngradePreview;

public record GetEquipmentDowngradePreviewQuery(long InventoryItemId, int TargetLevel) : IQuery<BaseResponse<EquipmentDowngradePreviewDto>>;
