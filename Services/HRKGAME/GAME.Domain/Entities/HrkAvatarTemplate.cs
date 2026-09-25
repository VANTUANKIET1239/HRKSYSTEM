namespace GAME.Domain.Entities
{
    public class HrkAvatarTemplate
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ImagePath { get; set; } = null!;
        public int DisplayOrder { get; set; }
        public bool IsDefault { get; set; }
        public bool IsEnabled { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public virtual ICollection<HrkPlayer> Players { get; set; } = new List<HrkPlayer>();
    }
}
