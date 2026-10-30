using GAME.Application.Templates;
using Core.Common.Errors;
using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerAvatar;

namespace GAME.Application.Features.Commands.PlayerAvatar;

public sealed record SelectPlayerAvatarCommand(string UserId, int AvatarTemplateId) : ICommand<BaseResponse<PlayerProfileDto>>;
