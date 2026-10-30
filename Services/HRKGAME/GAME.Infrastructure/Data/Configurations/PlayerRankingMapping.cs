using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Data.Configurations;

public static class PlayerRankingMapping
{
    public static void ConfigurePlayerRanking(this ModelBuilder model)
    {
        model.Entity<HrkPlayerRankingStats>(e =>
        {
            e.ToTable("HRK_PlayerRankingStats", table => table.HasCheckConstraint("CK_Ranking_Scores",
                "[CurrentFormationPower] >= 0 AND [PeakFormationPower] >= [CurrentFormationPower] AND [TotalDiamondsSpent] >= 0"));
            e.HasKey(x => x.PlayerId);
            e.Property(x => x.RowVersion).IsRowVersion();
            e.HasOne(x => x.Player).WithOne().HasForeignKey<HrkPlayerRankingStats>(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.PeakFormationPower, x.PeakPowerAchievedAtUtc, x.PlayerId })
                .IsDescending(true, false, false).HasDatabaseName("IX_PlayerRanking_Power").HasFilter("[PowerInitialized] = 1");
            e.HasIndex(x => new { x.TotalDiamondsSpent, x.DiamondsSpentAchievedAtUtc, x.PlayerId })
                .IsDescending(true, false, false).HasDatabaseName("IX_PlayerRanking_Diamonds");
        });
        model.Entity<HrkPlayerDiamondSpendTransaction>(e =>
        {
            e.ToTable("HRK_PlayerDiamondSpendTransactions", table => table.HasCheckConstraint("CK_DiamondSpend_Amount", "[Amount] >= 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(40);
            e.Property(x => x.Reference).HasMaxLength(120);
            e.HasIndex(x => new { x.PlayerId, x.RequestId }).IsUnique();
            e.HasOne<HrkPlayer>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
