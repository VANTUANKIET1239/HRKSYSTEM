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
    private sealed class StubReader : IPublishedLanguageReader
    {
        public Task<TranslationBundle?> GetAsync(string group, string culture, CancellationToken ct) => Task.FromResult<TranslationBundle?>(Bundle(7, "Hello"));
        public Task<TranslationBundle?> GetFromDatabaseAsync(string group, string culture, CancellationToken ct) => GetAsync(group, culture, ct);
    }
}
