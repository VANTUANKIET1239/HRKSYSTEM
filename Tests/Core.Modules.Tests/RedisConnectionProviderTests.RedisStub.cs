using System.Reflection;
using Core.Caching;
using Core.Caching.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Xunit;

namespace Core.Modules.Tests;
public sealed partial class RedisConnectionProviderTests
{
    public class RedisStub : DispatchProxy
    {
        public Func<MethodInfo, object? []?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object? []? args) => Handler(targetMethod!, args);
    }
}
