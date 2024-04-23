﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORCharacterDevelopmentModel : DefaultCharacterDevelopmentModel
    {
        public EventHandler<TraitLevelIncreaseEventArgs> OntraitLevelIncreased;
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

        public override void GetTraitLevelForTraitXp(Hero hero, TraitObject trait, int xpValue, out int traitLevel, out int clampedTraitXp)
        {
            base.GetTraitLevelForTraitXp(hero, trait, xpValue, out traitLevel, out clampedTraitXp);

            var level = traitLevel;
            var t = clampedTraitXp;
            if (hero.Culture.StringId == "vlandia")
            {
                if (trait.StringId == "Mercy" || trait.StringId == "Honor" || trait.StringId == "Valor")
                {
                    hero.AddCustomResource("Chivilary",xpValue);
                }
            }
        }
        public override int AttributePointsAtStart => base.AttributePointsAtStart + 3;
    }

    public class TraitLevelIncreaseEventArgs : EventArgs
    {
        public TraitLevelIncreaseEventArgs(int xpValue, TraitObject traitObject)
        {
            
        }
    }
}