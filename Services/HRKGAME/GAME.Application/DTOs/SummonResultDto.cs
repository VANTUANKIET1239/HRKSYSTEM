namespace GAME.Application.DTOs;
public sealed record SummonResultDto(Guid RequestId, string PoolCode, int Count, string Currency,
    long Cost, SummonWalletDto Wallet, IReadOnlyList<SummonRewardDto> Items);
