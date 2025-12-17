using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RaiseDead
{
    public class GraveyardNightWatchPartyComponent : PartyComponent
    {
        [SaveableProperty(1)]
        public Settlement Settlement { get; private set; }
        [CachedData] private TextObject _cachedName;
        public override Hero PartyOwner => Settlement.Owner != null ? Settlement.Owner : Settlement.MapFaction.Leader;
        public override TextObject Name
        {
            get
            {
                if (_cachedName == null)
                {
                    var nightwatch = TORTextHelper.GetTextObject("tor_graveyard_nightwatch_name", "{SETTLEMENTNAME}'s Nightwatch");
                    MBTextManager.SetTextVariable("SETTLEMENT_NAME", HomeSettlement.Name);
                    _cachedName = nightwatch;
                }
                return _cachedName;
            }
        }

        public override Settlement HomeSettlement => Settlement;

        private GraveyardNightWatchPartyComponent(Settlement settlement)
        {
            Settlement = settlement;
            _cachedName = TORTextHelper.GetTextObject("tor_graveyard_nightwatch_name", "{SETTLEMENTNAME}'s Nightwatch");
            MBTextManager.SetTextVariable("SETTLEMENT_NAME", HomeSettlement.Name);
        }


        public static MobileParty CreateParty(Settlement settlement)
        {
            return MobileParty.CreateParty(settlement + "_nightwatchparty_1", new GraveyardNightWatchPartyComponent(settlement));
        }

        protected override void OnMobilePartySetOnCreation()
        {
            InitializeQuestPartyProperties();
        }

        private void InitializeQuestPartyProperties()
        {
            MobileParty.ActualClan = Settlement.OwnerClan;
            PartyTemplateObject militiaPartyTemplate = Settlement.Culture.MilitiaPartyTemplate;
            MobileParty.InitializeMobilePartyAtPosition(militiaPartyTemplate, Settlement.GatePosition);
            MobileParty.Party.SetVisualAsDirty();
            MobileParty.Ai.DisableAi();
            MobileParty.Aggressiveness = 0f;
        }

        public override Banner GetDefaultComponentBanner()
        {
            return PartyOwner.ClanBanner;
        }
    }
}