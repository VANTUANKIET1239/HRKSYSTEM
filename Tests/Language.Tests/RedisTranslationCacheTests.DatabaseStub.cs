using System.Reflection;
using System.Text.Json;
using Core.Caching.Redis;
using Core.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Language.Tests;
public sealed partial class RedisTranslationCacheTests
{
    public class DatabaseStub : DispatchProxy
    {
        public Func<MethodInfo, object? []?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object? []? args) => Handler(targetMethod!, args);
    }
}
