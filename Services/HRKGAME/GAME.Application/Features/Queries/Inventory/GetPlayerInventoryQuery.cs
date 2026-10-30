using GAME.Application.Templates;
using Core.Persistence.Repositories;
using GAME.Application.Common.Mappings;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Inventory;

public record GetPlayerInventoryQuery(string? CategoryCode = null) : IQuery<BaseResponse<List<InventoryItemDto>>>;
