using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Tower;

namespace GAME.Application.Features.Queries.Tower;

public sealed record GetFloorDetailQuery(string UserId, int FloorNumber) : IQuery<BaseResponse<TowerFloorDetailDto>>;
