namespace GAME.Domain.Entities;

public class HrkEquipmentDowngradeConfig
{
    public int Id { get; set; }
    public int FromLevel { get; set; }
    public int ToLevel { get; set; }
    public decimal GoldRefundPercent { get; set; }
    public decimal StoneRefundPercent { get; set; }
    public bool RefundCharm { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

public class HrkEquipmentDowngradeHistory
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public long PlayerId { get; set; }
    public long PlayerInventoryId { get; set; }
    public int OldEnhancement { get; set; }
    public int NewEnhancement { get; set; }
    public long RefundedGold { get; set; }
    public string RefundedMaterialsJson { get; set; } = "[]";
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

public class HrkHeroStoneConfig
{
    public int Id { get; set; }
    public int HeroTemplateId { get; set; }
    public int ItemTemplateId { get; set; }
    public int DuplicateConversionQuantity { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

public class HrkHeroStarUpgradeConfig
{
    public int Id { get; set; }
    public int RarityId { get; set; }
    public int CurrentStar { get; set; }
    public int NextStar { get; set; }
    public long GoldCost { get; set; }
    public int UniversalStarStoneItemTemplateId { get; set; }
    public int UniversalStarStoneQuantity { get; set; }
    public int HeroStoneQuantity { get; set; }
    public decimal GrowthBonusPercent { get; set; }
    public int ExtraAttributeRollCount { get; set; } = 1;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

public class HrkPlayerHeroBonusAttribute
{
    public long Id { get; set; }
    public long PlayerHeroId { get; set; }
    public int UnlockedAtStar { get; set; }
    public int AttributeTypeId { get; set; }
    public decimal Value { get; set; }
    public bool IsPercentage { get; set; }
    public Guid RollSeed { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

public class HrkHeroStarAttributePool
{
    public int Id { get; set; }
    public int RarityId { get; set; }
    public int AttributeTypeId { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int Weight { get; set; }
    public bool IsPercentage { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
}

public class HrkHeroStarUpgradeHistory
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public long PlayerId { get; set; }
    public long PlayerHeroId { get; set; }
    public int OldStar { get; set; }
    public int NewStar { get; set; }
    public long GoldCost { get; set; }
    public int UniversalStoneQuantity { get; set; }
    public int HeroStoneItemTemplateId { get; set; }
    public int HeroStoneQuantity { get; set; }
    public string RolledAttributesJson { get; set; } = "[]";
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}

public class HrkHeroAcquisitionHistory
{
    public long Id { get; set; }
    public Guid RequestId { get; set; }
    public long PlayerId { get; set; }
    public int HeroTemplateId { get; set; }
    public string SourceType { get; set; } = null!;
    public string? SourceReferenceId { get; set; }
    public bool WasDuplicate { get; set; }
    public int ConvertedHeroStoneQuantity { get; set; }
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
}
