using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;

namespace TOR_Core.Extensions
{
    public static class TORTextHelper
    {


        public static TextObject GetTextObjectOfSkillId(string SkillId)
        {
            List<SkillObject> skills = Game.Current.DefaultSkills.GetDefaultSkills();

            skills.AddRange (TORSkills.Instance.GetTorSkills());

            return skills.FirstOrDefault(skill => SkillId == skill.StringId)?.Name;
        }
        
        public static TextObject GetTextObjectOfAttribute(string AttributeId)
        {
            AttributeId = AttributeId.ToLower();
            List<CharacterAttribute> attributes = Game.Current.DefaultCharacterAttributes.GetCharacterAttributes();

            attributes.Add  (TORAttributes.Discipline);

            return attributes.FirstOrDefault(attribute => AttributeId == attribute.StringId)?.Name;
        }


        
    }
}