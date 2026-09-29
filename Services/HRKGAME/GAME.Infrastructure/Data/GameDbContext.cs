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

        public DbSet<HrkSkillEffectType> SkillEffectTypes { get; set; } = null!;
        public DbSet<HrkItemCategory> ItemCategories { get; set; } = null!;
        public DbSet<HrkItemTemplate> ItemTemplates { get; set; } = null!;
        public DbSet<HrkAttributeType> AttributeTypes { get; set; } = null!;
        public DbSet<HrkItemTemplateAttribute> ItemTemplateAttributes { get; set; } = null!;
        public DbSet<HrkCategoryAllowedAttribute> CategoryAllowedAttributes { get; set; } = null!;
        public DbSet<HrkHeroTemplate> HeroTemplates { get; set; } = null!;
        public DbSet<HrkSkillTemplate> SkillTemplates { get; set; } = null!;
        public DbSet<HrkSkillAnimationConfig> SkillAnimationConfigs { get; set; } = null!;
        public DbSet<HrkSkillTimelinePhase> SkillTimelinePhases { get; set; } = null!;
        public DbSet<HrkSkillTargetType> SkillTargetTypes { get; set; } = null!;
        public DbSet<HrkSkillEffect> SkillEffects { get; set; } = null!;
        public DbSet<HrkSkillEffectScaling> SkillEffectScalings { get; set; } = null!;
        public DbSet<HrkSkillEffectStatModifier> SkillEffectStatModifiers { get; set; } = null!;
        public DbSet<HrkSkillEffectParameter> SkillEffectParameters { get; set; } = null!;
        public DbSet<HrkHeroSkill> HeroSkills { get; set; } = null!;
        public DbSet<HrkPlayer> Players { get; set; } = null!;
        public DbSet<HrkPlayerWallet> PlayerWallets { get; set; } = null!;
        public DbSet<HrkPlayerHero> PlayerHeroes { get; set; } = null!;
        public DbSet<HrkPlayerInventory> PlayerInventories { get; set; } = null!;
        public DbSet<HrkPlayerEquipment> PlayerEquipments { get; set; } = null!;
        public DbSet<HrkFormationTemplate> FormationTemplates { get; set; } = null!;
        public DbSet<HrkFormationSlotTemplate> FormationSlotTemplates { get; set; } = null!;
        public DbSet<HrkFormationLevelConfig> FormationLevelConfigs { get; set; } = null!;
        public DbSet<HrkPlayerFormation> PlayerFormations { get; set; } = null!;
        public DbSet<HrkBattleLog> BattleLogs { get; set; } = null!;
        public DbSet<HrkBattleConfig> BattleConfigs { get; set; } = null!;
        public DbSet<HrkEnhancementLevelConfig> EnhancementLevelConfigs { get; set; } = null!;
        public DbSet<HrkEnhancementMaterial> EnhancementMaterials { get; set; } = null!;
        public DbSet<HrkEquipmentEnhancementHistory> EquipmentEnhancementHistories { get; set; } = null!;
        public DbSet<HrkGameFeatureConfig> GameFeatureConfigs { get; set; } = null!;
        public DbSet<HrkCombatPowerConfig> CombatPowerConfigs { get; set; } = null!;
        public DbSet<HrkHeroRarityUpgradeConfig> HeroRarityUpgradeConfigs { get; set; } = null!;
        public DbSet<HrkHeroStarAuraConfig> HeroStarAuraConfigs { get; set; } = null!;
        public DbSet<HrkEquipmentDowngradeConfig> EquipmentDowngradeConfigs { get; set; } = null!;
        public DbSet<HrkEquipmentDowngradeHistory> EquipmentDowngradeHistories { get; set; } = null!;
        public DbSet<HrkHeroStoneConfig> HeroStoneConfigs { get; set; } = null!;
        public DbSet<HrkHeroStarUpgradeConfig> HeroStarUpgradeConfigs { get; set; } = null!;
        public DbSet<HrkPlayerHeroBonusAttribute> PlayerHeroBonusAttributes { get; set; } = null!;
        public DbSet<HrkHeroStarAttributePool> HeroStarAttributePools { get; set; } = null!;
        public DbSet<HrkHeroStarUpgradeHistory> HeroStarUpgradeHistories { get; set; } = null!;
        public DbSet<HrkHeroAcquisitionHistory> HeroAcquisitionHistories { get; set; } = null!;
        public DbSet<HrkAvatarTemplate> AvatarTemplates { get; set; } = null!;
        public DbSet<HrkPlayerCustomAvatar> PlayerCustomAvatars { get; set; } = null!;
        public DbSet<HrkDungeonMap> DungeonMaps { get; set; } = null!;
        public DbSet<HrkDungeonStage> DungeonStages { get; set; } = null!;
        public DbSet<HrkDungeonStageEnemy> DungeonStageEnemies { get; set; } = null!;
        public DbSet<HrkPlayerDungeonStageProgress> PlayerDungeonStageProgress { get; set; } = null!;
        public DbSet<HrkDungeonRun> DungeonRuns { get; set; } = null!;
        public DbSet<HrkDungeonStageDropPool> DungeonStageDropPools { get; set; } = null!;
        public DbSet<HrkDungeonMapStarChest> DungeonMapStarChests { get; set; } = null!;
        public DbSet<HrkPlayerDungeonStarChestClaim> PlayerDungeonStarChestClaims { get; set; } = null!;
        public DbSet<HrkDungeonStarRatingConfig> DungeonStarRatingConfigs { get; set; } = null!;
        public DbSet<HrkEquipmentRarityRollConfig> EquipmentRarityRollConfigs { get; set; } = null!;
        public DbSet<HrkPlayerInventoryAttribute> PlayerInventoryAttributes { get; set; } = null!;
        public DbSet<HrkHeroLevelConfig> HeroLevelConfigs { get; set; } = null!;
        public DbSet<HrkPlayerLevelConfig> PlayerLevelConfigs { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<HrkHeroLevelConfig>(entity =>
            {
                entity.ToTable("HRK_HeroLevelConfigs");
                entity.HasKey(x => x.Level);
            });

            modelBuilder.Entity<HrkPlayerLevelConfig>(entity =>
            {
                entity.ToTable("HRK_PlayerLevelConfigs");
                entity.HasKey(x => x.Level);
            });

            modelBuilder.Entity<HrkDungeonMap>(entity =>
            {
                entity.ToTable("HRK_DungeonMaps");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.Code).IsUnique();
                entity.HasIndex(x => x.DisplayOrder).IsUnique();
                entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.ImagePath).HasMaxLength(500).IsRequired();
                entity.Property(x => x.BackgroundPath).HasMaxLength(500).IsRequired();
                entity.HasOne(x => x.PreviousMap).WithMany().HasForeignKey(x => x.PreviousMapId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.MaxEquipmentRarity).WithMany().HasForeignKey(x => x.MaxEquipmentRarityId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HrkDungeonStarRatingConfig>(entity =>
            {
                entity.ToTable("HRK_DungeonStarRatingConfigs");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DungeonMapId, x.Stars }).IsUnique();
                entity.Property(x => x.MinRemainingHpRate).HasPrecision(5, 4);
                entity.HasOne(x => x.DungeonMap).WithMany(x => x.StarRatingConfigs)
                    .HasForeignKey(x => x.DungeonMapId).OnDelete(DeleteBehavior.Cascade);
            });
            modelBuilder.Entity<HrkDungeonStage>(entity =>
            {
                entity.ToTable("HRK_DungeonStages");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DungeonMapId, x.StageNumber }).IsUnique();
                entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
                entity.Property(x => x.StageType).HasMaxLength(20).IsRequired();
                entity.Property(x => x.BackgroundPath).HasMaxLength(500);
                entity.HasOne(x => x.DungeonMap).WithMany(x => x.Stages).HasForeignKey(x => x.DungeonMapId);
            });
            modelBuilder.Entity<HrkDungeonStageEnemy>(entity =>
            {
                entity.ToTable("HRK_DungeonStageEnemies");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.StageId, x.Position }).IsUnique();
                entity.Property(x => x.DisplayName).HasMaxLength(150);
                entity.Property(x => x.ImagePath).HasMaxLength(500);
                entity.Property(x => x.StatMultiplier).HasPrecision(8, 4);
                entity.HasOne(x => x.Stage).WithMany(x => x.Enemies).HasForeignKey(x => x.StageId);
                entity.HasOne(x => x.HeroTemplate).WithMany().HasForeignKey(x => x.HeroTemplateId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HrkPlayerDungeonStageProgress>(entity =>
            {
                entity.ToTable("HRK_PlayerDungeonStageProgress");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.PlayerId, x.StageId }).IsUnique();
                entity.Property(x => x.BestRemainingHpRate).HasPrecision(5, 4);
                entity.HasOne(x => x.Player).WithMany(x => x.DungeonProgress).HasForeignKey(x => x.PlayerId);
                entity.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HrkDungeonRun>(entity =>
            {
                entity.ToTable("HRK_DungeonRuns");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.PlayerId, x.ClientRequestId }).IsUnique();
                entity.HasIndex(x => x.BattleId);
                entity.Property(x => x.BattleId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.ClientRequestId).HasMaxLength(64).IsRequired();
                entity.Property(x => x.Result).HasMaxLength(20).IsRequired();
                entity.Property(x => x.FormationCode).HasMaxLength(50);
                entity.HasOne(x => x.Player).WithMany(x => x.DungeonRuns).HasForeignKey(x => x.PlayerId);
                entity.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HrkDungeonStageDropPool>(entity =>
            {
                entity.ToTable("HRK_DungeonStageDropPools");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.StageId, x.IsActive });
                entity.Property(x => x.DropRate).HasPrecision(5, 4);
                entity.HasOne(x => x.Stage).WithMany(x => x.DropPools).HasForeignKey(x => x.StageId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.ItemTemplate).WithMany().HasForeignKey(x => x.ItemTemplateId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HrkDungeonMapStarChest>(entity =>
            {
                entity.ToTable("HRK_DungeonMapStarChests");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.DungeonMapId, x.RequiredStars }).IsUnique();
                entity.Property(x => x.Description).HasMaxLength(500);
                entity.HasOne(x => x.DungeonMap).WithMany(x => x.StarChests).HasForeignKey(x => x.DungeonMapId).OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(x => x.GuaranteedItemTemplate).WithMany().HasForeignKey(x => x.GuaranteedItemTemplateId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<HrkPlayerDungeonStarChestClaim>(entity =>
            {
                entity.ToTable("HRK_PlayerDungeonStarChestClaims");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.PlayerId, x.StarChestId }).IsUnique();
                entity.HasOne(x => x.Player).WithMany(x => x.StarChestClaims).HasForeignKey(x => x.PlayerId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.StarChest).WithMany(x => x.Claims).HasForeignKey(x => x.StarChestId).OnDelete(DeleteBehavior.Cascade);
            });

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



            // 8. HRK_SkillEffectTypes
            modelBuilder.Entity<HrkSkillEffectType>(entity =>
            {
                entity.ToTable("HRK_SkillEffectTypes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.EffectGroup).HasMaxLength(30).IsRequired().HasDefaultValue("SPECIAL");
                entity.Property(e => e.IsBeneficial).HasDefaultValue(false);
                entity.Property(e => e.IsStackable).HasDefaultValue(false);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImagePath).HasMaxLength(255);
                entity.Property(e => e.ColorHex).HasMaxLength(20);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
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
                entity.Property(e => e.MinValue).HasPrecision(18, 4);
                entity.Property(e => e.MaxValue).HasPrecision(18, 4);

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
                entity.Property(e => e.BaseMagicDamage).HasDefaultValue(0);
                entity.Property(e => e.BaseMagicResistance).HasDefaultValue(0);
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
                entity.Property(e => e.ImagePath).HasMaxLength(255);
                entity.Property(e => e.SkillTypeCode).HasMaxLength(30).IsRequired().HasDefaultValue("ENERGY");
                entity.Property(e => e.TriggerCode).HasMaxLength(30).IsRequired().HasDefaultValue("MANUAL_ENERGY_FULL");
                entity.Property(e => e.EnergyCost).HasDefaultValue(100);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
            });

            modelBuilder.Entity<HrkSkillAnimationConfig>(entity =>
            {
                entity.ToTable("HRK_SkillAnimationConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.SkillId).IsUnique();
                entity.Property(e => e.SkillId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.AnimationKey).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DefaultPlaybackSpeed).HasPrecision(5, 2).HasDefaultValue(1m);
                entity.HasOne(e => e.Skill).WithOne(e => e.AnimationConfig)
                    .HasForeignKey<HrkSkillAnimationConfig>(e => e.SkillId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HrkSkillTimelinePhase>(entity =>
            {
                entity.ToTable("HRK_SkillTimelinePhases");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SkillAnimationConfigId, e.PhaseCode }).IsUnique();
                entity.Property(e => e.PhaseCode).HasMaxLength(30).IsRequired();
                entity.Property(e => e.TriggerEventType).HasMaxLength(40);
                entity.HasOne(e => e.SkillAnimationConfig).WithMany(e => e.Phases)
                    .HasForeignKey(e => e.SkillAnimationConfigId).OnDelete(DeleteBehavior.Cascade);
            });

            // 12a. HRK_SkillTargetTypes
            modelBuilder.Entity<HrkSkillTargetType>(entity =>
            {
                entity.ToTable("HRK_SkillTargetTypes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.TargetSide).HasMaxLength(20).IsRequired();
                entity.Property(e => e.SelectionRule).HasMaxLength(50).IsRequired();
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
            });

            // 12b. HRK_SkillEffects
            modelBuilder.Entity<HrkSkillEffect>(entity =>
            {
                entity.ToTable("HRK_SkillEffects");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SkillId, e.DisplayOrder }).IsUnique();
                entity.Property(e => e.SkillId).HasMaxLength(100).IsRequired();
                entity.Property(e => e.DamageSchoolCode).HasMaxLength(20);
                entity.Property(e => e.ExecutionGroup).HasMaxLength(50);
                entity.Property(e => e.ConditionCode).HasMaxLength(50);
                entity.Property(e => e.BaseValue).HasPrecision(18, 4).HasDefaultValue(0);
                entity.Property(e => e.ChancePercent).HasPrecision(5, 2).HasDefaultValue(100.0m);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(d => d.Skill)
                    .WithMany(p => p.Effects)
                    .HasForeignKey(d => d.SkillId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.EffectType)
                    .WithMany(p => p.Effects)
                    .HasForeignKey(d => d.EffectTypeId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.TargetType)
                    .WithMany(p => p.Effects)
                    .HasForeignKey(d => d.TargetTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 12c. HRK_SkillEffectScalings
            modelBuilder.Entity<HrkSkillEffectScaling>(entity =>
            {
                entity.ToTable("HRK_SkillEffectScalings");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SkillEffectId, e.AttributeTypeId }).IsUnique();
                entity.Property(e => e.Coefficient).HasPrecision(18, 6).HasDefaultValue(0);
                entity.Property(e => e.FlatValue).HasPrecision(18, 4).HasDefaultValue(0);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(d => d.SkillEffect)
                    .WithMany(p => p.Scalings)
                    .HasForeignKey(d => d.SkillEffectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.AttributeType)
                    .WithMany()
                    .HasForeignKey(d => d.AttributeTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 12d. HRK_SkillEffectStatModifiers
            modelBuilder.Entity<HrkSkillEffectStatModifier>(entity =>
            {
                entity.ToTable("HRK_SkillEffectStatModifiers");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ValueType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.Value).HasPrecision(18, 4);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(d => d.SkillEffect)
                    .WithMany(p => p.StatModifiers)
                    .HasForeignKey(d => d.SkillEffectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.AttributeType)
                    .WithMany()
                    .HasForeignKey(d => d.AttributeTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 12e. HRK_SkillEffectParameters
            modelBuilder.Entity<HrkSkillEffectParameter>(entity =>
            {
                entity.ToTable("HRK_SkillEffectParameters");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.SkillEffectId, e.ParameterCode }).IsUnique();
                entity.Property(e => e.ParameterCode).HasMaxLength(50).IsRequired();
                entity.Property(e => e.DecimalValue).HasPrecision(18, 4);
                entity.Property(e => e.StringValue).HasMaxLength(255);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(d => d.SkillEffect)
                    .WithMany(p => p.Parameters)
                    .HasForeignKey(d => d.SkillEffectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 13. HRK_HeroSkills
            modelBuilder.Entity<HrkHeroSkill>(entity =>
            {
                entity.ToTable("HRK_HeroSkills");
                entity.HasKey(e => new { e.HeroTemplateId, e.SkillId });
                entity.HasIndex(e => new { e.HeroTemplateId, e.SkillOrder }).IsUnique();
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
                entity.Property(e => e.AvatarType).HasMaxLength(20).HasDefaultValue("TEMPLATE").IsRequired();
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.User)
                    .WithOne(p => p.Player)
                    .HasForeignKey<HrkPlayer>(d => d.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.AvatarTemplate)
                    .WithMany(p => p.Players)
                    .HasForeignKey(d => d.AvatarTemplateId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<HrkAvatarTemplate>(entity =>
            {
                entity.ToTable("HRK_AvatarTemplates");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(80).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
                entity.Property(e => e.ImagePath).HasMaxLength(500).IsRequired();
                entity.Property(e => e.IsEnabled).HasDefaultValue(true);
            });

            modelBuilder.Entity<HrkPlayerCustomAvatar>(entity =>
            {
                entity.ToTable("HRK_PlayerCustomAvatars");
                entity.HasKey(e => e.PlayerId);
                entity.Property(e => e.ImageData).HasColumnType("varbinary(max)").IsRequired();
                entity.Property(e => e.ContentType).HasMaxLength(50).IsRequired();
                entity.Property(e => e.FileName).HasMaxLength(255);
                entity.Property(e => e.ContentHash).HasMaxLength(64).IsRequired();
                entity.HasOne(e => e.Player).WithOne(p => p.CustomAvatar)
                    .HasForeignKey<HrkPlayerCustomAvatar>(e => e.PlayerId)
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
                entity.HasIndex(e => new { e.PlayerId, e.IsActive });
                entity.HasIndex(e => new { e.PlayerId, e.HeroTemplateId });
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
                entity.HasIndex(e => new { e.PlayerId, e.IsActive, e.IsEquipped });
                entity.HasIndex(e => e.EquippedHeroId);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.AcquiredOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.EnhancementGrowthPercent).HasPrecision(8, 4);
                entity.Property(e => e.EnhancementGrowthMinPercent).HasPrecision(8, 4);
                entity.Property(e => e.EnhancementGrowthMaxPercent).HasPrecision(8, 4);

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

                entity.HasMany(d => d.Attributes)
                    .WithOne(p => p.PlayerInventory)
                    .HasForeignKey(p => p.PlayerInventoryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 17.1 HRK_EquipmentRarityRollConfigs
            modelBuilder.Entity<HrkEquipmentRarityRollConfig>(entity =>
            {
                entity.ToTable("HRK_EquipmentRarityRollConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RarityId).IsUnique();
                entity.Property(e => e.EnhancementGrowthMinPercent).HasPrecision(8, 4);
                entity.Property(e => e.EnhancementGrowthMaxPercent).HasPrecision(8, 4);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Rarity)
                    .WithMany()
                    .HasForeignKey(d => d.RarityId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // 17.2 HRK_PlayerInventoryAttributes
            modelBuilder.Entity<HrkPlayerInventoryAttribute>(entity =>
            {
                entity.ToTable("HRK_PlayerInventoryAttributes");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PlayerInventoryId, e.AttributeTypeId }).IsUnique();
                entity.Property(e => e.BaseRolledValue).HasPrecision(18, 4);
                entity.Property(e => e.CurrentValue).HasPrecision(18, 4);
                entity.Property(e => e.RollMinValue).HasPrecision(18, 4);
                entity.Property(e => e.RollMaxValue).HasPrecision(18, 4);
                entity.Property(e => e.RollQualityPercent).HasPrecision(5, 2);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("GETDATE()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.PlayerInventory)
                    .WithMany(p => p.Attributes)
                    .HasForeignKey(d => d.PlayerInventoryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.AttributeType)
                    .WithMany()
                    .HasForeignKey(d => d.AttributeTypeId)
                    .OnDelete(DeleteBehavior.Restrict);
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
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.PlayerId, e.FormationTemplateId }).IsUnique();
                entity.HasIndex(e => new { e.PlayerId, e.IsActive, e.IsSelected });
                entity.Property(e => e.Level).HasDefaultValue(1);
                entity.Property(e => e.IsSelected).HasDefaultValue(false);
                entity.Property(e => e.FormationName).HasMaxLength(50);
                entity.Property(e => e.TotalPower).HasDefaultValue(0);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");

                entity.HasOne(d => d.Player)
                    .WithMany(p => p.Formations)
                    .HasForeignKey(d => d.PlayerId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.FormationTemplate)
                    .WithMany(p => p.PlayerFormations)
                    .HasForeignKey(d => d.FormationTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);

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

            // 19a. HRK_FormationTemplates
            modelBuilder.Entity<HrkFormationTemplate>(entity =>
            {
                entity.ToTable("HRK_FormationTemplates");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(50).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.ImagePath).HasMaxLength(255);
                entity.Property(e => e.MaxLevel).HasDefaultValue(5);
                entity.Property(e => e.DisplayOrder).HasDefaultValue(0);
                entity.Property(e => e.IsEnabled).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
            });

            // 19b. HRK_FormationSlotTemplates
            modelBuilder.Entity<HrkFormationSlotTemplate>(entity =>
            {
                entity.ToTable("HRK_FormationSlotTemplates");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.FormationTemplateId, e.Slot }).IsUnique();
                entity.Property(e => e.RowType).HasMaxLength(20).IsRequired();
                entity.Property(e => e.DisplayX).HasDefaultValue(0);
                entity.Property(e => e.DisplayY).HasDefaultValue(0);
                entity.Property(e => e.IsEnabled).HasDefaultValue(true);

                entity.HasOne(d => d.FormationTemplate)
                    .WithMany(p => p.Slots)
                    .HasForeignKey(d => d.FormationTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // 19c. HRK_FormationLevelConfigs
            modelBuilder.Entity<HrkFormationLevelConfig>(entity =>
            {
                entity.ToTable("HRK_FormationLevelConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.FormationTemplateId, e.Level }).IsUnique();
                entity.Property(e => e.GoldCost).HasDefaultValue(0);
                entity.Property(e => e.StoneCost).HasDefaultValue(0);
                entity.Property(e => e.StatBonusJson).IsRequired();
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(d => d.FormationTemplate)
                    .WithMany(p => p.LevelConfigs)
                    .HasForeignKey(d => d.FormationTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.StoneItemTemplate)
                    .WithMany()
                    .HasForeignKey(d => d.StoneItemTemplateId)
                    .OnDelete(DeleteBehavior.Restrict);
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

            modelBuilder.Entity<HrkBattleConfig>(entity =>
            {
                entity.ToTable("HRK_BattleConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(80).IsRequired();
                entity.Property(e => e.Value).HasPrecision(18, 4);
                entity.Property(e => e.ValueType).HasMaxLength(20).IsRequired().HasDefaultValue("NUMBER");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsEnabled).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
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

            modelBuilder.Entity<HrkGameFeatureConfig>(entity =>
            {
                entity.ToTable("HRK_GameFeatureConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Code).IsUnique();
                entity.Property(e => e.Code).HasMaxLength(80).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(120).IsRequired();
                entity.Property(e => e.Icon).HasMaxLength(100);
                entity.Property(e => e.Placement).HasMaxLength(40).IsRequired();
                entity.Property(e => e.ActionCode).HasMaxLength(80);
                entity.HasOne(e => e.ParentFeature).WithMany(e => e.Children)
                    .HasForeignKey(e => e.ParentFeatureId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<HrkCombatPowerConfig>(entity =>
            {
                entity.ToTable("HRK_CombatPowerConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.StatCode).IsUnique();
                entity.Property(e => e.StatCode).HasMaxLength(40).IsRequired();
                entity.Property(e => e.PowerPerUnit).HasPrecision(18, 4);
            });

            modelBuilder.Entity<HrkHeroRarityUpgradeConfig>(entity =>
            {
                entity.ToTable("HRK_HeroRarityUpgradeConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.RarityId).IsUnique();
                entity.Property(e => e.StatGrowthRate).HasPrecision(10, 6);
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("GETDATE()");
                entity.HasOne(e => e.Rarity).WithMany()
                    .HasForeignKey(e => e.RarityId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<HrkHeroStarAuraConfig>(entity =>
            {
                entity.ToTable("HRK_HeroStarAuraConfigs");
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.HeroTemplateId, e.StarLevel }).IsUnique();
                entity.HasIndex(e => e.AuraCode).IsUnique();
                entity.Property(e => e.AuraCode).HasMaxLength(100).IsRequired();
                entity.Property(e => e.VisualKey).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Name).HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.PrimaryColorHex).HasMaxLength(9);
                entity.Property(e => e.SecondaryColorHex).HasMaxLength(9);
                entity.Property(e => e.Intensity).HasPrecision(5, 2).HasDefaultValue(1m);
                entity.Property(e => e.ParticleLevel).HasDefaultValue((byte)1);
                entity.Property(e => e.IsActive).HasDefaultValue(true);
                entity.Property(e => e.CreatedOn).HasDefaultValueSql("SYSUTCDATETIME()");
                entity.Property(e => e.UpdatedOn).HasDefaultValueSql("SYSUTCDATETIME()");

                entity.HasOne(d => d.HeroTemplate)
                    .WithMany(p => p.StarAuraConfigs)
                    .HasForeignKey(d => d.HeroTemplateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<HrkEquipmentDowngradeConfig>(entity =>
            {
                entity.ToTable("HRK_EquipmentDowngradeConfigs");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new { x.FromLevel, x.ToLevel }).IsUnique();
                entity.Property(x => x.GoldRefundPercent).HasPrecision(8, 4);
                entity.Property(x => x.StoneRefundPercent).HasPrecision(8, 4);
            });

            modelBuilder.Entity<HrkEquipmentDowngradeHistory>(entity =>
            {
                entity.ToTable("HRK_EquipmentDowngradeHistories");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.RequestId).IsUnique();
                entity.HasIndex(x => new { x.PlayerId, x.PlayerInventoryId });
                entity.Property(x => x.RefundedMaterialsJson).IsRequired();
            });

            modelBuilder.Entity<HrkHeroStoneConfig>(entity =>
            {
                entity.ToTable("HRK_HeroStoneConfigs");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.HeroTemplateId).IsUnique();
                entity.HasIndex(x => x.ItemTemplateId).IsUnique();
            });

            modelBuilder.Entity<HrkHeroStarUpgradeConfig>(entity =>
            {
                entity.ToTable("HRK_HeroStarUpgradeConfigs");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new
                {
                    x.RarityId,
                    x.CurrentStar,
                    x.NextStar
                }).IsUnique();
                entity.Property(x => x.GrowthBonusPercent).HasPrecision(10, 6);
            });

            modelBuilder.Entity<HrkPlayerHeroBonusAttribute>(entity =>
            {
                entity.ToTable("HRK_PlayerHeroBonusAttributes");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new
                {
                    x.PlayerHeroId,
                    x.UnlockedAtStar
                }).IsUnique();
                entity.Property(x => x.Value).HasPrecision(18, 4);
            });

            modelBuilder.Entity<HrkHeroStarAttributePool>(entity =>
            {
                entity.ToTable("HRK_HeroStarAttributePools");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => new
                {
                    x.RarityId,
                    x.AttributeTypeId
                }).IsUnique();
                entity.Property(x => x.MinValue).HasPrecision(18, 4);
                entity.Property(x => x.MaxValue).HasPrecision(18, 4);
            });

            modelBuilder.Entity<HrkHeroStarUpgradeHistory>(entity =>
            {
                entity.ToTable("HRK_HeroStarUpgradeHistories");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.RequestId).IsUnique();
                entity.HasIndex(x => new { x.PlayerId, x.PlayerHeroId });
            });

            modelBuilder.Entity<HrkHeroAcquisitionHistory>(entity =>
            {
                entity.ToTable("HRK_HeroAcquisitionHistories");
                entity.HasKey(x => x.Id);
                entity.HasIndex(x => x.RequestId).IsUnique();
                entity.HasIndex(x => new { x.PlayerId, x.HeroTemplateId });
                entity.Property(x => x.SourceType).HasMaxLength(50);
                entity.Property(x => x.SourceReferenceId).HasMaxLength(150);
            });
        }
    }
}
