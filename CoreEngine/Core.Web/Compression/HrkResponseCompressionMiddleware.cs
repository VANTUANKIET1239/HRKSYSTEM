using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Core.Web.Compression;

public sealed class HrkResponseCompressionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<HrkResponseCompressionOptions> _options;
    private readonly ILogger<HrkResponseCompressionMiddleware> _logger;
    private readonly ResponseCompressionMiddleware _compression;

    public HrkResponseCompressionMiddleware(RequestDelegate next, IOptionsMonitor<HrkResponseCompressionOptions> options,
        ILogger<HrkResponseCompressionMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
        _compression = new ResponseCompressionMiddleware(InvokeBufferedAsync, new HrkCompressionProvider());
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var attribute = context.GetEndpoint()?.Metadata.GetMetadata<HrkResponseCompressionAttribute>();
        var options = _options.CurrentValue;
        if (attribute is not { Enabled: true } || !options.Enabled || context.Response.HasStarted ||
            (context.Request.IsHttps && !options.EnableForHttps) || HttpMethods.IsHead(context.Request.Method) ||
            context.Request.Headers.ContainsKey(HeaderNames.Range))
        {
            await _next(context);
            return;
        }
        if (!options.Policies.TryGetValue(attribute.Policy, out var policy))
            throw new InvalidOperationException($"Unknown response compression policy '{attribute.Policy}'.");
        if (!policy.Enabled)
        {
            await _next(context);
            return;
        }

        // Vary is needed on the identity response too, including requests without Accept-Encoding.
        context.Response.OnStarting(() =>
        {
            var vary = context.Response.Headers.GetCommaSeparatedValues(HeaderNames.Vary);
            if (!vary.Contains("*", StringComparer.OrdinalIgnoreCase) && !vary.Contains(HeaderNames.AcceptEncoding, StringComparer.OrdinalIgnoreCase))
                context.Response.Headers.AppendCommaSeparatedValues(HeaderNames.Vary, HeaderNames.AcceptEncoding);
            return Task.CompletedTask;
        });
        var (encoding, identityAllowed) = HrkCompressionProvider.Negotiate(context, policy);
        if (encoding is null)
        {
            if (!identityAllowed)
            {
                context.Response.StatusCode = StatusCodes.Status406NotAcceptable;
                return;
            }
            await _next(context);
            return;
        }
        var state = new HrkCompressionState(new CompressionPolicySnapshot(attribute.Policy, policy.Level,
            policy.MinimumBodyBytes, policy.SlowCompressionMilliseconds), encoding, identityAllowed);
        var priorState = context.Features.Get<HrkCompressionState>();
        context.Features.Set(state);
        try
        {
            await _compression.Invoke(context);
            if (state.CompressionStarted != 0)
            {
                var elapsed = Stopwatch.GetElapsedTime(state.CompressionStarted).TotalMilliseconds;
                if (state.Policy.SlowCompressionMilliseconds > 0 && elapsed >= state.Policy.SlowCompressionMilliseconds)
                    _logger.LogInformation("Response compression phase took {ElapsedMilliseconds} ms for {Policy} using {Encoding}; {BodyBytes} input bytes; trace {TraceId}",
                        elapsed, state.Policy.Name, state.Encoding, state.BodyBytes, context.TraceIdentifier);
            }
        }
        finally
        {
            context.Features.Set(priorState);
        }
    }

    private async Task InvokeBufferedAsync(HttpContext context)
    {
        var original = context.Features.Get<IHttpResponseBodyFeature>()!;
        using var threshold = new ThresholdResponseBodyFeature(original, context.Features.Get<HrkCompressionState>()!);
        context.Features.Set<IHttpResponseBodyFeature>(threshold);
        try
        {
            await _next(context);
            await threshold.FinishAsync(context.RequestAborted);
        }
        catch (Exception error)
        {
            await threshold.AbortAsync(error);
            throw;
        }
        finally
        {
            context.Features.Set(original);
        }
    }
}
