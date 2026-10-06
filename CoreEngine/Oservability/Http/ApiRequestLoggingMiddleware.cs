using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Oservability.Http;

public sealed class ApiRequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<ApiRequestLoggingMiddleware> logger,
    IOptionsMonitor<ApiLoggingOptions> optionsMonitor)
{
    public async Task InvokeAsync(HttpContext http)
    {
        var options = optionsMonitor.CurrentValue;
        if (!options.Enabled || options.ExcludedPaths.Any(p => http.Request.Path.StartsWithSegments(p)))
        {
            await next(http);
            return;
        }

        // Reuse the ASP.NET request Activity. Fallback only when the host has none.
        using var fallback = Activity.Current is null
            ? new Activity("HTTP request").SetIdFormat(ActivityIdFormat.W3C).Start() : null;
        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["RequestId"] = http.TraceIdentifier,
            ["HttpRequestId"] = http.TraceIdentifier,
            ["RequestMethod"] = http.Request.Method,
            ["RequestPath"] = http.Request.Path.Value
        });
        var started = Stopwatch.GetTimestamp();
        logger.LogInformation("{EventName}: {RequestMethod} {RequestPath}",
            "ApiStarted", http.Request.Method, http.Request.Path.Value);

        Exception? failure = null;
        var originalBody = http.Response.Body;
        BoundedResponseCaptureStream? capture = null;
        try
        {
            var logBody = logger.IsEnabled(LogLevel.Debug) && options.AllowedBodyFields.Length > 0
                && options.BodyPaths.Any(p => Matches(http.Request.Path, p))
                && !http.WebSockets.IsWebSocketRequest;
            if (logBody && options.LogRequestBody)
                await LogInputAsync(http, options);
            if (logBody && options.LogResponseBody)
                http.Response.Body = capture = new(originalBody, options.MaxBodyBytes);
            await next(http);
        }
        catch (Exception exception)
        {
            failure = exception;
            throw;
        }
        finally
        {
            http.Response.Body = originalBody;
            if (capture is not null)
            {
                if (failure is null)
                    logger.LogDebug("{EventName}: {@ResponseBody}", "ApiOutput",
                        ApiBodySanitizer.Describe(capture.Captured, capture.Oversized, http.Response.ContentType, options));
                capture.Dispose();
            }
            var aborted = http.RequestAborted.IsCancellationRequested;
            // A propagated exception can still have the default 200 here. Report a
            // server error without altering the response or swallowing the exception.
            var status = failure is not null && !aborted && !http.Response.HasStarted
                ? StatusCodes.Status500InternalServerError : http.Response.StatusCode;
            var outcome = aborted ? "Aborted" : failure is not null || status >= 500 ? "ServerError"
                : status >= 400 ? "ClientError" : "Success";
            var level = outcome == "ServerError" ? LogLevel.Error
                : outcome is "ClientError" or "Aborted" ? LogLevel.Warning : LogLevel.Information;
            using var identity = logger.BeginScope(new Dictionary<string, object?>
            {
                ["UserId"] = http.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? http.User.FindFirstValue("sub"),
                ["ResponseStarted"] = http.Response.HasStarted
            });
            logger.Log(level, aborted ? null : failure,
                "{EventName}: {RequestMethod} {RequestPath} responded {StatusCode} in {DurationMs} ms with {Outcome}",
                "ApiEnded", http.Request.Method, http.Request.Path.Value, status,
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, outcome);
        }
    }

    private async Task LogInputAsync(HttpContext http, ApiLoggingOptions options)
    {
        if (http.Request.ContentLength > options.MaxBodyBytes)
        {
            logger.LogDebug("{EventName}: {OmittedReason}", "ApiInput", "BodyTooLarge");
            return;
        }
        var mediaType = http.Request.ContentType?.Split(';')[0].Trim();
        if (mediaType is null || !(mediaType.Equals("application/json", StringComparison.OrdinalIgnoreCase)
            || mediaType.EndsWith("+json", StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogDebug("{EventName}: {OmittedReason}", "ApiInput", "NonJsonBody");
            return;
        }
        http.Request.EnableBuffering();
        var position = http.Request.Body.Position;
        try
        {
            var bytes = new byte[options.MaxBodyBytes + 1];
            var length = 0;
            while (length < bytes.Length)
            {
                var read = await http.Request.Body.ReadAsync(bytes.AsMemory(length), http.RequestAborted);
                if (read == 0) break;
                length += read;
            }
            logger.LogDebug("{EventName}: {@RequestBody}", "ApiInput",
                ApiBodySanitizer.Describe(bytes[..length], length > options.MaxBodyBytes,
                    http.Request.ContentType, options));
        }
        catch (IOException)
        {
            logger.LogDebug("{EventName}: {OmittedReason}", "ApiInput", "BodyReadFailed");
        }
        finally { http.Request.Body.Position = position; }
    }

    private static bool Matches(PathString path, string pattern) => pattern.EndsWith("/*", StringComparison.Ordinal)
        ? path.StartsWithSegments(pattern[..^2])
        : string.Equals(path.Value, pattern, StringComparison.OrdinalIgnoreCase);
}
