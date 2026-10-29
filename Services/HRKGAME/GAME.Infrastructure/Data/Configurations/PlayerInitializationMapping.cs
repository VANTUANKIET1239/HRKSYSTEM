using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Data.Configurations;

public static class PlayerInitializationMapping
{
    public static void ConfigurePlayerInitialization(this ModelBuilder builder)
    {
        builder.Entity<HrkPlayerInitializationConfig>(e =>
        {
            e.ToTable("HRK_PlayerInitializationConfigs", t => t.UseSqlOutputClause(false));
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(50);
            e.HasIndex(x => new { x.Code, x.Version }).IsUnique();
            e.HasIndex(x => x.Code).IsUnique().HasFilter("[IsActive] = 1");
            e.HasOne<HrkFormationTemplate>().WithMany().HasForeignKey(x => x.DefaultFormationTemplateId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<HrkAvatarTemplate>().WithMany().HasForeignKey(x => x.DefaultAvatarTemplateId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<HrkPlayerInitializationHero>(e =>
        {
            e.ToTable("HRK_PlayerInitializationHeroes", t => t.UseSqlOutputClause(false));
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ConfigId, x.HeroTemplateId }).IsUnique();
            e.HasIndex(x => new { x.ConfigId, x.FormationPosition }).IsUnique().HasFilter("[FormationPosition] IS NOT NULL");
            e.HasOne<HrkPlayerInitializationConfig>().WithMany().HasForeignKey(x => x.ConfigId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<HrkHeroTemplate>().WithMany().HasForeignKey(x => x.HeroTemplateId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<HrkPlayerInitializationItem>(e =>
        {
            e.ToTable("HRK_PlayerInitializationItems", t => t.UseSqlOutputClause(false));
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.ConfigId, x.ItemTemplateId }).IsUnique();
            e.HasOne<HrkPlayerInitializationConfig>().WithMany().HasForeignKey(x => x.ConfigId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<HrkItemTemplate>().WithMany().HasForeignKey(x => x.ItemTemplateId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Entity<HrkPlayerInitialization>(e =>
        {
            e.ToTable("HRK_PlayerInitializations");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(450);
            e.Property(x => x.Status).HasMaxLength(20);
            e.HasIndex(x => x.UserId).IsUnique();
            e.HasIndex(x => x.RegistrationEventId).IsUnique();
            e.HasOne<HrkPlayerInitializationConfig>().WithMany().HasForeignKey(x => x.ConfigId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<HrkPlayer>().WithMany().HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
