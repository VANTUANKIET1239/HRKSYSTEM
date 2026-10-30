using System;

namespace GAME.Application.DTOs
{
    public class PlayerProfileDto
    {
        public long Id { get; set; }
        public string UserId { get; set; } = null!;
        public string PlayerName { get; set; } = null!;
        public int Level { get; set; }
        public int Exp { get; set; }
        public int MaxExp { get; set; }
        public int Power { get; set; }
        public string AvatarType { get; set; } = "TEMPLATE";
        public int? AvatarTemplateId { get; set; }
        public string? AvatarUrl { get; set; }
        public long AvatarVersion { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
