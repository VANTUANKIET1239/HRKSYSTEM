using Core.Persistence.Repositories;
using GAME.Application.DTOs;
using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GAME.Application.Features.Shared.Summon;

public abstract class SummonHandlerSupport
{
    protected readonly IUnitOfWork _unitOfWork;
    protected SummonHandlerSupport(IUnitOfWork unitOfWork)
    {
        this._unitOfWork = unitOfWork;
    }

    protected IQueryable<HrkSummonPool> PoolQuery() => _unitOfWork.ReadOnlyRepository<HrkSummonPool>().Query()
        .Include(x => x.Entries).ThenInclude(x => x.HeroTemplate).ThenInclude(x => x.Rarity);
    protected static List<HrkSummonPoolEntry> EnabledEntries(HrkSummonPool pool) => pool.Entries.Where(x => x.IsEnabled)
        .OrderBy(x => x.Id).ToList();
    protected static SummonResultDto ReadResult(HrkSummonBatch batch) => JsonSerializer.Deserialize<SummonResultDto>(batch.ResultJson) ?? throw new InvalidOperationException("Invalid persisted summon result.");
}
