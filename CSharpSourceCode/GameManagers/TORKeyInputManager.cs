using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

namespace TOR_Core.GameManagers
{
    public static  class TORKeyInputManager
    {
        public static void Initialize()
        {
            InitializeTorSpecificKeys();
        }
        
        private static void InitializeTorSpecificKeys()
        {
            HotKeyManager.RegisterInitialContexts((IEnumerable<GameKeyContext>) new List<GameKeyContext>()
            {
                (GameKeyContext) new TORGameKeyContext(),
            }, true);

            var context = nameof(TORGameKeyContext);
            var ContextTitleElement = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_category_name");
            ContextTitleElement.AddVariationWithId(context, new TaleWorlds.Localization.TextObject("The Old Realms"), new List<GameTextManager.ChoiceTag>());
            
            
            var KeyElementBindings = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name");
            var spellcastingModeKey = context + "_"+(int)TorKeyMap.SpellcastingMode;
            KeyElementBindings.AddVariationWithId(spellcastingModeKey, new TaleWorlds.Localization.TextObject("Spell Casting Mode"), new List<GameTextManager.ChoiceTag>());
            
            var nextSpellKey = context + "_"+(int)TorKeyMap.SelectNextAbility;
            KeyElementBindings.AddVariationWithId(nextSpellKey, new TaleWorlds.Localization.TextObject("Next Ability"), new List<GameTextManager.ChoiceTag>());
            
            var previousSpellKey = context + "_"+(int)TorKeyMap.SelectPreviousAbility;
            KeyElementBindings.AddVariationWithId(previousSpellKey, new TaleWorlds.Localization.TextObject("Last Ability"), new List<GameTextManager.ChoiceTag>());
            
            var quickCast = context + "_"+(int)TorKeyMap.QuickCast;
            KeyElementBindings.AddVariationWithId(quickCast, new TaleWorlds.Localization.TextObject("Quick Cast"), new List<GameTextManager.ChoiceTag>());
            
            var specialMove = context+ "_"+(int)TorKeyMap.SpecialMove;
            KeyElementBindings.AddVariationWithId(specialMove, new TaleWorlds.Localization.TextObject("Special Move"), new List<GameTextManager.ChoiceTag>());
            
            var KeyDescriptionElement = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description");
            var keyDescription = context +"_"+(int)TorKeyMap.SpellcastingMode;
            KeyDescriptionElement.AddVariationWithId(spellcastingModeKey, new TaleWorlds.Localization.TextObject(
                "1) During battle pressing Q puts you into aiming mode, a reticule will appear and you will be able to target your spells. Aiming mode is accompanied by a visual animation on your character."+"\n"+
                "2) Using the mouse wheel will switch between your spells (if you have learned multiple)"+"\n"+
                "3) New UI elements will appear on the bottom left of your screen, with an image representing your selected spell and a cooldown times "+"\n"+
                "4) Spells are reliant on the user's Winds of Magic (mana), this is fairly self-explanatory. It is regained over time on the campaign map."), new List<GameTextManager.ChoiceTag>());
            
            

        }
    }
    
}