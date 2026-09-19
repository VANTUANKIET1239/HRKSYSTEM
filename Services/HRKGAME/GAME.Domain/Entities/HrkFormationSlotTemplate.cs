namespace GAME.Domain.Entities
{
    public class HrkFormationSlotTemplate
    {
        public int Id { get; set; }
        public int FormationTemplateId { get; set; }
        public int Slot { get; set; } // 1 to 5
        public string RowType { get; set; } = "FRONT"; // FRONT, BACK
        public int Lane { get; set; } // 1 to 5
        public int DisplayX { get; set; }
        public int DisplayY { get; set; }
        public bool IsEnabled { get; set; } = true;

        public virtual HrkFormationTemplate FormationTemplate { get; set; } = null!;
    }
}
