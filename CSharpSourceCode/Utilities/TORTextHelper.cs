using NLog;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions
{
    public static class TORTextHelper
    {
        private static readonly HashSet<string> _missingTextsLogged = new();

        public static TextObject GetTextObjectOfSkillId(string skillId)
        {
            List<SkillObject> skills = Game.Current.DefaultSkills.GetDefaultSkills();

            skills.AddRange(TORSkills.Instance.GetTorSkills());

            return skills.FirstOrDefault(skill => skillId == skill.StringId)?.Name;
        }

        public static TextObject GetTextObjectOfAttribute(string attributeId)
        {
            attributeId = attributeId.ToLower();
            List<CharacterAttribute> attributes = Game.Current.DefaultCharacterAttributes.GetCharacterAttributes();
            
            attributes.Add(TORAttributes.Discipline);

            return attributes.FirstOrDefault(attribute => attributeId == attribute.StringId)?.Name;
        }

        public static string GetText(string id, string defaultText, bool skipValidation = true)
        {
            var text = GetTextObject(id, defaultText, skipValidation);
            return text.ToString();
        }

        public static string GetText(string id, string variation, string defaultText, bool skipValidation = true)
        {
            var text = GetTextObject(id, variation, defaultText, skipValidation);
            return text.ToString();
        }

        /// <summary>
        /// Intended for fetching text that will be passed into a native data structure that is initialized during load when variables are not yet available for setting.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This differs from GetText in that it still contains the localization id as the TextObject the string is placed in will then have it tokenized permitting fetching of the local language equivalent.
        /// </para>
        /// <para>
        /// Native code, eg. AddDialogLine (also  AddPlayerLine, AddWaitGameMenu, AddGameMenuOption, CreateDialogFlow, NpcLine, DisplayMessage, AddGameMenu, PlayerLine, etc.), takes strings in for its text which are then placed into a TextObject during ConversationSentence construction which is the point at which localization is performed.
        /// </para>
        /// <para>
        /// When the string representation of the TextObject fetched by TORTextHelper.GetTextObject is passed into the constructor, the variables are stripped because Conversations are built during campaign load and the 0 length value at that moment replaces the variable's token. Setting of the variables occurs on condition evaluation and so the raw variable text must be kept intact in the string when the argument is passed into the constructor.
        /// </para>
        /// </remarks>
        /// <returns>A string with its localization id and variables present.</returns>
        public static string GetTextForNative(string id, string variation, string defaultText, bool skipValidation = true)
        {
            var text = GetTextObject(id, variation, defaultText, skipValidation);
            return text.Value;
        }

        public static string GetTextForNative(string id, string defaultText, bool skipValidation = true)
        {
            var text = GetTextObject(id, defaultText, skipValidation);
            return text.Value;
        }


        public static TextObject GetTextObject(string id, string defaultText, bool skipValidation = true)
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
                TORCommon.Log(string.Format("[TEXT]Couldn't find text with id: {0}. Substituting default: {1}", id, defaultText), LogLevel.Error);
            }

            return new TextObject(defaultText);
        }

        public static TextObject GetTextObject(string id, string variation, string defaultText, bool skipValidation = true)
        {
            if (GameTexts.TryGetText(id, out var textObject, variation))
            {
                if (!skipValidation)
                {
                    var pureText = textObject.GetNativeTextWithoutTag();
                    if (pureText != defaultText)
                    {
                        TORCommon.Log(string.Format("[TEXT]Code text mismatches TOR XML text.{0}.{1}, \n XML : {2} \n CODE: {3}", id, variation, pureText, defaultText), LogLevel.Warn);
                    }
                }

                return textObject;
                
            }

            var key = $"{id}::{variation}";
            if (_missingTextsLogged.Add(key))
            {
                TORCommon.Log(string.Format("[TEXT]Couldn't find text with id: {0} {1}. Substituting default.", id, variation), LogLevel.Error);
            }

            return new TextObject(defaultText);
        }
    }
}