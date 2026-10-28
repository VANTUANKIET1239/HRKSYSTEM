using Core.TransactionalMessaging.Persistence;
using Language.Domain;
using Microsoft.EntityFrameworkCore;

namespace Language.Infrastructure;

public sealed class LanguageDbContext(DbContextOptions<LanguageDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.ConfigureTransactionalMessaging("Language_OutboxMessages", "Language_ProcessedMessages");
        model.Entity<LanguageCulture>(entity =>
        {
            entity.ToTable("HRK_LanguageCultures");
            entity.HasKey(x => x.Code);
            entity.Property(x => x.Code).HasMaxLength(20);
            entity.Property(x => x.Name).HasMaxLength(100);
        });
        model.Entity<LanguageRevision>(entity =>
        {
            entity.ToTable("HRK_LanguageRevisions", table => table.HasTrigger("TR_LanguageRevision_Immutable"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Group).HasMaxLength(40);
            entity.Property(x => x.Culture).HasMaxLength(20);
            entity.Property(x => x.PublishedBy).HasMaxLength(128);
            entity.HasAlternateKey(x => new { x.Id, x.Group, x.Culture });
            entity.HasOne<LanguageCulture>().WithMany().HasForeignKey(x => x.Culture).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<LanguageKey>(entity =>
        {
            entity.ToTable("HRK_LanguageKeys", table => table.HasTrigger("TR_LanguageKey_Immutable"));
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Group).HasMaxLength(40);
            entity.Property(x => x.Key).HasMaxLength(256).UseCollation("Latin1_General_100_BIN2");
            entity.HasIndex(x => new { x.Group, x.Key }).IsUnique();
        });
        model.Entity<LanguageRevisionText>(entity =>
        {
            entity.ToTable("HRK_LanguageRevisionTexts", table => table.HasTrigger("TR_LanguageRevisionText_Immutable"));
            entity.HasKey(x => new { x.RevisionId, x.LanguageKeyId });
            entity.Property(x => x.Value).HasColumnType("nvarchar(max)");
            entity.HasOne<LanguageRevision>().WithMany().HasForeignKey(x => x.RevisionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<LanguageKey>().WithMany().HasForeignKey(x => x.LanguageKeyId).OnDelete(DeleteBehavior.Restrict);
        });
        model.Entity<LanguagePublication>(entity =>
        {
            entity.ToTable("HRK_LanguagePublications", table => table.HasTrigger("TR_LanguagePublication_Sealed"));
            entity.HasKey(x => new { x.Group, x.Culture });
            entity.Property(x => x.Group).HasMaxLength(40);
            entity.Property(x => x.Culture).HasMaxLength(20);
            entity.HasOne<LanguageRevision>().WithMany().HasForeignKey(x => new { x.RevisionId, x.Group, x.Culture })
                .HasPrincipalKey(x => new { x.Id, x.Group, x.Culture }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
