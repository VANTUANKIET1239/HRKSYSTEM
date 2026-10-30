using GAME.Application.Common;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;

namespace GAME.Application.Features.Queries.Player
{
    public record GetPlayerWalletQuery : IQuery<BaseResponse<PlayerWalletDto>>;
}
