using Core.Persistence.Repositories;
using GAME.Application.Common.Helpers;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.Battle;

public record GetBattleLogsQuery(string BattleId) : IQuery<BaseResponse<List<BattleLogDto>>>;
