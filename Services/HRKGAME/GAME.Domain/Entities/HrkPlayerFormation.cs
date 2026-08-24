using System;

namespace GAME.Domain.Entities
{
    public class HrkPlayerFormation
    {
        public long PlayerId { get; set; }
        public string FormationName { get; set; } = "Main Team";
        public long? Position1 { get; set; }
        public long? Position2 { get; set; }
        public long? Position3 { get; set; }
        public long? Position4 { get; set; }
        public long? Position5 { get; set; }
        public int TotalPower { get; set; } = 0;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;

        public virtual HrkPlayer Player { get; set; } = null!;
        public virtual HrkPlayerHero? Hero1 { get; set; }
        public virtual HrkPlayerHero? Hero2 { get; set; }
        public virtual HrkPlayerHero? Hero3 { get; set; }
        public virtual HrkPlayerHero? Hero4 { get; set; }
        public virtual HrkPlayerHero? Hero5 { get; set; }
    }
}
