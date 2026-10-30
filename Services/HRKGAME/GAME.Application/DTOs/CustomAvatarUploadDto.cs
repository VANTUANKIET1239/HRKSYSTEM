using System;

namespace GAME.Application.DTOs
{

    public class CustomAvatarUploadDto
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = null!;
        public string? FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
