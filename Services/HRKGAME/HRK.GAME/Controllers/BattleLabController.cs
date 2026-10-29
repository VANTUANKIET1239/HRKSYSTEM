using Core.Application.CQRS;
using Core.Web.Compression;
using GAME.Application.Features.Commands.BattleLab;
using GAME.Application.Features.Queries.BattleLab;
using GAME.Application.DTOs;
using Core.Common.Entity.MyCompany.Shared.Responses;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/battle/lab")]
public sealed class BattleLabController(
    ICqrsDispatcher dispatcher,
    IWebHostEnvironment environment,
    IConfiguration configuration)
    : HRKControllerBase
{
    private static readonly SemaphoreSlim RunGate = new(1, 1);
    // Disabled outside Development unless explicitly enabled AND the caller is Admin.
    private bool CanUseLab => environment.IsDevelopment() || (configuration.GetValue<bool>("BattleLab:Enabled") && User.IsInRole("Admin"));

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken ct)
    {
        if (!CanUseLab)
            return NotFound();
        return HrkOk(await dispatcher.QueryAsync(new GetBattleLabCatalogQuery(), ct));
    }

    [HttpPost("run")]
    [HrkResponseCompression("BattleReplay")]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> Run(BattleLabRequestDto request, CancellationToken ct)
    {
        if (!CanUseLab)
            return NotFound();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (!await RunGate.WaitAsync(0, ct))
            return StatusCode(429, BaseResponse<object>.FailResponse("Another lab batch is running. Please retry.", statusCode: 429, code: "LAB_BUSY"));
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try
        {
            return HrkOk(await dispatcher.CommandAsync(new RunBattleLabCommand(request), timeout.Token));
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return StatusCode(408, BaseResponse<object>.FailResponse("Lab timed out. Reduce battle count or rounds.", statusCode: 408, code: "LAB_TIMEOUT"));
        }
        finally
        {
            RunGate.Release();
        }
    }
}
