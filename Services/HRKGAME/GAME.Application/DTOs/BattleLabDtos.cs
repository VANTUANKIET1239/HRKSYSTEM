namespace GAME.Application.DTOs;

public sealed class BattleLabSlotDto
{
    public int HeroTemplateId { get; set; }
    public int Position { get; set; }
    public string Mode { get; set; } = "CUSTOM";
    public CalculatedStatsDto? Stats { get; set; }
    public BattleLabBuildDto? Build { get; set; }
}

public sealed class BattleLabBuildDto
{
    public int Level { get; set; } = 1;
    public int Stars { get; set; } = 1;
    public byte AuraTier { get; set; } = 1;
    public int RollSeed { get; set; } = 1;
    public List<BattleLabEquipmentDto> Equipment { get; set; } = [];
}

public sealed class BattleLabEquipmentDto
{
    public int ItemTemplateId { get; set; }
    public int Enhancement { get; set; }
    public int Stars { get; set; }
}

public sealed class BattleLabItemOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public int LevelReq { get; set; }
}

public sealed class BattleLabResolvedBuildDto
{
    public int Team { get; set; }
    public int Position { get; set; }
    public List<HeroStatBreakdownDto> Breakdown { get; set; } = [];
    public List<HeroBonusAttributeDto> StarBonuses { get; set; } = [];
    public List<DungeonDroppedEquipmentDto> Equipment { get; set; } = [];
}

public sealed class BattleLabHeroRunDto
{
    public BattleHeroStatisticsDto Statistics { get; set; } = new();
    public int RemainingHp { get; set; }
    public int SkillCasts { get; set; }
    public int EnergyCasts { get; set; }
}

public sealed class BattleLabSkillMetricDto
{
    public long ActorId { get; set; }
    public string SkillId { get; set; } = "";
    public string School { get; set; } = "";
    public int Hits { get; set; }
    public long HpDamage { get; set; }
    public int CritHits { get; set; }
}

public sealed class BattleLabRequestDto
{
    public List<BattleLabSlotDto> Left { get; set; } = [];
    public List<BattleLabSlotDto> Right { get; set; } = [];
    public int Seed { get; set; } = 1;
    public int Count { get; set; } = 1;
    public int MaxRounds { get; set; } = 100;
    public decimal? DefenseConstant { get; set; }
}

public sealed class BattleLabCatalogDto
{
    public List<BattleLabItemOptionDto> Equipment { get; set; } = [];
    public List<PlayerHeroDto> Heroes { get; set; } = [];
    public decimal DefenseConstant { get; set; }
}

public sealed class BattleLabRunDto
{
    public List<BattleLabHeroRunDto> Heroes { get; set; } = [];
    public int Seed { get; set; }
    public string Winner { get; set; } = "";
    public int Rounds { get; set; }
}

public sealed class BattleLabReportDto
{
    public int ReportVersion { get; set; } = 2;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string EngineVersion { get; set; } = "";
    public string SnapshotHash { get; set; } = "";
    public BattleInitialStateDto ResolvedSnapshot { get; set; } = new();
    public List<BattleLabResolvedBuildDto> ResolvedBuilds { get; set; } = [];
    public List<BattleLabSkillMetricDto> Skills { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public Dictionary<string, decimal> BattleConfigs { get; set; } = [];
    public BattleLabRequestDto Settings { get; set; } = new();
    public List<BattleLabRunDto> Runs { get; set; } = [];
    public List<BattleHeroStatisticsDto> Totals { get; set; } = [];
    public List<StartBattleResultDto> Replays { get; set; } = [];
}
