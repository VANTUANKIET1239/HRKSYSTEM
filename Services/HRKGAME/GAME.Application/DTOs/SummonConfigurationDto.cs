namespace GAME.Application.DTOs;
public sealed record SummonConfigurationDto(long PlayerId, SummonWalletDto Wallet, IReadOnlyList<SummonPoolDto> Pools);
