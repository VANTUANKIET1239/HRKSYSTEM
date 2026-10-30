using Core.Persistence.Repositories;
using GAME.Application.Templates;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Inventory.GetPlayerEquipment;

public record GetPlayerEquipmentQuery(string? CategoryCode = null, bool IncludeEquipped = false) : IQuery<BaseResponse<List<InventoryItemDto>>>;
