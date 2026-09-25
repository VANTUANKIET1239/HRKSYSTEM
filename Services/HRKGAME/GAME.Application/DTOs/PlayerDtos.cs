using System;

namespace GAME.Application.DTOs
{
    public class PlayerGameInfoDto
    {
        public PlayerProfileDto? Profile { get; set; }
        public PlayerWalletDto? Wallet { get; set; }
        public long? SelectedFormationId { get; set; }
        public string? SelectedFormationCode { get; set; }
        public string? SelectedFormationName { get; set; }
        public int FormationPower { get; set; }
    }
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

    public class PlayerAvatarTemplateDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ImagePath { get; set; } = null!;
        public bool IsSelected { get; set; }
    }

    public class PlayerCustomAvatarDto
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = null!;
        public string ContentHash { get; set; } = null!;
    }

    public class CustomAvatarUploadDto
    {
        public byte[] ImageData { get; set; } = Array.Empty<byte>();
        public string ContentType { get; set; } = null!;
        public string? FileName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class PlayerWalletDto
    {
        public long PlayerId { get; set; }
        public long Gold { get; set; }
        public int Diamonds { get; set; }
        public int UpgradeMaterials { get; set; }
        public int MaxCapacity { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
}
