using Core.Persistence.Repositories;
using GAME.Application.Interfaces;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace GAME.Infrastructure.Services;

public class GamePlayerService : IGamePlayerService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContext;
    private readonly IDiagnosticContext _diagnostic;
    public GamePlayerService(IUnitOfWork unitOfWork, IHttpContextAccessor httpContext, IDiagnosticContext diagnostic)
    {
        _unitOfWork = unitOfWork;
        _httpContext = httpContext;
        _diagnostic = diagnostic;
    }

    public async Task<HrkPlayer?> GetPlayerByUserIdAsync(string userId, CancellationToken cancellationToken = default, bool tracked = false)
    {
        var query = tracked ? _unitOfWork.Repository<HrkPlayer>().Query() : _unitOfWork.ReadOnlyRepository<HrkPlayer>().Query();
        var player = await query.FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, cancellationToken);
        if (player is not null && _httpContext.HttpContext is { } http)
        {
            http.Items["PlayerId"] = player.Id;
            _diagnostic.Set("PlayerId", player.Id);
        }

        return player;
    }

    public async Task<HrkPlayer?> GetPlayerByIdAsync(long playerId, CancellationToken cancellationToken = default, bool tracked = false)
    {
        var query = tracked ? _unitOfWork.Repository<HrkPlayer>().Query() : _unitOfWork.ReadOnlyRepository<HrkPlayer>().Query();
        return await query.SingleOrDefaultAsync(p => p.Id == playerId, cancellationToken);
    }

    public async Task<HrkPlayerWallet?> GetWalletByPlayerIdAsync(long playerId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.Repository<HrkPlayerWallet>().Query()
            .FirstOrDefaultAsync(w => w.PlayerId == playerId, cancellationToken);
    }
}
