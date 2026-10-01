using GAME.Application.DTOs;
using GAME.Application.Interfaces;
using HRK.GAME.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.GAME.Controllers;

[Authorize]
[Route("api/battle/lab")]
public sealed class BattleLabController(IBattleLabService lab, IWebHostEnvironment environment,
    IConfiguration configuration) : HRKControllerBase
{
    private static readonly SemaphoreSlim RunGate = new(1, 1);
    // Disabled outside Development unless explicitly enabled AND the caller is Admin.
    private bool CanUseLab => environment.IsDevelopment() ||
        (configuration.GetValue<bool>("BattleLab:Enabled") && User.IsInRole("Admin"));

    [HttpGet("catalog")]
    public async Task<IActionResult> Catalog(CancellationToken ct)
    {
        if (!CanUseLab) return NotFound();
        return OkResponse(await lab.GetCatalogAsync(ct));
    }

    [HttpPost("run")]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> Run(BattleLabRequestDto request, CancellationToken ct)
    {
        if (!CanUseLab) return NotFound();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        if (!await RunGate.WaitAsync(0, ct))
            return StatusCode(429, new { success = false, message = "Another lab batch is running. Please retry." });
        timeout.CancelAfter(TimeSpan.FromSeconds(45));
        try { return OkResponse(await lab.RunAsync(request, timeout.Token)); }
        catch (ArgumentException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (InvalidOperationException ex) { return BadRequest(new { success = false, message = ex.Message }); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        { return StatusCode(408, new { success = false, message = "Lab timed out. Reduce battle count or rounds." }); }
        finally { RunGate.Release(); }
    }
}
