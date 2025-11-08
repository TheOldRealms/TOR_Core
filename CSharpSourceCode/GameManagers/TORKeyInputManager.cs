using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;

namespace TOR_Core.GameManagers
{
    public static  class TORKeyInputManager
    {
        public static void Initialize()
        {
            InitializeHotKeyManager();
        }
        
        private static void InitializeHotKeyManager()
        {
            var list = HotKeyManager.GetAllCategories();
            List<GameKeyContext> keyList = new List<GameKeyContext>();
            foreach(var item in list)
            {
                keyList.Add(item);
            }
            if(!keyList.Any(x => x is TORGameKeyContext)) keyList.Add(new TORGameKeyContext());
            HotKeyManager.RegisterInitialContexts(keyList, true);

            var context = nameof(TORGameKeyContext);
            var ContextTitleElement = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_category_name");
            ContextTitleElement.AddVariationWithId(context, new TaleWorlds.Localization.TextObject("The Old Realms"), new List<GameTextManager.ChoiceTag>());
            
            var KeyElementBindings = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name");
            var spellcastingModeKey = context + "_"+(int)TorKeyMap.QuickCastSelectionMenu;
            KeyElementBindings.AddVariationWithId(spellcastingModeKey, new TextObject ("{=tor_spellcasting_mode_key_str}Quick Cast Selection Menu"), new List<GameTextManager.ChoiceTag>());
            
            var quickCast = context + "_"+(int)TorKeyMap.QuickCast;
            KeyElementBindings.AddVariationWithId(quickCast, new TaleWorlds.Localization.TextObject("{=tor_quick_cast_key_str}Quick Cast"), new List<GameTextManager.ChoiceTag>());
            
            var specialMove = context+ "_"+(int)TorKeyMap.CareerAbilityCast;
            KeyElementBindings.AddVariationWithId(specialMove, new TaleWorlds.Localization.TextObject("{=tor_career_ability_key_str}Career Ability"), new List<GameTextManager.ChoiceTag>());


            var spellCastingModeDescription = new TextObject ("{=tor_spellcasting_mode_description_str}1) During battle pressing Q puts you into aiming mode, a reticule will appear and you will be able to target your spells. Aiming mode is accompanied by a visual animation on your character." + "\n" +
                                                     "2) Using the mouse wheel will switch between your spells (if you have learned multiple)" + "\n" +
                                                     "3) New UI elements will appear on the bottom left of your screen, with an image representing your selected spell and a cooldown times " + "\n" +
                                                     "4) Spells are reliant on the user's Winds of Magic (mana), this is fairly self-explanatory. It is regained over time on the campaign map.");
            var KeyDescriptionElement = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description");
            KeyDescriptionElement.AddVariationWithId(spellcastingModeKey, spellCastingModeDescription, new List<GameTextManager.ChoiceTag>());
            KeyDescriptionElement.AddVariationWithId(quickCast, new TaleWorlds.Localization.TextObject(
                "{=tor_quick_cast_description_str}Cast spells or prayers without needing to switch to spell cast mode. Warning: There is no target indication and the Spells follow predefined casting order. For targeted casting, always use the Spellcaster mode"), new List<GameTextManager.ChoiceTag>());
            KeyDescriptionElement.AddVariationWithId(specialMove, new TaleWorlds.Localization.TextObject(
                "{=tor_career_ability_description_str}Starting Career ability, like the mistform for the Vampire Counts, or Knightly strike of the Grailknight"), new List<GameTextManager.ChoiceTag>());
        }
    }
    
}