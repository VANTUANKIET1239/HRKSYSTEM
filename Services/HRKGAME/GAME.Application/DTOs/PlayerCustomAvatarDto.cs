using System;

namespace GAME.Application.DTOs
{

    public class PlayerCustomAvatarDto
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = null!;
        public string ContentHash { get; set; } = null!;
    }
}
