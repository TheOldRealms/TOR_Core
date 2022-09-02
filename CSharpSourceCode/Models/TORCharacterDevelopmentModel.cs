using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Models
{
    public class TORCharacterDevelopmentModel : DefaultCharacterDevelopmentModel
    {
        public override List<Tuple<SkillObject, int>> GetSkillsDerivedFromTraits(Hero hero, CharacterObject templateCharacter = null, bool isByNaturalGrowth = false)
        {
            var list = base.GetSkillsDerivedFromTraits(hero, templateCharacter, isByNaturalGrowth);
            var character = templateCharacter == null ? hero.CharacterObject : templateCharacter;
            var traitLevel = character.GetTraitLevel(TORCharacterTraits.SpellCasterSkills);
            if(traitLevel > 0)
            {
                list.Add(new Tuple<SkillObject, int>(TORSkills.SpellCraft, 100));
            }
            return list;
        }

        public override int AttributePointsAtStart => base.AttributePointsAtStart + 3;
    }
}
