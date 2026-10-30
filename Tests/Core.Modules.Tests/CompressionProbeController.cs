using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Core.Common.Errors;
using Core.Web.Compression;
using Core.Web.Filters;
using Core.Web.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Core.Modules.Tests;

[ApiController]
[Route("compression-probe")]
[HrkResponseCompression]
public sealed class CompressionProbeController : ControllerBase
{
    [HttpGet("disabled")]
    [HrkResponseCompression(Enabled = false)]
    public IActionResult Disabled() => Ok(new { name = "disabled" });

    [HttpGet("large")]
    public IActionResult Large() => Ok(Enumerable.Range(0, 4000).Select(sequence => new { sequence, name = "Chiêu mộ tướng", damage = 123 }));

    [HttpGet("business")]
    public IActionResult Business() => throw new BusinessException("ITEM_LOCKED", "Item locked.");

    [HttpGet("system")]
    public IActionResult SystemFailure() => throw new InvalidOperationException("secret");
}
