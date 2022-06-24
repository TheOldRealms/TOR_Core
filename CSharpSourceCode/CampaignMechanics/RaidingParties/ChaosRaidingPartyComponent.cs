﻿using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.CampaignMechanics.TORCustomSettlement;

namespace TOR_Core.CampaignMechanics.ChaosRaiding
{
    public class ChaosRaidingPartyComponent : WarPartyComponent, IRaidingParty
    {
        [SaveableProperty(1)] public Settlement Portal { get; private set; }

        [SaveableProperty(3)] public Settlement Target { get; set; }

        [SaveableField(4)] private Hero _owner;
        public override Hero PartyOwner => _owner;

        [SaveableField(5)] private Settlement _home;
        public override Settlement HomeSettlement => _home;

        [CachedData] private TextObject _cachedName;

        private ChaosRaidingPartyComponent(Settlement portal, TORCustomSettlementComponent questBattleSettlementComponent)
        {
            Portal = portal;
        }

        private void InitializeChaosRaidingParty(MobileParty mobileParty, int partySize)
        {
            Clan chaosClan = Clan.All.ToList().Find(x => x.StringId == "chaos_clan_1");
            if (chaosClan != null && chaosClan.Culture != null && chaosClan.Culture.StringId == "chaos_culture")
            {
                PartyTemplateObject chaosPartyTemplate = chaosClan.Culture.DefaultPartyTemplate;
                mobileParty.Party.MobileParty.InitializeMobilePartyAroundPosition(chaosPartyTemplate, Portal.Position2D, 1f, troopNumberLimit: partySize);
                mobileParty.ActualClan = chaosClan;
                _owner = mobileParty.ActualClan.Leader;
                _home = Portal;
                mobileParty.Aggressiveness = 2.0f;
                mobileParty.Party.Visuals.SetMapIconAsDirty();
                mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Meat, MBRandom.RandomInt(partySize, partySize * 2)));
                //mobileParty.SetPartyUsedByQuest(true);
            }
            else
            {
                throw new MBNotFoundException("Chaos Clan object not found. Can not spawn chaos parties.");
            }
        }

        public static MobileParty CreateChaosRaidingParty(string stringId, Settlement portal, TORCustomSettlementComponent component, int partySize)
        {
            return MobileParty.CreateParty(stringId,
                new ChaosRaidingPartyComponent(portal, component),
                mobileParty => ((ChaosRaidingPartyComponent)mobileParty.PartyComponent).InitializeChaosRaidingParty(mobileParty, partySize));
        }

        public override TextObject Name
        {
            get
            {
                if (_cachedName == null)
                {
                    _cachedName = new TextObject("Chaos Raiders");
                }
                return _cachedName;
            }
        }

        protected override void OnInitialize()
        {
            ((TORCustomSettlementComponent)Portal.SettlementComponent).RaidingParties.Add(MobileParty);
        }

        protected override void OnFinalize()
        {
            ((TORCustomSettlementComponent)Portal.SettlementComponent).RaidingParties.Remove(MobileParty);
        }

        public void SetBehavior(MobileParty party, PartyThinkParams partyThinkParams)
        {
            if (Target == null || Target.IsRaided || Target == Portal || Target.IsUnderRaid)
            {
                var find = Settlement.FindSettlementsAroundPosition(party.Position2D, 100, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage);
                if (find.Count() != 0)
                {
                    Target = find.GetRandomElementInefficiently();
                }
                else
                {
                    Target = Portal;
                }
            }

            if (Target.IsVillage && !Target.IsRaided && !Target.IsUnderRaid && Target != Portal)
            {
                partyThinkParams.AIBehaviorScores[new AIBehaviorTuple(Target, AiBehavior.RaidSettlement)] = 10f;
            }

            if (Target == Portal)
            {
                partyThinkParams.AIBehaviorScores[new AIBehaviorTuple(Portal, AiBehavior.GoToSettlement)] = 8f;
            }
        }
    }

    public class ChaosRaidingPartySaveDefiner : SaveableTypeDefiner
    {
        public ChaosRaidingPartySaveDefiner() : base(2_543_135)
        {
        }

        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
            AddClassDefinition(typeof(ChaosRaidingPartyComponent), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
            ConstructContainerDefinition(typeof(List<ChaosRaidingPartyComponent>));
        }
    }
}
