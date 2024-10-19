using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORCharacterDevelopmentModel : DefaultCharacterDevelopmentModel
    {
        public override List<Tuple<SkillObject, int>> GetSkillsDerivedFromTraits(Hero hero, CharacterObject templateCharacter = null, bool isByNaturalGrowth = false)
        {
            var list = base.GetSkillsDerivedFromTraits(hero, templateCharacter, isByNaturalGrowth);
            var character = templateCharacter == null ? hero.CharacterObject : templateCharacter;
            var spellCastingtraitLevel = character.GetTraitLevel(TORCharacterTraits.SpellCasterSkills);
            if(spellCastingtraitLevel > 0)
            {
                list.Add(new Tuple<SkillObject, int>(TORSkills.SpellCraft, 100));
            }
            
            var GunpowderSkill = character.GetTraitLevel(TORCharacterTraits.Gunner);
            if(GunpowderSkill > 0)
            {
                list.Add(new Tuple<SkillObject, int>(TORSkills.GunPowder, 50));
            }

            var shallyaLevel = character.GetTraitLevel(TORCharacterTraits.ShallyaDevoted);
            
            if(shallyaLevel > 0)
            {
                list.Add(new Tuple<SkillObject, int>(TORSkills.Faith, 50));
            }
            
            var sigmarLevel = character.GetTraitLevel(TORCharacterTraits.SigmarDevoted);
            
            if(sigmarLevel > 0)
            {
                list.Add(new Tuple<SkillObject, int>(TORSkills.Faith, 50));
            }
            var ulricLevel = character.GetTraitLevel(TORCharacterTraits.UlricDevoted);
            if(ulricLevel > 0)
            {
                list.Add(new Tuple<SkillObject, int>(TORSkills.Faith, 50));
            }
            
            return list;
        }

        public override void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int xpValue, out int traitLevel, out int clampedTraitXp)
        {
            base.GetTraitLevelForTraitXp(hero, trait, xpValue, out traitLevel, out clampedTraitXp);
            
            if(xpValue<-500) return; //fail save -1500 traitvalue for killing lords is a bit much :)
            if (hero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                if (trait.StringId == "Valor" && xpValue < 0)
                {
                    return;
                } 
                if (trait.StringId == "Mercy" || trait.StringId == "Honor" || trait.StringId == "Valor")
                {
                    hero.AddCustomResource("Chivalry",xpValue);
                }
            }
        }
        public override int AttributePointsAtStart => base.AttributePointsAtStart + 3;
    }
}