namespace GAME.Application.DTOs;

public sealed record SummonRequestDto(Guid RequestId, string PoolCode, int Count);
public sealed record SummonWalletDto(long Gold, int Diamonds);
public sealed record SummonHeroDto(int HeroTemplateId, string Name, string Avatar, string RarityCode,
    string RarityName, string? RarityColorHex, decimal ProbabilityPercent);
public sealed record SummonPoolDto(string Code, string Name, string Currency, long SingleCost,
    long TenCost, IReadOnlyList<SummonHeroDto> Heroes);
public sealed record SummonConfigurationDto(long PlayerId, SummonWalletDto Wallet, IReadOnlyList<SummonPoolDto> Pools);
public sealed record SummonRewardDto(int DrawIndex, int HeroTemplateId, string Name, string Avatar,
    string RarityCode, string RarityName, string? RarityColorHex, bool IsNew,
    int? StoneItemTemplateId, string? StoneName, int StoneQuantity);
public sealed record SummonResultDto(Guid RequestId, string PoolCode, int Count, string Currency,
    long Cost, SummonWalletDto Wallet, IReadOnlyList<SummonRewardDto> Items);
