namespace GAME.Application.DTOs;

public sealed class EquipmentRolledAttributeDto
{
    public int AttributeTypeId { get; set; }
    public string AttributeCode { get; set; } = null!;
    public string AttributeName { get; set; } = null!;
    public bool IsPercentage { get; set; }
    public string ValueType => IsPercentage ? "PERCENT" : "FLAT";
    public decimal BaseRolledValue { get; set; }
    public decimal EnhancementValue { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal RollMinValue { get; set; }
    public decimal RollMaxValue { get; set; }
    public decimal MinValue => RollMinValue;
    public decimal MaxValue => RollMaxValue;
    public decimal RollQualityPercent { get; set; }
    public decimal RollPercent => RollQualityPercent;
    public int DisplayOrder { get; set; }
}
