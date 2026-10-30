using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace GAME.Domain.Tests;

internal sealed class TestAsyncQuery<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncQuery(IEnumerable<T> enumerable) : base(enumerable)
    {
    }

    public TestAsyncQuery(Expression expression) : base(expression)
    {
    }

    IQueryProvider IQueryable.Provider => new TestAsyncProvider(this);

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
}
