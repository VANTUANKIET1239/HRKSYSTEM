using System.Net;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using Core.Common.Errors;
using Core.InternalApi;
using Core.Localization;
using Core.Web.Filters;
using Core.Web.Responses;
using Core.Web.Compression;
using HRK.LANGUAGE.Controllers;
using Language.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Language.Tests;

[ApiController, Route("localization-probe")]
public sealed class LocalizationProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => throw new BusinessException("LEVEL", "Cần đạt cấp 8.")
    { Arguments = new Dictionary<string,object?> { ["level"] = 8 } };
    [HttpGet("interpolated")]
    public IActionResult Interpolated() => throw BusinessException.Localized("BALANCE", $"Cần {1234:N0} Vàng, hiện có {500:N0}.");
}
