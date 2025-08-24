using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AUTH.Domain.Entities
{
    [Table("HRK_LoginSessions")]
    public class HRK_LoginSession   
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public Guid SessionId { get; set; }

        public string? AccessToken { get; set; }

        public string? RefreshToken { get; set; }

        [MaxLength(45)]
        public string? IPAddress { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

        public DateTime? LogoutTime { get; set; }

        [NotMapped]
        public bool IsActive => LogoutTime == null;
    }
}
