using System;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.GameManagers
{
    public class TORGameKeyContext : GameKeyContext
    {
        //Sly : This assumes that no other mod is adding keys with the same indices. We're trapped by a poor native design that increases the risk of compatability issues with other mods.

        
        //Sly : Native code casts the enums of "GameKey" to strings for the ids, then reads them out. So the first ~116 positions are blocked since their names are predefined by the GameKeyDefinition enum.
        public const int NativeKeyCount = (int)GameKeyDefinition.TotalGameKeyCount;
        //GameKeyContext constructor takes in an integer and iterates to (i < count) so the TotalGameKeyCount is 1 higher than the number of keys in the dictionary.
        //Due to casting enums to strings for the string ids, TotalGameKeyCount can't be used and we therefore need a set of values with no overlap => dictionary count + 1 in construction.
        public const int AbilitySelectionMenu = NativeKeyCount + 1;
        public const int QuickCast = NativeKeyCount + 2;
        public const int CareerAbilityCast = NativeKeyCount + 3;

        //Sly : why did we pass a 120 argument for gameKeysCount?
        //Because the game is superposing the categories. As long as 2 categories are not registered for usage in a context, their shared keys on bindings don't matter. But the entire list of keys shares the same mapping to enums.
        //Each subcategory has null entries for all of the key actions that it doesn't use. But also the key actions are specific to each category, so I think it's just due to a design choice in the past that is bothersome for modders to use.
        //GameKeyContextType.AuxiliarySerializedAndShownInOptions is for hotkeys only which are things like navigation menu keys and other things with functions outside of the gameplay contexts which are instead GameKeys.
        public TORGameKeyContext() : base(nameof(TORGameKeyContext), (NativeKeyCount + Enum.GetValues(typeof(TorKeyMap)).Length + 1), GameKeyContextType.Default) 
        {
            RegisterGameKey(new GameKey(AbilitySelectionMenu, "AbilitySelectionMenu", nameof(TORGameKeyContext), InputKey.Q, InputKey.ControllerLLeft, nameof(TORGameKeyContext)));//dpad left
            RegisterGameKey(new GameKey(QuickCast, "QuickCast", nameof(TORGameKeyContext), InputKey.MiddleMouseButton, InputKey.Invalid, nameof(TORGameKeyContext)));
            RegisterGameKey(new GameKey(CareerAbilityCast, "CareerAbilityCast", nameof(TORGameKeyContext), InputKey.RightShift, InputKey.Invalid, nameof(TORGameKeyContext)));
        }
    }

    public enum TorKeyMap
    {
        AbilitySelectionMenu = TORGameKeyContext.AbilitySelectionMenu,
        QuickCast = TORGameKeyContext.QuickCast,
        CareerAbilityCast = TORGameKeyContext.CareerAbilityCast
    }
}