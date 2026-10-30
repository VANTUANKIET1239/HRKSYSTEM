using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.Features.Shared.PlayerAvatar;

namespace GAME.Application.Features.Commands.PlayerAvatar;

public sealed record SavePlayerCustomAvatarCommand(string UserId, CustomAvatarUploadDto Upload) : ICommand<BaseResponse<PlayerProfileDto>>;
