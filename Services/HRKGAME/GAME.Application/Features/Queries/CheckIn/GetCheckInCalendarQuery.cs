using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Features.Shared.CheckIn;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Queries.CheckIn;

public sealed record GetCheckInCalendarQuery(string UserId) : IQuery<BaseResponse<CheckInCalendarDto>>;
