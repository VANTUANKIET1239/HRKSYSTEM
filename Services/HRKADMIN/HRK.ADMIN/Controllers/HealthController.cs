using Admin.Application.DTOs.HealthMonitoring;
using Admin.Application.Features.Queries.HealthMonitoring;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Application.CQRS;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRK.ADMIN.Controllers;

[ApiController]
[Authorize(Policy = "HealthAdmin")]
[Route("api/admin/health")]
public sealed class HealthController(ICqrsDispatcher dispatcher) : ControllerBase
{
    [HttpGet("services")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<ActionResult<BaseResponse<SystemHealthReport>>> GetServices(CancellationToken cancellationToken) =>
        Ok(await dispatcher.QueryAsync(new GetServicesHealthQuery(), cancellationToken));
}
