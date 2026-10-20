using GAME.Application.Templates;
using Core.Application.CQRS;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Application.Features.Shared.Formation;

namespace GAME.Application.Features.Commands.Formation;

public sealed record InitializePlayerFormationsCommand(string UserId) : ICommand<BaseResponse<bool>>;

public sealed class InitializePlayerFormationsCommandHandler(
    IUnitOfWork unitOfWork,
    IGamePlayerService players,
    IFormationStatService formationStats,
    ICombatPowerService power,
    IHeroStatCalculationService heroStats, IGameTemplateCatalog? templates = null)
    : FormationCommandHandlerSupport(unitOfWork, players, formationStats, power, heroStats, templates), ICommandHandler<InitializePlayerFormationsCommand, BaseResponse<bool>>
{
    public async Task<BaseResponse<bool>> Handle(InitializePlayerFormationsCommand message, CancellationToken ct)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var player = await GetPlayerRequiredAsync(message.UserId, ct);
            await EnsurePlayerFormationsInitializedAsync(player, ct);
        }, ct);
        return BaseResponse<bool>.SuccessResponse(true);
    }
}
