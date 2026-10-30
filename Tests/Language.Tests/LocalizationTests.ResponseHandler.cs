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
public sealed partial class LocalizationTests
{
    private sealed class ResponseHandler(Func<HttpRequestMessage, TranslationBundle> response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(response(request)) });
        }
    }
}
