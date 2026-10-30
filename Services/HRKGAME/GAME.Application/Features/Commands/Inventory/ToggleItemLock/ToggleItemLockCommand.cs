using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Commands.Inventory.ToggleItemLock;

public record ToggleItemLockCommand(long InventoryItemId, bool IsLocked) : ICommand<BaseResponse<bool>>;
