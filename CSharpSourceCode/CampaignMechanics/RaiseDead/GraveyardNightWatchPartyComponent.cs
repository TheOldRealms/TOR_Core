﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;

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
                if(_cachedName == null)
                {
                    _cachedName = new TextObject(Settlement.Name.ToString() + "'s Nightwatch");
                }
                return _cachedName;
            }
        }

        public override Settlement HomeSettlement => Settlement;

        private GraveyardNightWatchPartyComponent(Settlement settlement)
        {
            Settlement = settlement;
            _cachedName = new TextObject(settlement.Name.ToString() + "'s Nightwatch");
        }


        public static MobileParty CreateParty(Settlement settlement)
        {
            return MobileParty.CreateParty(settlement + "_nightwatchparty_1", new GraveyardNightWatchPartyComponent(settlement), delegate (MobileParty mobileParty)
            {
                (mobileParty.PartyComponent as GraveyardNightWatchPartyComponent).InitializeQuestPartyProperties(mobileParty);
            });
        }

        private void InitializeQuestPartyProperties(MobileParty mobileParty)
        {
            mobileParty.ActualClan = Settlement.OwnerClan;
            PartyTemplateObject militiaPartyTemplate = Settlement.Culture.MilitiaPartyTemplate;
            mobileParty.InitializeMobilePartyAtPosition(militiaPartyTemplate, Settlement.GatePosition, 7);
            mobileParty.Party.Visuals.SetMapIconAsDirty();
            mobileParty.Ai.DisableAi();
            mobileParty.Aggressiveness = 0f;
        }
    }
}
