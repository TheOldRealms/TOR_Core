using Helpers;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TaleWorlds.CampaignSystem.GameMenus.GameMenu;

namespace TOR_Core.CampaignMechanics.CustomEncounterDialogs
{
    public class CustomDialogCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Start);
        }

        private void Start(CampaignGameStarter obj)
        {
            obj.AddDialogLine("chaos_greeting", "start", "close_window", TORTextHelper.GetText("tor_chaos_greeting", "You will die drowning in a pool of your own blood!"), () => EncounteredPartyMatch("chaos_clan_1") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("chaos_die", "start", "close_window", TORTextHelper.GetText("tor_chaos_wounded_dialog_text", "I will return!"), () => EncounteredPartyMatch("chaos_clan_1") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("beastmen_greeting", "start", "close_window", TORTextHelper.GetText("tor_beastmen_greeting", "We will trample your puny body beneath our hooves!"), () => (EncounteredPartyMatch("steppe_bandits") || EncounteredPartyMatch("beastmen_clan_1")) && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("beastmen_die", "start", "close_window", TORTextHelper.GetText("tor_beastmen_wounded_dialog_text", "The dark gods have abandoned us!"), () => EncounteredPartyMatch("steppe_bandits") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("brokenwheel_greeting", "start", "close_window", TORTextHelper.GetText("tor_brokenwheel_greeting", "You will be bathed in flames of Chaos and you will be happy!"), () => EncounteredPartyMatch("chs_cult_1") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("brokenwheel_die", "start", "close_window", TORTextHelper.GetText("tor_brokenwheel_wounded_dialog_text", "The schemes of Tzeentch are endless, you have accomplished nothing!"), () => EncounteredPartyMatch("chs_cult_1") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("illumination_greeting", "start", "close_window", TORTextHelper.GetText("tor_illumination_greeting", "Come open the third eye, let me show you brother!"), () => EncounteredPartyMatch("chs_cult_2") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("illumination_die", "start", "close_window", TORTextHelper.GetText("tor_illumination_wounded_dialog_text", "You may have won the battle, but my life has been more successful than yours will ever be!"), () => EncounteredPartyMatch("chs_cult_2") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("secondflesh_greeting", "start", "close_window", TORTextHelper.GetText("tor_secondflesh_greeting", "I will spread my pox on your flesh!"), () => EncounteredPartyMatch("chs_cult_3") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("secondflesh_die", "start", "close_window", TORTextHelper.GetText("tor_secondflesh_wounded_dialog_text", "Today, death. Tomorrow, rebirth. The cycle cannot be stopped!"), () => EncounteredPartyMatch("chs_cult_3") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("undead_notalk", "start", "close_window", TORTextHelper.GetText("tor_undead_no_talk_text", "..."), () => CharacterObject.OneToOneConversationCharacter.IsUndead() && CharacterObject.OneToOneConversationCharacter.HeroObject == null, null, 199);

            obj.AddDialogLine("troll_greeting", "start", "close_window", TORTextHelper.GetText("tor_troll_greeting", "*GRAAAAWWWRR* *sniff* *sniff* ...MEAT!"), () => EncounteredPartyMatch("troll_clan_1"), null, 199);

            obj.AddDialogLine("druchii_greeting_war", "start", "close_window", TORTextHelper.GetText("tor_druchii_greeting_neutral", "What do you want, stranger? Speak quickly before you find yourself chained up in the bowels of my ship."), () => EncounteredPartyMatch("druchii_clan_1") && !HeroIsWounded()
            && (bool)PlayerEncounter.EncounteredMobileParty?.MapFaction?.IsAtWarWith(Hero.MainHero.MapFaction), null, 199);

            obj.AddDialogLine("druchii_greeting_nowar", "start", "close_window", TORTextHelper.GetText("tor_druchii_greeting_war", "It's you... The Masters of Karond Kar will be pleased indeed. Time to embrace your chains, slave!"), () => EncounteredPartyMatch("druchii_clan_1") && !HeroIsWounded()
            && !(bool)PlayerEncounter.EncounteredMobileParty?.MapFaction?.IsAtWarWith(Hero.MainHero.MapFaction), null, 199);

            obj.AddDialogLine("druchii_die", "start", "close_window", TORTextHelper.GetText("tor_druchii_die", "Our defeat is a mockery to Khaine."), () => EncounteredPartyMatch("druchii_clan_1") && HeroIsWounded(), null, 199);

            obj.AddGameMenuOption("encounter", "druchii_sell_slaves", TORTextHelper.GetText("tor_druchii_sell_slaves", "Sell your prisoners as slaves ({RANSOM_AMOUNT}{GOLD_ICON})"), (args) =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Ransom;
                int ransomValueOfAllTransferablePrisoners = GetRansomValueOfAllTransferablePrisoners();
                if (ransomValueOfAllTransferablePrisoners > 0)
                {
                    MBTextManager.SetTextVariable("RANSOM_AMOUNT", ransomValueOfAllTransferablePrisoners);
                }
                else
                {
                    args.IsEnabled = false;
                    args.Tooltip = TORTextHelper.GetTextObject("tor_no_prisoners_to_sell_tooltip_text", "You have no prisoners to sell.");
                }
                if (PlayerEncounter.EncounteredParty?.MapFaction?.IsAtWarWith(Hero.MainHero.MapFaction) == true)
                {
                    args.IsEnabled = false;
                    args.Tooltip = TORTextHelper.GetTextObject("tor_at_war_tooltip_text", "You are at war with this faction.");
                }
                if (EncounteredPartyMatch("druchii_clan_1", false)) return true;
                else return false;
            }, (args) =>
            {
                SellPrisonersAction.ApplyForSelectedPrisoners(PartyBase.MainParty, PlayerEncounter.EncounteredParty, MobilePartyHelper.GetPlayerPrisonersPlayerCanSell());
                SwitchToMenu("encounter");
            }, true, 0);
        }

        private static int GetRansomValueOfAllTransferablePrisoners()
        {
            int value = 0;
            List<string> list = Campaign.Current.GetCampaignBehavior<IViewDataTracker>().GetPartyPrisonerLocks().ToList();
            foreach (TroopRosterElement troopRosterElement in PartyBase.MainParty.PrisonRoster.GetTroopRoster())
            {
                if (!list.Contains(troopRosterElement.Character.StringId))
                {
                    value += Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(troopRosterElement.Character, Hero.MainHero) * troopRosterElement.Number;
                }
            }
            return value;
        }

        private bool EncounteredPartyMatch(string clanId, bool checkForConversationCharacter = true)
        {
            var party = PlayerEncounter.EncounteredMobileParty;
            if (party != null && party.ActualClan != null)
            {
                if (party.ActualClan.StringId == clanId)
                {
                    if (checkForConversationCharacter) return party.MemberRoster.Contains(CharacterObject.OneToOneConversationCharacter);
                    else return true;
                }
            }
            return false;
        }

        private bool HeroIsWounded()
        {
            var hero = CharacterObject.OneToOneConversationCharacter.HeroObject;
            return hero == null ? false : hero.IsWounded;
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}