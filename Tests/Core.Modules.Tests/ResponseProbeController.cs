using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Application.DependencyInjection;
using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Common.Errors;
using Core.Web.Responses;
using Core.Web.Filters;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Core.Modules.Tests;

[ApiController]
[Route("response-probe")]
public sealed class ResponseProbeController(ISender sender) : ControllerBase
{
    [HttpGet("{mode}")]
    public IActionResult Get(string mode) => mode switch
    {
        "business" => throw new BusinessException("INSUFFICIENT_GOLD", "Không đủ Vàng."),
        "missing" => throw new BusinessException("HERO_NOT_FOUND", "Không tìm thấy võ tướng.", BusinessErrorKind.NotFound),
        "system" => throw new InvalidOperationException("secret-connection-string"),
        "unauthorized" => Unauthorized(),
        "claim-auth" => throw new UnauthorizedAccessException("Internal claim details"),
        "forbidden" => StatusCode(403),
        "typed-failure" => Ok(BaseResponse<int>.FailResponse("Item locked.", statusCode: 409, code: "ITEM_LOCKED")),
        "legacy-failure" => Ok(new BaseResponse<int> { Success = false, StatusCode = 404, Message = "Missing item." }),
        "empty" => NotFound(),
        "created" => StatusCode(201, new { success = false }),
        _ => Ok(BaseResponse<object>.SuccessResponse(new { success = false, outcome = "defeat" }))
    };

    [HttpPost("model-validation")]
    public IActionResult ModelValidation(RequiredNameRequest request) => throw new InvalidOperationException("Model validation failed to short-circuit.");

    [HttpPost("cqrs-validation")]
    public async Task<IActionResult> CqrsValidation(NameCommand request) => Ok(await sender.Send(request));
}
