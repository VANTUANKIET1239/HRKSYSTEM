using GAME.Application.DTOs;

namespace GAME.Application.Common;

public static class HeroStarMaterialPolicy
{
    public static bool IsEnough(StarMaterialRequirementDto material) =>
        material.Required > 0 && material.Owned >= material.Required;

    public static bool CanUpgrade(
        long goldOwned,
        long goldRequired,
        StarMaterialRequirementDto universalStone,
        StarMaterialRequirementDto heroStone) =>
        goldOwned >= goldRequired && (IsEnough(universalStone) || IsEnough(heroStone));

    public static bool UseHeroStone(
        string? materialType,
        StarMaterialRequirementDto universalStone,
        StarMaterialRequirementDto heroStone)
    {
        // Older clients omit the choice: prefer hero-specific stones to save universal stones.
        var useHeroStone = materialType?.ToUpperInvariant() switch
        {
            null => IsEnough(heroStone),
            "HERO" => true,
            "UNIVERSAL" => false,
            _ => throw new InvalidOperationException("Loại đá tăng sao không hợp lệ.")
        };

        if (!IsEnough(useHeroStone ? heroStone : universalStone))
        {
            throw new InvalidOperationException("Không đủ loại đá đã chọn để tăng sao.");
        }

        return useHeroStone;
    }
}
