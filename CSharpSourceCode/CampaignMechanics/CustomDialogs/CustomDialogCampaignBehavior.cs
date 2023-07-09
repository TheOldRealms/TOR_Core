using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TOR_Core.Extensions;

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
            obj.AddDialogLine("chaos_greeting", "start", "close_window", "{=ccultist_drowninblood}You will die drowning in a pool of your own blood!", () => EncounteredPartyMatch("chaos_clan_1") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("chaos_die", "start", "close_window", "I will return!", () => EncounteredPartyMatch("chaos_clan_1") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("beastmen_greeting", "start", "close_window", "{=beastmen_trample}We will trample your puny body beneath our hooves!", () => (EncounteredPartyMatch("steppe_bandits") || EncounteredPartyMatch("beastmen_clan_1")) && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("beastmen_die", "start", "close_window", "The dark gods have abandoned us!", () => EncounteredPartyMatch("steppe_bandits") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("brokenwheel_greeting", "start", "close_window", "{=ccultist_flames}You will be bathed in flames of Chaos and you will be happy!", () => EncounteredPartyMatch("chs_cult_1") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("brokenwheel_die", "start", "close_window", "The schemes of Tzeentch are endless, you have accomplished nothing!", () => EncounteredPartyMatch("chs_cult_1") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("illumination_greeting", "start", "close_window", "{=ccultist_thirdeye}Come open the third eye, let me show you brother!", () => EncounteredPartyMatch("chs_cult_2") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("illumination_die", "start", "close_window", "You may have won the battle, but my life has been more successful than yours will ever be!", () => EncounteredPartyMatch("chs_cult_2") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("secondflesh_greeting", "start", "close_window", "{=ccultist_spreadpox}I will spread my pox on your flesh!", () => EncounteredPartyMatch("chs_cult_3") && !HeroIsWounded(), null, 199);
            obj.AddDialogLine("secondflesh_die", "start", "close_window", "Today, death. Tomorrow, rebirth. The cycle cannot be stopped!", () => EncounteredPartyMatch("chs_cult_3") && HeroIsWounded(), null, 199);

            obj.AddDialogLine("undead_notalk", "start", "close_window", "...", () => CharacterObject.OneToOneConversationCharacter.IsUndead() && CharacterObject.OneToOneConversationCharacter.HeroObject == null, null, 199);
        }

        private bool EncounteredPartyMatch(string clanId)
        {
            var party = PlayerEncounter.EncounteredMobileParty;
            if (party != null && party.ActualClan != null)
            {
                return party.ActualClan.StringId == clanId && party.MemberRoster.Contains(CharacterObject.OneToOneConversationCharacter);
            }
            return false;
        }

        private bool HeroIsWounded()
        {
            var hero = CharacterObject.OneToOneConversationCharacter.HeroObject;
            if (hero == null)
                return false;
            return hero.IsWounded;
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
