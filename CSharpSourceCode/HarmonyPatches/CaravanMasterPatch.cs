using HarmonyLib;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace TOR_Core.HarmonyPatches
{
    [HarmonyPatch]
    public static class CaravanMasterPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(CaravanPartyComponent), "InitializeCaravanOnCreation")]
        public static bool CaravanLeaderOverride(MobileParty mobileParty, Hero caravanLeader, ItemRoster caravanItems, int troopToBeGiven, bool isElite, CaravanPartyComponent __instance)
        {
            __instance.MobileParty.Aggressiveness = 0f;

            if (troopToBeGiven == 0)
            {
                float num;
                if (MBRandom.RandomFloat < 0.67f)
                {
                    num = (1f - MBRandom.RandomFloat * MBRandom.RandomFloat) * 0.5f + 0.5f;
                }
                else
                {
                    num = 1f;
                }
                int num2 = (int)(mobileParty.Party.PartySizeLimit * num);
                if (num2 >= 10)
                {
                    num2--;
                }
                troopToBeGiven = num2;
            }
            PartyTemplateObject pt = isElite ? __instance.Settlement.Culture.EliteCaravanPartyTemplate : __instance.Settlement.Culture.CaravanPartyTemplate;
            mobileParty.InitializeMobilePartyAtPosition(pt, __instance.Settlement.GatePosition, troopToBeGiven);
            if (caravanLeader != null)
            {
                mobileParty.MemberRoster.AddToCounts(caravanLeader.CharacterObject, 1, true, 0, 0, true, -1);
            }
            else
            {
                CharacterObject character = GetCaravanMaster(__instance.Settlement.Culture);
                mobileParty.MemberRoster.AddToCounts(character, 1, true, 0, 0, true, -1);
            }
            mobileParty.ActualClan = __instance.Owner.Clan;
            mobileParty.Party.SetVisualAsDirty();
            mobileParty.InitializePartyTrade(10000 + ((__instance.Owner.Clan == Clan.PlayerClan) ? 5000 : 0));
            if (caravanItems != null)
            {
                mobileParty.ItemRoster.Add(caravanItems);
                return false;
            }
            float num3 = 10000f;
            ItemObject itemObject = null;
            foreach (ItemObject itemObject2 in TaleWorlds.CampaignSystem.Extensions.Items.All)
            {
                if (itemObject2.ItemCategory == DefaultItemCategories.PackAnimal && !itemObject2.NotMerchandise && itemObject2.Value < num3)
                {
                    itemObject = itemObject2;
                    num3 = itemObject2.Value;
                }
            }
            if (itemObject != null)
            {
                mobileParty.ItemRoster.Add(new ItemRosterElement(itemObject, (int)(mobileParty.MemberRoster.TotalManCount * 0.5f), null));
            }
            return false;
        }

        private static CharacterObject GetCaravanMaster(CultureObject culture)
        {
            CharacterObject master = null;
            if(culture == null)
            {
                master = CharacterObject.All.FirstOrDefault(x => x.Occupation == Occupation.CaravanGuard);
            }
            else
            {
                master = culture.CaravanMaster;
                master ??= CharacterObject.All.FirstOrDefault(x => x.Occupation == Occupation.CaravanGuard && x.IsInfantry && x.Level == 26 && x.Culture == culture);
                master ??= CharacterObject.All.FirstOrDefault(x => x.Occupation == Occupation.CaravanGuard && x.Culture == culture);
                master ??= CharacterObject.All.FirstOrDefault(x => x.Occupation == Occupation.CaravanGuard);
            }
            return master;
        }
    }
}
