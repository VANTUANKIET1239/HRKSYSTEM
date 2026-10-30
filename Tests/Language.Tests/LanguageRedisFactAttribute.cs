using System.Globalization;
using Core.Caching;
using Core.Caching.Redis;
using Core.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Language.Tests;

public sealed class LanguageRedisFactAttribute : FactAttribute
{
    public LanguageRedisFactAttribute()
    { if (Environment.GetEnvironmentVariable("HRK_LANGUAGE_REDIS_TESTS") != "1") Skip = "Set HRK_LANGUAGE_REDIS_TESTS=1 and HRK_LANGUAGE_REDIS_CONNECTION to use an isolated key prefix."; }
}
