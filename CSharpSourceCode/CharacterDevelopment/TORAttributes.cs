using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment
{
    /// <summary>
    /// Discipline was retired when Faith / Gunpowder / Spellcraft moved onto
    /// vanilla attributes. The accessor stays for binary compatibility with
    /// callers that still reference it; it now returns null and those
    /// callers must null-check before dereferencing.
    /// </summary>
    public class TORAttributes
    {
        public static TORAttributes Instance { get; private set; }

        public static CharacterAttribute Discipline => null;

        public TORAttributes()
        {
            Instance = this;
        }
    }
}