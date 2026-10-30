namespace GAME.Application.DTOs;

public class PlayerPendingRewardDto
{
    public long Id { get; set; }
    public string SourceType { get; set; } = null!;
    public int SourceRefId { get; set; }
    public string Description { get; set; } = null!;
    public List<GenericRewardItemDto> RewardItems { get; set; } = new();
    public string Status { get; set; } = "PENDING";
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? ClaimedOnUtc { get; set; }
}
