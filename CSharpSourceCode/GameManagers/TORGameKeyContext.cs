using System.Collections.Generic;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.Utilities;

namespace TOR_Core.GameManagers
{
    public class TORGameKeyContext : GenericPanelGameKeyCategory
    {
        public const string SpellCasterModeVariable = "SpellCastingMode";
        
        
        

        public TORGameKeyContext() :  base("TOR")
        {
            //this.RegisterGameKey(new GameKey(9, "Attack", nameof (CombatHotKeyCategory), InputKey.Q, TorKeyCateogries.SpellcastingCategory));
            this.RegisterGameKey(new GameKey(57, "Spellcasting", TorKeyCateogries.SpellcastingCategory, InputKey.Q, nameof(TORGameKeyContext)));

            if (this.RegisteredGameKeys.Count > 0)
            {
                TORCommon.Say("hello");
            }

        }
        
        
        
        
    }
    
    public static class TorKeyCateogries
    {
        public static string SpellcastingCategory = nameof (SpellcastingCategory);
  
    }
}