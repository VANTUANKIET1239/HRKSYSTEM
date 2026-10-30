using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.Player;
public sealed record GetPlayerInitializationQuery(string UserId) : IQuery<BaseResponse<PlayerInitializationStatusDto>>;
