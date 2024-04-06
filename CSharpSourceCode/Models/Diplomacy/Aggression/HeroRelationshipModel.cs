using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.Models.Diplomacy.Aggression
{
    public class HeroRelationshipModel
    {
        public HeroRelationshipModel(Hero hero1, Hero hero2, int relationship)
        {
            HeroesDevotion = new Dictionary<MBGUID, DevotionLevel>
            {
                { hero1.Id, hero1.GetDevotionToDominantReligion() },
                { hero2.Id, hero2.GetDevotionToDominantReligion() }
            };

            RelationshipLevel = relationship;
        }
        public Dictionary<MBGUID, DevotionLevel> HeroesDevotion {  get; }
        public int RelationshipLevel { get; set; }
    }
}
