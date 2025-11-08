using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Extensions
{
    public static class SkillExtensions
    {
        public static List<SkillObject> GetDefaultSkills(this DefaultSkills defaultSkills)
        {
            List<SkillObject> skills = new List<SkillObject>();
            
            skills.Add (DefaultSkills.OneHanded);
            skills.Add (DefaultSkills.TwoHanded);
            skills.Add (DefaultSkills.Polearm);
            
            skills.Add (DefaultSkills.Bow);
            skills.Add (DefaultSkills.Crossbow);
            skills.Add (DefaultSkills.Throwing);
            
            skills.Add (DefaultSkills.Athletics);
            skills.Add (DefaultSkills.Crafting);
            skills.Add (DefaultSkills.Riding);
            
            skills.Add (DefaultSkills.Tactics);
            skills.Add (DefaultSkills.Scouting);
            skills.Add (DefaultSkills.Roguery);
            
            skills.Add (DefaultSkills.Charm);
            skills.Add (DefaultSkills.Leadership);
            skills.Add (DefaultSkills.Trade);
            
            skills.Add (DefaultSkills.Engineering);
            skills.Add (DefaultSkills.Medicine);
            skills.Add (DefaultSkills.Steward);
            
            return skills;
        }

        public static List<SkillObject> GetTorSkills(this TORSkills torSkills)
        {
            List<SkillObject> skills = new List<SkillObject>();
            skills.Add (TORSkills.Faith);
            skills.Add (TORSkills.GunPowder);
            skills.Add (TORSkills.SpellCraft);
            return skills;
        }


        public static List<CharacterAttribute> GetCharacterAttributes(this DefaultCharacterAttributes characterAttributes)
        {
            List<CharacterAttribute> attributes =new List<CharacterAttribute>();
            attributes.Add (DefaultCharacterAttributes.Vigor);
            attributes.Add (DefaultCharacterAttributes.Control);
            attributes.Add (DefaultCharacterAttributes.Endurance);
            attributes.Add (DefaultCharacterAttributes.Cunning);
            attributes.Add (DefaultCharacterAttributes.Social);
            attributes.Add (DefaultCharacterAttributes.Intelligence);
            return attributes;
        }
        
    }
}