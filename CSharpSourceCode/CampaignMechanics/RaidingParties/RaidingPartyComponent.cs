using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.RaidingParties
{
    public class RaidingPartyComponent : WarPartyComponent, IRaidingParty
    {
        [SaveableProperty(1)] public Settlement Target { get; set; }

        [SaveableField(2)] private Hero _owner;
        public override Hero PartyOwner => _owner;

        [SaveableField(3)] private Settlement _home;
        public override Settlement HomeSettlement => _home;

        [CachedData] private TextObject _cachedName;

        private RaidingPartyComponent(Settlement home, string name, Clan ownerClan)
        {
            _home = home;
            _cachedName = new TextObject(name);
            _owner = ownerClan.Leader;
        }

        private void InitializeRaidingParty(MobileParty mobileParty, int partySize, PartyTemplateObject template, Clan ownerClan)
        {
            if (ownerClan != null && template != null)
            {
                mobileParty.Party.MobileParty.InitializeMobilePartyAroundPosition(template, HomeSettlement.Position2D, 1f, troopNumberLimit: partySize);
                mobileParty.ActualClan = ownerClan;
                mobileParty.Aggressiveness = 2.0f;
                mobileParty.Party.SetVisualAsDirty();
                mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Meat, MBRandom.RandomInt(partySize, partySize * 2)));
                mobileParty.SetPartyUsedByQuest(true);
            }
            else
            {
                throw new MBNullParameterException("PartyTemplateObject or owner Clan is null.");
            }
        }

        public static MobileParty CreateRaidingParty(string stringId, Settlement home, string name, PartyTemplateObject template, Clan owner, int partySize)
        {
            return MobileParty.CreateParty(stringId,
                new RaidingPartyComponent(home, name, owner),
                mobileParty => ((RaidingPartyComponent)mobileParty.PartyComponent).InitializeRaidingParty(mobileParty, partySize, template, owner));
        }

        public override TextObject Name
        {
            get
            {
                if (_cachedName == null)
                {
                    _cachedName = new TextObject(HomeSettlement.Culture.Name + " Raiders");
                }
                return _cachedName;
            }
        }

        public void HourlyTick()
        {
            if(Target == null || Target.IsRaided || Target.IsUnderRaid || Target == HomeSettlement)
            {
                FindNewTarget();
            }
            else if(Party.MobileParty.ShortTermBehavior != AiBehavior.RaidSettlement)
            {
                ResumeRaiding();
            }
        }

        private void FindNewTarget()
        {
            Target = TORCommon.FindSettlementsAroundPosition(Party.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            SetPartyAiAction.GetActionForRaidingSettlement(Party.MobileParty, Target);
        }

        private void ResumeRaiding()
        {
            SetPartyAiAction.GetActionForRaidingSettlement(Party.MobileParty, Target);
        }
    }
}
