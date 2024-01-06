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
        public const int QuickCastSelectionMenu = (int) TorKeyMap.QuickCastSelectionMenu;
        public const int QuickCast = (int)TorKeyMap.QuickCast;
        public const int CareerAbilityCast = (int)TorKeyMap.CareerAbilityCast;
        public TORGameKeyContext() :  base(nameof(TORGameKeyContext),120) // I don't exactly know why they do it, but they seem to cast the id enums of "GameKey" to strings, and then read them out. So the first 108 positions are blocked since their names are predefined of the GameKey enum.
        {
            RegisterGameKey(new GameKey(QuickCastSelectionMenu, "QuickCastSelectionMenu", nameof(TORGameKeyContext), InputKey.Q, nameof(TORGameKeyContext)));
            RegisterGameKey(new GameKey(QuickCast, "QuickCast", nameof(TORGameKeyContext), InputKey.MiddleMouseButton, nameof(TORGameKeyContext)));
            RegisterGameKey(new GameKey(CareerAbilityCast, "CareerAbilityCast", nameof(TORGameKeyContext), InputKey.LeftAlt, nameof(TORGameKeyContext)));
        }
    }

    public enum TorKeyMap
    {
        QuickCastSelectionMenu = 109,
        QuickCast=110,
        CareerAbilityCast = 111
    }
}