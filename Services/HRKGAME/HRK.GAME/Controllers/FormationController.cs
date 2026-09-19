using Core.Common.Entity.MyCompany.Shared.Responses;
using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace HRK.GAME.Controllers
{
    [Authorize]
    [Route("api/formations")]
    [Route("api/formation")]
    public class FormationController : HRKControllerBase
    {
        private readonly IFormationService _formationService;

        public FormationController(IFormationService formationService)
        {
            _formationService = formationService;
        }

        [HttpGet]
        [HttpGet("list")]
        public async Task<IActionResult> GetFormations(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var formations = await _formationService.GetPlayerFormationsAsync(userId, cancellationToken);
            return OkResponse(formations);
        }

        [HttpGet("{formationCode}")]
        public async Task<IActionResult> GetFormationDetail(string formationCode, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var detail = await _formationService.GetFormationDetailAsync(userId, formationCode, cancellationToken);
            if (detail == null)
                return NotFoundResponse($"Không tìm thấy thông tin trận pháp '{formationCode}'.");

            return OkResponse(detail);
        }

        [HttpPut("{formationCode}/positions")]
        public async Task<IActionResult> UpdatePositions(
            string formationCode,
            [FromBody] UpdateFormationPositionsRequest request,
            CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var updated = await _formationService.UpdatePositionsAsync(userId, formationCode, request, cancellationToken);
                return OkResponse(updated, "Cập nhật đội hình thành công.");
            }
            catch (ArgumentException ex)
            {
                return ErrorResponse(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
        }

        [HttpPut("{formationCode}/select")]
        public async Task<IActionResult> SelectFormation(string formationCode, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _formationService.SelectFormationAsync(userId, formationCode, cancellationToken);
                return OkResponse(result, $"Đã kích hoạt trận pháp '{result.Name}' cho chiến trận.");
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
        }

        [HttpPost("{formationCode}/upgrade")]
        public async Task<IActionResult> UpgradeFormation(string formationCode, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _formationService.UpgradeFormationAsync(userId, formationCode, cancellationToken);
                return OkResponse(result, $"Nâng cấp trận pháp lên Cấp {result.NewLevel} thành công!");
            }
            catch (InvalidOperationException ex)
            {
                return ErrorResponse(ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFoundResponse(ex.Message);
            }
        }

        [HttpGet("main")]
        public async Task<IActionResult> GetLegacyFormation(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var formation = await _formationService.GetPlayerFormationAsync(userId, "Main Team", cancellationToken);
            return OkResponse(formation);
        }
    }
}
