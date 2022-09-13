using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Utilities;

namespace TOR_Core.GameManagers
{
    public class TORGameKeyContext : GameKeyContext
    {
        public const string SpellCasterModeVariable = "SpellCastingMode";
        public const int Spellcasting = 109;
        public TORGameKeyContext() :  base(nameof(TORGameKeyContext),110)
        {
           // var t = Module.CurrentModule.GlobalTextManager.FindAllTextVariations("str_key_category_name");
           //var t = GameTexts.FindText("str_key_category_name" ,nameof(TORGameKeyContext));
            //Module.CurrentModule.GlobalTextManager.AddGameText(t.GetID());
            
            this.RegisterGameKey(new GameKey(Spellcasting, "Spellcasting", nameof(TORGameKeyContext), InputKey.Q, nameof(TORGameKeyContext)));
            //this.RegisterGameKey(new GameKey(1, "Spellcasting", nameof(TORGameKeyContext), InputKey.Q, nameof(TORGameKeyContext)));
            //this.RegisterTorHotKey(nameof (SpellCasterModeVariable), InputKey.Q, HotKey.Modifiers.None);
        }


        /*
        public TORGameKeyContext() :  base(nameof(TORGameKeyContext))
        {

            //this.RegisterGameKey(new GameKey(9, "Attack", nameof (CombatHotKeyCategory), InputKey.Q, TorKeyCateogries.SpellcastingCategory));
            

            if (this.RegisteredGameKeys.Count > 0)
            {
                TORCommon.Say("hello");
            }

        }
        */
        
        
        
        
    }
    
    public static class TorKeyCateogries
    {
        public static string SpellcastingCategory = nameof (SpellcastingCategory);
  
    }

    public enum TorKeyMap
    {
        TorSpellcasting = 109
    }
}