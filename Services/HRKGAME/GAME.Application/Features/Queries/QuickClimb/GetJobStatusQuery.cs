using static GAME.Application.Features.Shared.QuickClimb.QuickClimbProjection;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;

namespace GAME.Application.Features.Queries.QuickClimb;

public sealed record GetJobStatusQuery(string UserId, string JobId) : IQuery<BaseResponse<QuickClimbJobStatusDto>>;
