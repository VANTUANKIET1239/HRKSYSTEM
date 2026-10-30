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

internal sealed class MemoryTranslations : ITranslationCache
{
    public Dictionary<(string,string), TranslationBundle> Bundles { get; } = new();
    public bool Failure { get; init; }
    public Task<TranslationBundle?> GetCurrentAsync(string group, string culture, CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); if (Failure) throw new IOException("Redis offline"); return Task.FromResult(Bundles.GetValueOrDefault((group,culture))); }
    public Task PublishAsync(TranslationBundle bundle, CancellationToken ct) { Bundles[(bundle.Group,bundle.Culture)] = bundle; return Task.CompletedTask; }
}
