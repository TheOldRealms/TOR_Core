using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace TOR_Core.Utilities
{
    public class TORGameMenuBackgroundSwitcher
    {
        /** Janky fix for https://github.com/TheOldRealms/TOW_Core/issues/213
         *
         *  Refers to the radius at which npc parties raid villages / settlements
         *  Adjust as needed if the call in 
         *  game_menu_ui_village_hostile_raid_on_init_tow becomes expensive for
         *  whatever reason.
         *  
         *  Lower value = less expensive.
         */
        private static readonly float RAID_RADIUS = 20f;
        [GameMenuInitializationHandler("village_looted")]
        [GameMenuInitializationHandler("village_raid_ended_leaded_by_someone_else")]
        [GameMenuInitializationHandler("raiding_village")]
        private static void game_menu_ui_village_hostile_raid_on_init_tow(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement
                ?? TORCommon.FindNearestSettlement(MobileParty.MainParty, RAID_RADIUS)
                ?? null;

            if (settlement == null || settlement.Culture == null)
            {
                args.MenuContext.SetBackgroundMeshName("wait_raiding_village");
                return;
            }

            switch (settlement.Culture.StringId)
            {
                case TORConstants.Cultures.EMPIRE:
                    args.MenuContext.SetBackgroundMeshName("empire_looted_village");
                    return;
                case TORConstants.Cultures.SYLVANIA:
                    args.MenuContext.SetBackgroundMeshName("vampire_looted_village");
                    return;
                case TORConstants.Cultures.BRETONNIA:
                    args.MenuContext.SetBackgroundMeshName("bretonnia_looted_village");
                    return;
                case TORConstants.Cultures.EONIR:
                case TORConstants.Cultures.ASRAI:
                    args.MenuContext.SetBackgroundMeshName("we_village_burned");
                    return;
                default:
                    args.MenuContext.SetBackgroundMeshName("wait_raiding_village");
                    return;
            }
        }

        [GameMenuInitializationHandler("town_arena")]
        private static void game_menu_town_menu_arena_on_init_tow(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement
                ?? TORCommon.FindNearestSettlement(MobileParty.MainParty, RAID_RADIUS)
                ?? null;

            if(settlement != null)
            {
                
                if(settlement.Culture.StringId == TORConstants.Cultures.MOUSILLON)
                {
                    args.MenuContext.SetBackgroundMeshName("bretonnia_arena");
                    return;
                }
                if(settlement.Culture.StringId == TORConstants.Cultures.BRETONNIA)
                {
                    args.MenuContext.SetBackgroundMeshName("bretonnia_arena");
                    return;
                }
                else if (settlement.Culture.StringId == TORConstants.Cultures.SYLVANIA)
                {
                    args.MenuContext.SetBackgroundMeshName("vampire_arena");
                    return;
                }
            }
            
            args.MenuContext.SetBackgroundMeshName("generic_arena");
            //args.MenuContext.SetAmbientSound("event:/map/ambient/node/settlements/2d/arena");
        }

        [GameMenuInitializationHandler("prisoner_wait")]
        [GameMenuInitializationHandler("taken_prisoner")]
        [GameMenuInitializationHandler("menu_captivity_end_no_more_enemies")]
        [GameMenuInitializationHandler("menu_captivity_end_by_ally_party_saved")]
        [GameMenuInitializationHandler("menu_captivity_end_by_party_removed")]
        [GameMenuInitializationHandler("menu_captivity_end_wilderness_escape")]
        [GameMenuInitializationHandler("menu_escape_captivity_during_battle")]
        [GameMenuInitializationHandler("menu_released_after_battle")]
        [GameMenuInitializationHandler("menu_captivity_end_propose_ransom_wilderness")]
        private static void wait_menu_ui_prisoner_wait_on_init_tow(MenuCallbackArgs args)
        {
            var culture = Hero.MainHero.Culture;
            if (culture == null)
            {
                args.MenuContext.SetBackgroundMeshName("wait_captive_male");
                return;
            }

            switch (culture.StringId)
            {
                case TORConstants.Cultures.EMPIRE:
                    args.MenuContext.SetBackgroundMeshName("empire_captive");
                    return;
                case TORConstants.Cultures.SYLVANIA:
                    args.MenuContext.SetBackgroundMeshName("vampire_captive");
                    return;
                case TORConstants.Cultures.BRETONNIA:
                    args.MenuContext.SetBackgroundMeshName("bretonnia_captive");
                    return;
                case TORConstants.Cultures.MOUSILLON:
                    args.MenuContext.SetBackgroundMeshName("bretonnia_captive");
                    return;
                case TORConstants.Cultures.ASRAI:
                case TORConstants.Cultures.EONIR:
                    args.MenuContext.SetBackgroundMeshName("wood_elves_captive");
                    return;
                default:
                    args.MenuContext.SetBackgroundMeshName("wait_captive_male");
                    return;
            }
        }
        [GameMenuInitializationHandler("town_backstreet")]
        private static void wait_menu_ui_town_backstreet_on_init_tor(MenuCallbackArgs args)
        {
            var settlement = Settlement.CurrentSettlement;

            if (settlement == null) return;
            
            if (settlement.Culture == null)
            {
                args.MenuContext.SetBackgroundMeshName("vlandia_backstreet");
                return;
            }

            switch (settlement.Culture.StringId)
            {
                case "mousillon":
                    args.MenuContext.SetBackgroundMeshName("vlandia_tavern");
                    return;
                case "eonir":
                    args.MenuContext.SetBackgroundMeshName("eonir_town_background");
                    return;
            }
        }
        
        [GameMenuInitializationHandler("town_keep")]
        private static void wait_menu_ui_town_keep_on_init_tor(MenuCallbackArgs args)
        {
            var culture = Hero.MainHero.Culture;
            if (culture == null)
            {
                args.MenuContext.SetBackgroundMeshName("bretonnia_city");
                return;
            }

            switch (culture.StringId)
            {
                case "mousillon":
                    args.MenuContext.SetBackgroundMeshName("bretonnia_city_background");
                    return;
                case "eonir":
                    args.MenuContext.SetBackgroundMeshName("eonir_town_background");
                    return;
            }
        }
        
        
        
        
    }
}
