namespace GAME.Application.DTOs;
public sealed record SummonRewardDto(int DrawIndex, int HeroTemplateId, string Name, string Avatar,
    string RarityCode, string RarityName, string? RarityColorHex, bool IsNew,
    int? StoneItemTemplateId, string? StoneName, int StoneQuantity);
