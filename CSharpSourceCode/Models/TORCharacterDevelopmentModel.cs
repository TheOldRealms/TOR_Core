using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORCharacterDevelopmentModel : DefaultCharacterDevelopmentModel
    {
        /*
        public override List<Tuple<SkillObject, int>> GetSkillsDerivedFromTraits(Hero hero, CharacterObject templateCharacter = null, bool isByNaturalGrowth = false)
        {
            var list = base.GetSkillsDerivedFromTraits(hero, templateCharacter, isByNaturalGrowth);
            var character = templateCharacter == null ? hero.CharacterObject : templateCharacter;
            var spellCastingtraitLevel = character.GetTraitLevel(TORCharacterTraits.SpellCasterSkills);
            if(spellCastingtraitLevel > 0)
            {
                list.Add(new Tuple<SkillObject, int>(TORSkills.Spellcraft, 100));
            }
            
            var gunpowderSkill = character.GetTraitLevel(TORCharacterTraits.Gunner);
            if(gunpowderSkill > 0)
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
            
            var ladyLevel = character.GetTraitLevel(TORCharacterTraits.LadyDevoted);
            if(ladyLevel > 0)
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
        */

        
        /// <remarks>
        /// Copy of native code, but adjusted to exclude riding for dwarfs on npcs.
        /// </remarks>
        public override SkillObject GetNextSkillToAddFocus(Hero hero)
        {
            SkillObject skillObject = null;
			float num = float.MinValue;
			foreach (SkillObject skillObject2 in Skills.All)
			{
				if (hero.HeroDeveloper.CanAddFocusToSkill(skillObject2))
				{
                    if (hero.IsDwarf() && skillObject2 == DefaultSkills.Riding && hero.CharacterObject != CharacterObject.PlayerCharacter) continue; //Dwarfs won't auto-assign focus points into riding as they can't benefit from it normally. Player can manually assign points on themselves or companions to avoid this restriction if desired.

					int focus = hero.HeroDeveloper.GetFocus(skillObject2);
					float num2 = (float)hero.GetSkillValue(skillObject2) - Campaign.Current.Models.CharacterDevelopmentModel.CalculateLearningLimit(hero.CharacterAttributes, focus, skillObject2, false).ResultNumber;
					if (num2 > num)
					{
						num = num2;
						skillObject = skillObject2;
					}
				}
			}
			return skillObject;
        }

        public override void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int xpValue, out int traitLevel, out int clampedTraitXp)
        {
            base.GetTraitLevelForTraitXp(hero, trait, xpValue, out traitLevel, out clampedTraitXp);

            if (xpValue < -500) return; //fail save -1500 traitvalue for killing lords is a bit much :)
            if (hero.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                if (trait.StringId == "Valor" && xpValue < 0)
                {
                    return;
                }
                if (trait.StringId == "Mercy" || trait.StringId == "Honor" || trait.StringId == "Valor")
                {
                    hero.AddCustomResource("Chivalry", xpValue);
                }
            }
        }
        public override int AttributePointsAtStart => base.AttributePointsAtStart + 3;
    }
}