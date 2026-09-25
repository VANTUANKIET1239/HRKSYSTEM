namespace GAME.Domain.Entities
{
    public class HrkPlayerCustomAvatar
    {
        public long PlayerId { get; set; }
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = null!;
        public string? FileName { get; set; }
        public int FileSize { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public string ContentHash { get; set; } = null!;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public virtual HrkPlayer Player { get; set; } = null!;
    }
}
