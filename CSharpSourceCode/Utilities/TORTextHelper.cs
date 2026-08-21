using NLog;
using System.Collections.Generic;
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

        /// <remarks>
        /// Conversation strings are stored during game load when their variables are still unknown. TextObject.ToString can't be used because it writes the empty value into the variable token and the string will be missing important information. TextObject.Value.ToString likewise can't be used because it would keep the localization id in the string which is only needed when fetching a TextObject language variant and has already occurred before the return.
        /// </remarks>
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

        /// <summary>
        /// 
        /// </summary>
        /// <remarks>
        /// Conversations (DialogLine and PlayerLine) take strings in for their text which are then placed into a TextObject during ConversationSentence construction which is the point at which localization is performed. If the string representation of the original text is passed as a string into the constructor, the variables are effectively stripped because Conversations are built during campaign load when their values have not yet been set. Setting of the variables occurs on Line condition evaluation and so the raw variable text must be kept intact in the string when the argument is passed into the constructor.
        /// Menus similarly pass a string into a text object and have the same issue.
        /// </remarks>
        /// <returns>The value of the TextObject to be passed into a Conversation being created.</returns>
        public static string GetTextForNative(string id, string variation, string defaultText, bool skipValidation = false)
        {
            var text = GetTextObject(id, variation, defaultText, skipValidation);
            return text.Value;
        }

        public static string GetTextForNative(string id, string defaultText, bool skipValidation = false)
        {
            var text = GetTextObject(id, defaultText, skipValidation);
            return text.Value;
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
                        TORCommon.Log(string.Format("[TEXT]Code text mismatches TOR XML text.{0}.{1}, \n XML : {2} \n CODE: {3}", id,variation,pureText,defaultText), LogLevel.Warn);
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