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
[Route("compression-unmarked")]
public sealed class UnmarkedCompressionProbeController : ControllerBase
{
    [HttpGet("unmarked")]
    public IActionResult Unmarked() => Ok(new { name = "unmarked" });

}
