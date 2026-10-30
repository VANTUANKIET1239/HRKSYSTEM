using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace GAME.Domain.Tests;

internal sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
{
    public T Current => inner.Current;

    public ValueTask DisposeAsync()
    {
        inner.Dispose();
        return ValueTask.CompletedTask;
    }

    public ValueTask<bool> MoveNextAsync() => ValueTask.FromResult(inner.MoveNext());
}
