using System.Text.Json;
using System.Globalization;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Events;
using GAME.Application.Features.Shared.CheckIn;
using GAME.Application.Interfaces;
using GAME.Application.Services;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Features.Commands.CheckIn;

public sealed record ClaimCheckInCommand(string UserId, Guid RequestId) : ICommand<BaseResponse<CheckInClaimResultDto>>;
