using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Core.Persistence.Repositories;
using GAME.Infrastructure.Services;
using Microsoft.AspNetCore.Http;

namespace GAME.Domain.Tests;

internal static class TestGamePlayers
{
    // These tests exercise only the actual player/wallet lookups, without a request context.
    public static GamePlayerService Create(IUnitOfWork unitOfWork) => new(unitOfWork, new HttpContextAccessor(), null!);
}
