using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/game-config")]
    public class GameConfigController : HRKControllerBase
    {
        private readonly IGameFeatureConfigService _featureConfigService;
        private readonly ICombatPowerService _combatPowerService;

        public GameConfigController(IGameFeatureConfigService featureConfigService, ICombatPowerService combatPowerService)
        {
            _featureConfigService = featureConfigService;
            _combatPowerService = combatPowerService;
        }

        [HttpGet("features")]
        public async Task<IActionResult> GetFeatures(CancellationToken cancellationToken) =>
            HrkOk(BaseResponse<List<GameFeatureConfigDto>>.SuccessResponse(
                await _featureConfigService.GetFeatureTreeAsync(cancellationToken)));

        [HttpGet("combat-power")]
        public async Task<IActionResult> GetCombatPowerConfigs(CancellationToken cancellationToken) =>
            HrkOk(BaseResponse<List<CombatPowerConfigDto>>.SuccessResponse(
                await _combatPowerService.GetConfigsAsync(cancellationToken)));
    }
}
