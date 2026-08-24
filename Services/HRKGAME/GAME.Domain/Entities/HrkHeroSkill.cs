namespace GAME.Domain.Entities
{
    public class HrkHeroSkill
    {
        public int HeroTemplateId { get; set; }
        public string SkillId { get; set; } = null!;
        public byte SkillOrder { get; set; } = 1;

        public virtual HrkHeroTemplate HeroTemplate { get; set; } = null!;
        public virtual HrkSkillTemplate Skill { get; set; } = null!;
    }
}
