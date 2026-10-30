using Core.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GAME.Application.Templates;

public interface IGameTemplateRefresh
{
    /// <summary>Reloads every configured table and publishes them together.</summary>
    Task<TemplateRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);
    /// <summary>Reloads T and its static relation group, retaining other tables from the previous revision.</summary>
    Task<TemplateRefreshResult> RefreshAsync<T>(CancellationToken cancellationToken = default) where T : class;
}
