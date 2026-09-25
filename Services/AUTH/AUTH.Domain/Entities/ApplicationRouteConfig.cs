using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AUTH.Domain.Entities;

[Table("HRK_ApplicationRouteConfigs")]
public class HRK_ApplicationRouteConfig
{
    [Key]
    public int Id { get; set; }

    [Required, MaxLength(50)]
    public string AppCode { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string RoutePrefix { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string ApiPrefix { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Audience { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string DefaultRoute { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string LoginTitle { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ThemeClass { get; set; }

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
