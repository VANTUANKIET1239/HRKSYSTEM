using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Shared.CheckIn;

internal sealed record CurrentCheckInCalendar(HrkCheckInCalendar Calendar, DateOnly GameDate, DateTime NextResetUtc,
    IReadOnlyDictionary<int, (HrkCheckInRewardConfig Config, List<GenericRewardItemDto> Rewards)> Days);
