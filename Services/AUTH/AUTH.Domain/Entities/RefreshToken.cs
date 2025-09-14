using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AUTH.Domain.Entities
{
    [Table("HRK_RefreshTokens")]
    public class HRK_RefreshToken
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TokenHash { get; set; } = default!;

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public Guid SessionId { get; set; }

        public DateTime CreatedAt { get; set; }
        public string CreatedByIp { get; set; } = default!;
        public DateTime ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByTokenHash { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => RevokedAt == null && !IsExpired;
    }
}
