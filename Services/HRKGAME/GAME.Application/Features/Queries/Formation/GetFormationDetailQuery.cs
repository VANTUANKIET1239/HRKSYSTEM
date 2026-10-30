using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Formation;

namespace GAME.Application.Features.Queries.Formation;

public sealed record GetFormationDetailQuery(string UserId, string FormationCode) : IQuery<BaseResponse<FormationDetailDto?>>;
