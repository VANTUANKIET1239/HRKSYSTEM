using GAME.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GAME.Infrastructure.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options) : base(options)
        {
        }

        public DbSet<HrkUser> Users { get; set; } = null!;
        public DbSet<HrkRarity> Rarities { get; set; } = null!;
        public DbSet<HrkHeroFaction> HeroFactions { get; set; } = null!;
        public DbSet<HrkHeroClass> HeroClasses { get; set; } = null!;
        public DbSet<HrkSkillCostType> SkillCostTypes { get; set; } = null!;
        public DbSet<HrkSkillCategory> SkillCategories { get; set; } = null!;
        public DbSet<HrkSkillDamageType> SkillDamageTypes { get; set; } = null!;
        public DbSet<HrkSkillEffectType> SkillEffectTypes { get; set; } = null!;
        public DbSet<HrkItemCategory> ItemCategories { get; set; } = null!;
        public DbSet<HrkItemTemplate> ItemTemplates { get; set; } = null!;
        public DbSet<HrkAttributeType> AttributeTypes { get; set; } = null!;
        public DbSet<HrkItemTemplateAttribute> ItemTemplateAttributes { get; set; } = null!;
        public DbSet<HrkCategoryAllowedAttribute> CategoryAllowedAttributes { get; set; } = null!;
        public DbSet<HrkHeroTemplate> HeroTemplates { get; set; } = null!;
        public DbSet<HrkSkillTemplate> SkillTemplates { get; set; } = null!;
        public DbSet<HrkHeroSkill> HeroSkills { get; set; } = null!;
        public DbSet<HrkPlayer> Players { get; set; } = null!;
        public DbSet<HrkPlayerWallet> PlayerWallets { get; set; } = null!;
        public DbSet<HrkPlayerHero> PlayerHeroes { get; set; } = null!;
        public DbSet<HrkPlayerInventory> PlayerInventories { get; set; } = null!;
        public DbSet<HrkPlayerEquipment> PlayerEquipments { get; set; } = null!;
        public DbSet<HrkPlayerFormation> PlayerFormations { get; set; } = null!;
        public DbSet<HrkBattleLog> BattleLogs { get; set; } = null!;
        public DbSet<HrkEnhancementLevelConfig> EnhancementLevelConfigs { get; set; } = null!;
        public DbSet<HrkEnhancementMaterial> EnhancementMaterials { get; set; } = null!;
        public DbSet<HrkEquipmentEnhancementHistory> EquipmentEnhancementHistories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. HRK_Users
            modelBuilder.Entity<HrkUser>(entity =>
            {
                entity.ToTable("HRK_Users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(450);
                entity.Property(e => e.UserName).HasMaxLength(256);
                entity.Property(e => e.NormalizedUserName).HasMaxLength(256);
                entity.Property(e => e.Email).HasMaxLength(256);
                entity.Property(e => e.NormalizedEmail).HasMaxLength(256);
                entity.Property(e => e.FullName).HasMaxLength(256);
            });

            // 2. HRK_Rarities
            modelBuilder.Entity<HrkRarity>(entity =>
            {
                entity.ToTable("HRK_Rarities");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.ColorHex).HasMaxLength(20);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 3. HRK_HeroFactions
            modelBuilder.Entity<HrkHeroFaction>(entity =>
            {
                entity.ToTable("HRK_HeroFactions");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 4. HRK_HeroClasses
            modelBuilder.Entity<HrkHeroClass>(entity =>
            {
                entity.ToTable("HRK_HeroClasses");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 5. HRK_SkillCostTypes
            modelBuilder.Entity<HrkSkillCostType>(entity =>
            {
                entity.ToTable("HRK_SkillCostTypes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 6. HRK_SkillCategories
            modelBuilder.Entity<HrkSkillCategory>(entity =>
            {
                entity.ToTable("HRK_SkillCategories");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 7. HRK_SkillDamageTypes
            modelBuilder.Entity<HrkSkillDamageType>(entity =>
            {
                entity.ToTable("HRK_SkillDamageTypes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 8. HRK_SkillEffectTypes
            modelBuilder.Entity<HrkSkillEffectType>(entity =>
            {
                entity.ToTable("HRK_SkillEffectTypes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 9. HRK_ItemCategories
            modelBuilder.Entity<HrkItemCategory>(entity =>
            {
                entity.ToTable("HRK_ItemCategories");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 10. HRK_ItemTemplates
            modelBuilder.Entity<HrkItemTemplate>(entity =>
            {
                entity.ToTable("HRK_ItemTemplates");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
                entity.Property(e => e.ImagePath).HasMaxLength(255);
                entity.Ignore(e => e.Icon);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.ItemTemplates)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Rarity)
                    .WithMany(p => p.ItemTemplates)
                    .HasForeignKey(d => d.RarityId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(d => d.Attributes)
                    .WithOne(a => a.ItemTemplate)
                    .HasForeignKey(a => a.ItemTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 10.1 HRK_AttributeTypes
            modelBuilder.Entity<HrkAttributeType>(entity =>
            {
                entity.ToTable("HRK_AttributeTypes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(255);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 10.2 HRK_ItemTemplateAttributes
            modelBuilder.Entity<HrkItemTemplateAttribute>(entity =>
            {
                entity.ToTable("HRK_ItemTemplateAttributes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.ItemTemplateId, e.AttributeTypeId }).IsUnique();
                entity.Property(e => e.Value).HasPrecision(18, 4);

                entity.HasOne(d => d.ItemTemplate)
                    .WithMany(p => p.Attributes)
                    .HasForeignKey(d => d.ItemTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.AttributeType)
                    .WithMany(p => p.TemplateAttributes)
                    .HasForeignKey(d => d.AttributeTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 10.3 HRK_CategoryAllowedAttributes
            modelBuilder.Entity<HrkCategoryAllowedAttribute>(entity =>
            {
                entity.ToTable("HRK_CategoryAllowedAttributes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.CategoryId, e.AttributeTypeId }).IsUnique();
                entity.Property(e => e.MinValue).HasPrecision(18, 4);
                entity.Property(e => e.MaxValue).HasPrecision(18, 4);

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.AllowedAttributes)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.AttributeType)
                    .WithMany(p => p.CategoryAllowedAttributes)
                    .HasForeignKey(d => d.AttributeTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 11. HRK_HeroTemplates
            modelBuilder.Entity<HrkHeroTemplate>(entity =>
            {
                entity.ToTable("HRK_HeroTemplates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Avatar).HasMaxLength(255).IsRequired();
                entity.Property(e => e.BaseCrit).HasPrecision(5, 2);
                entity.Property(e => e.BaseCritDmg).HasPrecision(5, 2);
                entity.Property(e => e.BaseLifesteal).HasPrecision(5, 2);
                entity.Property(e => e.BaseAccuracy).HasPrecision(5, 2);
                entity.Property(e => e.BaseResistance).HasPrecision(5, 2);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Faction)
                    .WithMany(p => p.HeroTemplates)
                    .HasForeignKey(d => d.FactionId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Class)
                    .WithMany(p => p.HeroTemplates)
                    .HasForeignKey(d => d.ClassId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Rarity)
                    .WithMany(p => p.HeroTemplates)
                    .HasForeignKey(d => d.RarityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 12. HRK_SkillTemplates
            modelBuilder.Entity<HrkSkillTemplate>(entity =>
            {
                entity.ToTable("HRK_SkillTemplates");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasMaxLength(100);
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Icon).HasMaxLength(100).IsRequired();
                entity.Property(e => e.TargetType).HasMaxLength(50).IsRequired().HasDefaultValue("single");
                entity.Property(e => e.Cooldown).HasMaxLength(20).IsRequired().HasDefaultValue("0s");
                entity.Property(e => e.DamageMultiplier).HasPrecision(5, 2).HasDefaultValue(1.0m);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.CostType)
                    .WithMany(p => p.SkillTemplates)
                    .HasForeignKey(d => d.CostTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Category)
                    .WithMany(p => p.SkillTemplates)
                    .HasForeignKey(d => d.CategoryId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.DamageType)
                    .WithMany(p => p.SkillTemplates)
                    .HasForeignKey(d => d.DamageTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.EffectType)
                    .WithMany(p => p.SkillTemplates)
                    .HasForeignKey(d => d.EffectTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 13. HRK_HeroSkills
            modelBuilder.Entity<HrkHeroSkill>(entity =>
            {
                entity.ToTable("HRK_HeroSkills");
                entity.HasKey(e => new { e.HeroTemplateId, e.SkillId });
                entity.Property(e => e.SkillId).HasMaxLength(100);

                entity.HasOne(d => d.HeroTemplate)
                    .WithMany(p => p.HeroSkills)
                    .HasForeignKey(d => d.HeroTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Skill)
                    .WithMany(p => p.HeroSkills)
                    .HasForeignKey(d => d.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 14. HRK_Players
            modelBuilder.Entity<HrkPlayer>(entity =>
            {
                entity.ToTable("HRK_Players");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.PlayerName).IsUnique();
                entity.Property(e => e.UserId).HasMaxLength(450).IsRequired();
                entity.Property(e => e.PlayerName).HasMaxLength(100).IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.User)
                    .WithOne(p => p.Player)
                    .HasForeignKey<HrkPlayer>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 15. HRK_PlayerWallets
            modelBuilder.Entity<HrkPlayerWallet>(entity =>
            {
                entity.ToTable("HRK_PlayerWallets");
                entity.HasKey(e => e.PlayerId);
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Player)
                    .WithOne(p => p.Wallet)
                    .HasForeignKey<HrkPlayerWallet>(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 16. HRK_PlayerHeroes
            modelBuilder.Entity<HrkPlayerHero>(entity =>
            {
                entity.ToTable("HRK_PlayerHeroes");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Heroes)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.HeroTemplate)
                    .WithMany(p => p.PlayerHeroes)
                    .HasForeignKey(d => d.HeroTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 17. HRK_PlayerInventory
            modelBuilder.Entity<HrkPlayerInventory>(entity =>
            {
                entity.ToTable("HRK_PlayerInventory");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AcquiredOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Inventories)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.ItemTemplate)
                    .WithMany(p => p.Inventories)
                    .HasForeignKey(d => d.ItemTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.EquippedHero)
                    .WithMany(p => p.EquippedItems)
                    .HasForeignKey(d => d.EquippedHeroId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 18. HRK_PlayerEquipment
            modelBuilder.Entity<HrkPlayerEquipment>(entity =>
            {
                entity.ToTable("HRK_PlayerEquipment");
                entity.HasKey(e => new { e.PlayerId, e.HeroId });

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Equipments)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Hero)
                    .WithMany(p => p.EquippedOnHeroes)
                    .HasForeignKey(d => d.HeroId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Weapon)
                    .WithMany()
                    .HasForeignKey(d => d.WeaponId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Armor)
                    .WithMany()
                    .HasForeignKey(d => d.ArmorId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Helmet)
                    .WithMany()
                    .HasForeignKey(d => d.HelmetId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Boots)
                    .WithMany()
                    .HasForeignKey(d => d.BootsId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Ring)
                    .WithMany()
                    .HasForeignKey(d => d.RingId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Artifact)
                    .WithMany()
                    .HasForeignKey(d => d.ArtifactId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // 19. HRK_PlayerFormations
            modelBuilder.Entity<HrkPlayerFormation>(entity =>
            {
                entity.ToTable("HRK_PlayerFormations");
                entity.HasKey(e => new { e.PlayerId, e.FormationName });
                entity.Property(e => e.FormationName).HasMaxLength(50).HasDefaultValue("Main Team");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Formations)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.Hero1)
                    .WithMany()
                    .HasForeignKey(d => d.Position1)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Hero2)
                    .WithMany()
                    .HasForeignKey(d => d.Position2)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Hero3)
                    .WithMany()
                    .HasForeignKey(d => d.Position3)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Hero4)
                    .WithMany()
                    .HasForeignKey(d => d.Position4)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.Hero5)
                    .WithMany()
                    .HasForeignKey(d => d.Position5)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            // 20. HRK_BattleLogs
            modelBuilder.Entity<HrkBattleLog>(entity =>
            {
                entity.ToTable("HRK_BattleLogs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.BattleId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.SkillId).HasMaxLength(100);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Skill)
                    .WithMany()
                    .HasForeignKey(d => d.SkillId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // 21. HRK_EnhancementLevelConfigs
            modelBuilder.Entity<HrkEnhancementLevelConfig>(entity =>
            {
                entity.ToTable("HRK_EnhancementLevelConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.CurrentLevel).IsUnique();
                entity.Property(e => e.BaseSuccessRate).HasPrecision(8, 4);
                entity.Property(e => e.FailureDropLevels).HasDefaultValue(0);
                entity.Property(e => e.MaxStoneSlots).HasDefaultValue(3);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
            });

            // 22. HRK_EnhancementMaterials
            modelBuilder.Entity<HrkEnhancementMaterial>(entity =>
            {
                entity.ToTable("HRK_EnhancementMaterials");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.ItemTemplateId).IsUnique();
                entity.Property(e => e.MaterialType).HasMaxLength(30).IsRequired();
                entity.Property(e => e.SuccessRateBonus).HasPrecision(8, 4).HasDefaultValue(0);
                entity.Property(e => e.PreventLevelDrop).HasDefaultValue(false);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.ItemTemplate)
                    .WithMany()
                    .HasForeignKey(d => d.ItemTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 23. HRK_EquipmentEnhancementHistory
            modelBuilder.Entity<HrkEquipmentEnhancementHistory>(entity =>
            {
                entity.ToTable("HRK_EquipmentEnhancementHistory");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RequestId).IsUnique();
                entity.HasIndex(e => e.PlayerId);
                entity.HasIndex(e => e.PlayerInventoryId);
                entity.Property(e => e.BaseSuccessRate).HasPrecision(8, 4);
                entity.Property(e => e.StoneBonusRate).HasPrecision(8, 4);
                entity.Property(e => e.CharmBonusRate).HasPrecision(8, 4);
                entity.Property(e => e.FinalSuccessRate).HasPrecision(8, 4);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Player)
                    .WithMany()
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.PlayerInventory)
                    .WithMany()
                    .HasForeignKey(d => d.PlayerInventoryId)
                    .OnDelete(DeleteBehavior.NoAction);

                entity.HasOne(d => d.ItemTemplate)
                    .WithMany()
                    .HasForeignKey(d => d.ItemTemplateId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
