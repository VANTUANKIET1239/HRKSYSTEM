using System;

namespace GAME.Application.DTOs
{

    public class PlayerAvatarTemplateDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string ImagePath { get; set; } = null!;
        public bool IsSelected { get; set; }
    }
}
