using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    /// <summary>
    /// Temporary party component for troll cave defenders.
    /// Used to establish a proper MapEvent for battle mechanics.
    /// The party is destroyed after the battle ends.
    /// </summary>
    public class TrollCaveDefenderPartyComponent : PartyComponent
    {
        private const string TrollTroopId = "tor_gs_trolls";
        private const string TrollClanId = "troll_clan_1";

        [SaveableField(1)] private Settlement _home;
        [SaveableField(2)] private string _name;
        [SaveableField(3)] private int _trollCount;

        private static Clan TrollClan => Clan.FindFirst(x => x.StringId == TrollClanId);

        public override Hero PartyOwner => TrollClan?.Leader;
        public override TextObject Name => new TextObject(_name);
        public override Settlement HomeSettlement => _home;

        private TrollCaveDefenderPartyComponent(Settlement home, string name, int trollCount)
        {
            _home = home;
            _name = name;
            _trollCount = trollCount;
        }

        public override Banner GetDefaultComponentBanner()
        {
            return TrollClan?.Banner;
        }

        protected override void OnMobilePartySetOnCreation()
        {
            InitializeTrollParty();
        }

        private void InitializeTrollParty()
        {
            // Set troll clan
            var trollClan = TrollClan;
            MobileParty.ActualClan = trollClan;

            // Add trolls directly to roster
            var trollCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(TrollTroopId);
            if (trollCharacter != null && _trollCount > 0)
            {
                MobileParty.MemberRoster.AddToCounts(trollCharacter, _trollCount);
            }

            // Set up party properties
            MobileParty.Aggressiveness = 0f;
            MobileParty.Party.SetVisualAsDirty();
        }

        public static MobileParty CreateTrollDefenderParty(Settlement trollCave, int trollCount)
        {
            var component = new TrollCaveDefenderPartyComponent(trollCave, "Troll Defenders", trollCount);
            return MobileParty.CreateParty(trollCave.StringId + "_troll_defenders", component);
        }

        /// <summary>
        /// Destroys the temporary defender party after battle
        /// </summary>
        public static void DestroyDefenderParty(MobileParty party)
        {
            if (party != null && party.IsActive)
            {
                DestroyPartyAction.Apply(null, party);
            }
        }
    }
}
