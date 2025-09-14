using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AUTH.Domain.Entities
{
    [Table("HRK_AccessTokenRecords")]
    public class HRK_AccessTokenRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        [MaxLength(450)]
        public string UserId { get; set; }

        [Required]
        public Guid SessionId { get; set; } = default!; 
        public string Jti { get; set; } = default!; 
        public DateTime ExpiresAt { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }
    }
}
