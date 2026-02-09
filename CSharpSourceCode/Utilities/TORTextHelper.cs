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
        private static readonly HashSet<string> _missingTextsLogged = new();
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


        public static string GetText(string id, string defaultText, bool skipValidation = false)
        {
            var text = GetTextObject(id, defaultText, skipValidation);
            return text.ToString();
        }

        public static string GetText(string id, string variation, string defaultText, bool skipValidation = false)
        {
            var text = GetTextObject(id, variation, defaultText, skipValidation);
            return text.ToString();
        }


        public static TextObject GetTextObject(string id, string defaultText, bool skipValidation = false)
        {
            if (GameTexts.TryGetText(id, out var textObject))
            {
                if (!skipValidation)
                {
                    var pureText = textObject.GetNativeTextWithoutTag();
                    if (pureText != defaultText)
                    {
                        TORCommon.Log(string.Format("Code text mismatches TOR XML text for {0}. \n XML : {1}\n CODE: {2}", id, pureText, defaultText), LogLevel.Warn);
                    }
                }

                return textObject;
            }

            if (_missingTextsLogged.Add(id))
            {
                TORCommon.Log(string.Format("[TEXT]Couldn't find text with id: {0}.  switch to default: {1}", id, defaultText), LogLevel.Error);
            }

            return new TextObject(defaultText);
        }

        public static TextObject GetTextObject(string id, string variation, string defaultText, bool skipValidation = false)
        {
            if (GameTexts.TryGetText(id, out var textObject, variation))
            {
                if (!skipValidation)
                {
                    var pureText = textObject.GetNativeTextWithoutTag();
                    if (pureText != defaultText)
                    {
                        TORCommon.Log(string.Format("[TEXT]Code text mismatches TOR XML text.{0}, \n XML : {1} \n CODE: {2}", id,pureText,defaultText), LogLevel.Warn);
                    }
                }

                return textObject;
                
            }
            var key = $"{id}::{variation}";
            if (_missingTextsLogged.Add(key))
            {
                TORCommon.Log(string.Format("[TEXT]Couldn't find text with id: {0} {1}.  switch to default.", id, variation), LogLevel.Error);
            }

            return new TextObject(defaultText);
        }
    }
}