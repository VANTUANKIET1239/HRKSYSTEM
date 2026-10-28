using Core.Common.Entity.MyCompany.Shared.Responses;
using Core.Localization;
using Core.Common.Errors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Core.Web.Responses;

public static class ApiResponseLocalizer
{
    public static string ResolveCulture(HttpContext context, LocalizationOptions options)
    {
        // Explicit client preference takes precedence; fall back to the shared preference cookie.
        IList<Microsoft.Net.Http.Headers.StringWithQualityHeaderValue>? languages;
        try { languages = context.Request.GetTypedHeaders().AcceptLanguage; }
        catch (FormatException) { languages = null; }
        foreach (var language in (languages ?? []).OrderByDescending(item => item.Quality ?? 1))
            if ((language.Quality ?? 1) > 0 && TranslationIdentity.Culture(language.Value.Value) is { } supported)
                return supported;
        return TranslationIdentity.Culture(context.Request.Cookies[options.CookieName]) ?? options.DefaultCulture;
    }

    public static async Task ApplyAsync(IApiResponse response, HttpContext context)
    {
        var localizer = context.RequestServices.GetService<ITextLocalizer>();
        var options = context.RequestServices.GetService<IOptions<LocalizationOptions>>()?.Value;
        if (localizer is null || options is null || !options.Enabled) return;
        var culture = ResolveCulture(context, options);
        // A failed translation dependency must not hold an error response for multiple per-bundle timeouts.
        using var budget = CancellationTokenSource.CreateLinkedTokenSource(context.RequestAborted);
        budget.CancelAfter(TimeSpan.FromSeconds(options.ApiTimeoutSeconds));
        try
        {
            var metadata = context.Items[typeof(BusinessException)] as BusinessException;
            var messageKey = metadata?.MessageKey ?? (metadata?.MessageTemplate is { } template
                ? TranslationIdentity.TextKey(template) : response.Error?.Code);

            async Task<string> Translate(string text, string? key = null) {
                return await localizer.GetAsync(
                options.Group, culture, key ?? TranslationIdentity.TextKey(text), text,
                metadata?.Arguments, budget.Token);
            }

            
            var original = response.Message;
            var messageResult = original;
            if (!string.IsNullOrWhiteSpace(original))
                messageResult = await Translate(original, messageKey);
            List<string>? errorsResult = null;
            if (response.Errors is { } errors)
            {
                var translated = new List<string>();
                foreach (var error in errors)
                    translated.Add(error == original ? messageResult! : await Translate(error));
                errorsResult = translated;
            }
            var detailResult = response.Error;
            if (response.Error is { } detail)
            {
                Dictionary<string, string[]>? validation = null;
                if (detail.ValidationErrors is { } fields)
                {
                    validation = new();
                    foreach (var (name, messages) in fields)
                    {
                        var values = new List<string>();
                        foreach (var message in messages) values.Add(await Translate(message));
                        validation[name] = values.ToArray();
                    }
                }
                detailResult = detail with
                {
                    Message = detail.Message == original ? messageResult! : await Translate(detail.Message, messageKey ?? detail.Code),
                    ValidationErrors = validation
                };
            }
            response.Message = messageResult;
            response.Errors = errorsResult;
            response.Error = detailResult;
            context.Response.Headers.ContentLanguage = culture;
            var vary = context.Response.Headers.GetCommaSeparatedValues("Vary");
            if (!vary.Contains("*", StringComparer.OrdinalIgnoreCase))
                foreach (var name in new[] { "Accept-Language", "Cookie" })
                    if (!vary.Contains(name, StringComparer.OrdinalIgnoreCase))
                        context.Response.Headers.AppendCommaSeparatedValues("Vary", name);
        }
        catch (OperationCanceledException) when (!context.RequestAborted.IsCancellationRequested)
        {
            // Preserve the safe original response if localization exceeds its total budget.
        }
    }
}
