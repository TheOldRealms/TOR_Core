namespace TOR_Core.Utilities
{
    using TaleWorlds.Localization;

    public static class TextObjectExtension
    {
        /// <summary>
        /// Gets the text from a TextObject and removes the translation tag {=...} while keeping variables like {VARIABLE} intact.
        /// </summary>
        public static string GetNativeTextWithoutTag(this TextObject textObject)
        {
            if (textObject == null) return string.Empty;

            string text = textObject.Value;

            // Translation tags are always at the beginning: {=tag_id}Rest of text {VARIABLE}
            // We want to remove only the {=tag_id} part
            if (text.StartsWith("{="))
            {
                int closingBraceIndex = text.IndexOf('}');
                if (closingBraceIndex > 0)
                {
                    // Remove everything from start up to and including the first }
                    return text.Substring(closingBraceIndex + 1);
                }
            }

            return text;
        }
    }
}