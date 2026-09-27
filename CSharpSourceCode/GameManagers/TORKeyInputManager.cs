using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.GameManagers
{
    public static class TORKeyInputManager
    {
        public static void Initialize()
        {
            InitializeHotKeyManager();
        }

        // Note: We cannot use TORTextHelper here because the GameTextManager it relies on is not yet instantiated.
        // We therefore use the GlobalTextManager which is filled with the strings in the global_strings.xml for each mod that has one. Its usage is for the strings that appear anywhere before a campaign is loaded.
        //Sly : I hate that the keys are stored as indices in a list based on their enum value which means you can't reliably predict where your key will land at without ballooning the number of entries in the list.
        private static void InitializeHotKeyManager()
        {
            HotKeyManager.RegisterContext(new TORGameKeyContext());


            //Sly : this doesn't work bro, the {newline} variables are lost.
            //TODO : fix yo shit.
            var context = nameof(TORGameKeyContext);
            var ContextTitleElement = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_category_name");
            ContextTitleElement.AddVariationWithId(context, Module.CurrentModule.GlobalTextManager.GetGameText("str_key_category_name").Variations.First(x => x.Id == "tor_key_category_name").Text, new List<GameTextManager.ChoiceTag>());

            var KeyElementBindings = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name");

            var spellcastingModeKey = context + "_" + (int)TorKeyMap.AbilitySelectionMenu;
            KeyElementBindings.AddVariationWithId(spellcastingModeKey, Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name").Variations.First(x => x.Id == "TORGameKeyContext_AbilitySelectionMenu").Text, new List<GameTextManager.ChoiceTag>());

            var quickCast = context + "_" + (int)TorKeyMap.QuickCast;
            KeyElementBindings.AddVariationWithId(quickCast, Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name").Variations.First(x => x.Id == "TORGameKeyContext_QuickCast").Text, new List<GameTextManager.ChoiceTag>());

            var specialMove = context + "_" + (int)TorKeyMap.CareerAbilityCast;
            KeyElementBindings.AddVariationWithId(specialMove, Module.CurrentModule.GlobalTextManager.GetGameText("str_key_name").Variations.First(x => x.Id == "TORGameKeyContext_CareerAbilityCast").Text, new List<GameTextManager.ChoiceTag>());



            var KeyDescriptionElement = Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description");

            KeyDescriptionElement.AddVariationWithId(spellcastingModeKey, Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description").Variations.First(x => x.Id == "TORGameKeyContext_AbilitySelectionMenu").Text, new List<GameTextManager.ChoiceTag>());
            KeyDescriptionElement.AddVariationWithId(quickCast, Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description").Variations.First(x => x.Id == "TORGameKeyContext_QuickCast").Text, new List<GameTextManager.ChoiceTag>());
            KeyDescriptionElement.AddVariationWithId(specialMove, Module.CurrentModule.GlobalTextManager.GetGameText("str_key_description").Variations.First(x => x.Id == "TORGameKeyContext_CareerAbilityCast").Text, new List<GameTextManager.ChoiceTag>());
        }
    }

}