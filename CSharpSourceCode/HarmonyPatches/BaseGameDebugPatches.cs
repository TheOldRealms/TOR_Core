using HarmonyLib;
using SandBox.CampaignBehaviors;
using SandBox.ViewModelCollection.Nameplate;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ModuleManager;
using TOR_Core.Extensions;
using TOR_Core.GameManagers;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class BaseGameDebugPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CraftingCampaignBehavior), "CreateTownOrder")]
        public static bool PreventCreateCraftingOrder(Hero orderOwner, int orderSlot)
        {
            return orderOwner != null && orderOwner.CurrentSettlement != null && orderOwner.Occupation != Occupation.Special && !orderOwner.IsAICompanion();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TroopRoster), "EnsureLength")]
        public static bool EnsureLengthProper(int length, ref TroopRosterElement[] ___data, int ____count)
        {
            if (length > 0 && (___data == null || length > ___data.Length))
            {
                TroopRosterElement[] array = new TroopRosterElement[length];
                for (int i = 0; i < ____count; i++)
                {
                    array[i] = ___data[i];
                }
                ___data = array;
            }
            return false;
        }

        [HarmonyFinalizer]
        [HarmonyPatch(typeof(MobilePartyAi), "GetBehaviors")]
        public static Exception GetBehaviorsFinalizer(Exception __exception, MobileParty ____mobileParty, ref AiBehavior bestAiBehavior, ref IInteractablePoint behaviorObject, ref CampaignVec2 bestTargetPoint)
        {
            if (__exception != null)
            {
                bestAiBehavior = AiBehavior.Hold;
                behaviorObject = null;
                bestTargetPoint = ____mobileParty.Position;
            }
            return null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(TavernEmployeesCampaignBehavior), "conversation_talk_bard_on_condition")]
        public static bool PreventCrash2(TavernEmployeesCampaignBehavior __instance, ref bool __result)
        {
            if (CharacterObject.OneToOneConversationCharacter.Occupation == Occupation.Musician)
            {
                List<string> list = new List<string>();
                List<Settlement> list2 = (from x in Settlement.All where x.IsTown && x != Settlement.CurrentSettlement select x).ToList();
                Settlement settlement = list2.TakeRandom(1).FirstOrDefault();
                if (settlement != null) MBTextManager.SetTextVariable("RANDOM_TOWN", settlement.Name, false);
                list.Add("{=3n1KRLpZ}'My love is far \n I know not where \n Perhaps the winds shall tell me'");
                list.Add("{=NQOQb0C9}'And many thousand bodies lay a-rotting in the sun \n But things like that must be you know for kingdoms to be won'");
                list.Add("{=bs8ayCGX}'A warrior brave you might surely be \n With your blade and your helm and your bold fiery steed \n But I'll give you a warning you'd be wise to heed \n Don't toy with the fishwives of {RANDOM_TOWN}'");
                list.Add("{=3n1KRLpZ}'My love is far \n I know not where \n Perhaps the winds shall tell me'");
                list.Add("{=YequZz6U}'Oh the maidens of {RANDOM_TOWN} are merry and fair \n Plotting their mischief with flowers in their hair \n Were I still a young man I sure would be there \n But now I'll take warmth over trouble'");
                list.Add("{=CM8Tr3lL}'Oh my pocket's been picked \n And my shirt's drenched with sick \n And my head feels like it's come a fit to bursting'");
                list.Add("{=2fbLBXtT}'O'er the whale-road she sped \n She were manned by the dead  \n And the clouds followed black in her wake'");
                string value = list.TakeRandom(1).FirstOrDefault();
                MBTextManager.SetTextVariable("LYRIC_SCRAP", new TextObject(value, null), false);
                __result = true;
            }
            else __result = false;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(HotKeyManager), "RegisterInitialContexts")]
        public static bool AddTorContext(ref IEnumerable<GameKeyContext> contexts)
        {
            List<GameKeyContext> newcontexts = contexts.ToList();
            if (!newcontexts.Any(x => x is TORGameKeyContext)) newcontexts.Add(new TORGameKeyContext());
            contexts = newcontexts;
            return true;
        }


        //For some reason, sometimes on a hideout becoming visible this fires off without the lists of settlement tuples being initialized
        //which should happen on nameplate creation of the new visible hideout.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SettlementNameplatesVM), "OnPartyBaseVisibilityChange")]
        public static bool PreventSettlementNameplateCrash(SettlementNameplatesVM __instance, PartyBase party, IEnumerable<Tuple<Settlement, GameEntity>> ____allHideouts, IEnumerable<Tuple<Settlement, GameEntity>> ____allRetreats, IEnumerable<Tuple<Settlement, GameEntity>> ____allRegularSettlements)
        {
            if (____allHideouts == null || ____allRetreats == null || ____allRegularSettlements == null)
            {
                return false;
            }

            return true;
        }
    }
}