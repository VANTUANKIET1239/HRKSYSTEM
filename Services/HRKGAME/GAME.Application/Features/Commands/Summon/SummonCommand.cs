using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.Common;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Services;
using GAME.Application.Features.Shared.Summon;

namespace GAME.Application.Features.Commands.Summon;

public sealed record SummonCommand(string UserId, SummonRequestDto Request) : ICommand<BaseResponse<SummonResultDto>>;
