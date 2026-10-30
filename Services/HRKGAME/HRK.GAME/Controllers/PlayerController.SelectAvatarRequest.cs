using GAME.Application.Features.Queries.Player;
using HRK.GAME.Common;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GAME.Application.DTOs;
using GAME.Application.Features.Commands.PlayerAvatar;

namespace HRK.GAME.Controllers;
public sealed partial class PlayerController
{
    public sealed record SelectAvatarRequest(int AvatarTemplateId);
}
