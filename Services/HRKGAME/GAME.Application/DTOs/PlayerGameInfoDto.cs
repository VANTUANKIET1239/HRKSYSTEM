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
}
