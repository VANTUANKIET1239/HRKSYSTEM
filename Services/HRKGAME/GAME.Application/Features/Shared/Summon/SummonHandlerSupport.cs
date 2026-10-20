using Core.Persistence.Repositories;
using GAME.Application.Templates;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using System.Text.Json;

namespace GAME.Application.Features.Shared.Summon;

public abstract class SummonHandlerSupport
{
    protected readonly IUnitOfWork _unitOfWork;
    protected readonly IGameTemplateCatalog _templates;
    protected SummonHandlerSupport(IUnitOfWork unitOfWork, IGameTemplateCatalog? templates = null)
    {
        this._unitOfWork = unitOfWork;
        _templates = templates ?? new DatabaseTemplateCatalog(unitOfWork);
    }

    protected async Task<IEnumerable<HrkSummonPool>> GetPoolsAsync(CancellationToken ct) =>
        await _templates.GetAllAsync<HrkSummonPool>(ct);
    protected static List<HrkSummonPoolEntry> EnabledEntries(HrkSummonPool pool) => pool.Entries.Where(x => x.IsEnabled)
        .OrderBy(x => x.Id).ToList();
    protected static SummonResultDto ReadResult(HrkSummonBatch batch) => JsonSerializer.Deserialize<SummonResultDto>(batch.ResultJson) ?? throw new InvalidOperationException("Invalid persisted summon result.");
}
