using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.DotNet;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Utilities;

namespace TOR_Core.GameManagers
{
    public class TORGameKeyContext : GameKeyContext
    {
        public const int Spellcasting = (int) TorKeyMap.SpellcastingMode;
        public const int SelectNextAbility = (int) TorKeyMap.SelectNextAbility;
        public const int SelectPreviousAbility = (int)TorKeyMap.SelectPreviousAbility;
        public const int QuickCast = (int)TorKeyMap.QuickCast;
        public const int SpecialMove = (int)TorKeyMap.SpecialMove;
        public TORGameKeyContext() :  base(nameof(TORGameKeyContext),120) // I don't exactly why they do it, but they seem to cast the id enums of "GameKey" to strings, and then read them out. So the first 108 positions are blocked since their names are predefined of the GameKey enum.
        {
            this.RegisterGameKey(new GameKey(Spellcasting, "Spellcasting", nameof(TORGameKeyContext), InputKey.Q,nameof(TORGameKeyContext)));
            this.RegisterGameKey(new GameKey(SelectPreviousAbility, "PreviousAbility", nameof(TORGameKeyContext), InputKey.X2MouseButton,nameof(TORGameKeyContext)));
            this.RegisterGameKey(new GameKey(SelectNextAbility, "NextAbility", nameof(TORGameKeyContext), InputKey.X1MouseButton,nameof(TORGameKeyContext)));
            this.RegisterGameKey(new GameKey(QuickCast, "QuickCast", nameof(TORGameKeyContext), InputKey.MiddleMouseButton,nameof(TORGameKeyContext)));
            this.RegisterGameKey(new GameKey(SpecialMove, "SpecialMove", nameof(TORGameKeyContext), InputKey.LeftAlt,nameof(TORGameKeyContext)));
        }
    }

    public enum TorKeyMap
    {
        SpellcastingMode = 109,
        SelectNextAbility =110,
        SelectPreviousAbility =111,
        QuickCast=112,
        SpecialMove=113
    }
}