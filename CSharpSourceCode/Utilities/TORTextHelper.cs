using Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions
{
    public static class TORTextHelper
    {
        public static TextObject GetTextObjectOfSkillId(string SkillId)
        {
            List<SkillObject> skills = Game.Current.DefaultSkills.GetDefaultSkills();

            skills.AddRange(TORSkills.Instance.GetTorSkills());

            return skills.FirstOrDefault(skill => SkillId == skill.StringId)?.Name;
        }

        public static TextObject GetTextObjectOfAttribute(string AttributeId)
        {
            AttributeId = AttributeId.ToLower();
            List<CharacterAttribute> attributes = Game.Current.DefaultCharacterAttributes.GetCharacterAttributes();

            attributes.Add(TORAttributes.Discipline);

            return attributes.FirstOrDefault(attribute => AttributeId == attribute.StringId)?.Name;
        }


        public static string GetText(string id, string defaultText)
        {
            var text = GetTextObject(id, defaultText);
            return text.Value;
        }

        public static string GetText(string id, string variation, string defaultText)
        {
            var text = GetTextObject(id, variation, defaultText);
            return text.ToString();
        }


        public static TextObject GetTextObject(string id, string defaultText)
        {
            if (GameTexts.TryGetText(id, out var textObject))
            {
                var pureText = textObject.GetNativeTextWithoutTag();
                if (pureText != defaultText)
                {
                    TORCommon.Log(string.Format("Code text mismatches TOR XML text for {0}. \n {1},\n{2}", id, pureText, defaultText), LogLevel.Warn);
                }

                return textObject;
            }

            TORCommon.Log(string.Format("Couldn't find text with id: {0}.  switch to default", id), LogLevel.Error);
            return new TextObject(defaultText);
        }

        public static TextObject GetTextObject(string id, string variation, string defaultText)
        {
            if (GameTexts.TryGetText(id, out var textObject, variation))
            {
                var pureText = textObject.GetNativeTextWithoutTag();
                if (pureText != defaultText)
                {
                    TORCommon.Log(string.Format("Code text mismatches TOR XML text. \n {0},\n{0}", id), LogLevel.Warn);
                }

                return textObject;
            }

            TORCommon.Log(string.Format("Couldn't find text with id: {0}.  switch to default", id), LogLevel.Error);
            return new TextObject(defaultText);
        }
    }
}