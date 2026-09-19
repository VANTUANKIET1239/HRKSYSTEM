namespace GAME.Domain.Services
{
    /// <summary>
    /// Kết quả thuần túy của một lượt thử cường hóa từ Domain Service.
    /// </summary>
    public sealed record EnhancementExecutionResult(
        bool IsSuccess,
        int OldEnhancement,
        int TargetEnhancement,
        int NewEnhancement,
        decimal BaseSuccessRate,
        decimal StoneBonusRate,
        decimal CharmBonusRate,
        decimal FinalSuccessRate,
        int FailureDropLevels,
        bool WasLevelProtected);
}
