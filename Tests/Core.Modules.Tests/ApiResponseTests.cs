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

public class ApiResponseTests
{
    private static TestServer CreateServer(CapturedLogs logs) => new(new WebHostBuilder()
        .ConfigureServices(services =>
        {
            services.AddLogging(builder => builder.ClearProviders().AddProvider(logs));
            services.AddControllers(options => options.AddHrkApiFilters()).AddApplicationPart(typeof(ResponseProbeController).Assembly);
            services.AddCoreApplication(typeof(ResponseProbeController).Assembly);
            services.AddHrkApiResponses();
        })
        .Configure(app =>
        {
            app.UseHrkApiResponses();
            app.UseRouting();
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));

    [Theory]
    [InlineData("business", 409, "INSUFFICIENT_GOLD", "business")]
    [InlineData("missing", 404, "HERO_NOT_FOUND", "business")]
    [InlineData("unauthorized", 401, "UNAUTHORIZED", "auth")]
    [InlineData("claim-auth", 401, "UNAUTHORIZED", "auth")]
    [InlineData("forbidden", 403, "FORBIDDEN", "auth")]
    [InlineData("typed-failure", 409, "ITEM_LOCKED", "business")]
    [InlineData("legacy-failure", 404, "NOT_FOUND", "business")]
    [InlineData("empty", 404, "NOT_FOUND", "business")]
    public async Task ExpectedFailures_HaveConsistentHttpAndEnvelope(string mode, int status, string code, string type)
    {
        var logs = new CapturedLogs();
        using var server = CreateServer(logs);
        using var response = await server.CreateClient().GetAsync("/response-probe/" + mode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(status, json.GetProperty("statusCode").GetInt32());
        Assert.False(json.GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("data").ValueKind);
        Assert.Equal(code, json.GetProperty("error").GetProperty("code").GetString());
        Assert.Equal(type, json.GetProperty("error").GetProperty("type").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.GetProperty("traceId").GetString()));
        Assert.DoesNotContain(logs.Entries, entry => entry.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task UnexpectedFailure_IsLoggedAndRedacted()
    {
        var logs = new CapturedLogs();
        using var server = CreateServer(logs);
        using var response = await server.CreateClient().GetAsync("/response-probe/system");
        var text = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(text);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("INTERNAL_SERVER_ERROR", json.GetProperty("error").GetProperty("code").GetString());
        Assert.DoesNotContain("secret-connection-string", text);
        Assert.Contains(logs.Entries, entry => entry.Level >= LogLevel.Error && entry.Exception != null);
    }

    [Theory]
    [InlineData("gameplay", 200)]
    [InlineData("created", 201)]
    public async Task SuccessfulRequests_PreserveDataAndHttpStatus(string mode, int status)
    {
        using var server = CreateServer(new CapturedLogs());
        using var response = await server.CreateClient().GetAsync("/response-probe/" + mode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal(status, json.GetProperty("statusCode").GetInt32());
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.False(json.GetProperty("data").GetProperty("success").GetBoolean());
        Assert.Equal(JsonValueKind.Null, json.GetProperty("error").ValueKind);
    }

    [Theory]
    [InlineData("model-validation")]
    [InlineData("cqrs-validation")]
    public async Task Validation_IsRejectedBeforeHandlerAndIncludesFields(string route)
    {
        var logs = new CapturedLogs();
        using var server = CreateServer(logs);
        using var response = await server.CreateClient().PostAsJsonAsync("/response-probe/" + route, new { name = "" });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("VALIDATION_ERROR", json.GetProperty("error").GetProperty("code").GetString());
        Assert.True(json.GetProperty("error").GetProperty("validationErrors").TryGetProperty("Name", out _));
        Assert.DoesNotContain(logs.Entries, entry => entry.Level >= LogLevel.Error);
    }

    [Fact]
    public void UnauthorizedFactory_IsFailure()
    {
        var result = BaseResponse<string>.UnauthorizedResponse("discarded");
        Assert.False(result.Success);
        Assert.Null(result.Data);
        Assert.Equal("UNAUTHORIZED", result.Error!.Code);
    }

    [Fact]
    public async Task UnsupportedMethod_HasMatchingErrorEnvelope()
    {
        using var server = CreateServer(new CapturedLogs());
        using var response = await server.CreateClient().PostAsJsonAsync("/response-probe/business", new { });
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal(405, json.GetProperty("statusCode").GetInt32());
        Assert.Equal("METHOD_NOT_ALLOWED", json.GetProperty("error").GetProperty("code").GetString());
    }
}

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

public sealed class RequiredNameRequest
{
    [Required] public string Name { get; set; } = "";
}
public sealed record NameCommand(string Name) : IRequest<BaseResponse<string>>;
public sealed class NameCommandValidator : AbstractValidator<NameCommand>
{
    public NameCommandValidator() => RuleFor(request => request.Name).NotEmpty();
}
public sealed class NameCommandHandler : IRequestHandler<NameCommand, BaseResponse<string>>
{
    public Task<BaseResponse<string>> Handle(NameCommand request, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Invalid request reached the mutating handler.");
}

public sealed class CapturedLogs : ILoggerProvider
{
    public ConcurrentQueue<(LogLevel Level, Exception? Exception)> Entries { get; } = new();
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(Entries);
    public void Dispose() { }
    private sealed class CaptureLogger(ConcurrentQueue<(LogLevel Level, Exception? Exception)> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => entries.Enqueue((logLevel, exception));
    }
}
