using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.Summon;

namespace GAME.Application.Features.Queries.Summon;

public sealed record GetSummonConfigurationQuery(string UserId) : IQuery<BaseResponse<SummonConfigurationDto>>;
