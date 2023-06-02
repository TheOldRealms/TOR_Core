﻿using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;

namespace TOR_Core.CampaignMechanics.Diplomacy
{
    public class TORDeclareWarDecision : DeclareWarDecision
    {

        public TORDeclareWarDecision(Clan proposerClan, IFaction factionToDeclareWarOn) : base(proposerClan, factionToDeclareWarOn) { }

        public override float DetermineSupport(Clan clan, DecisionOutcome possibleOutcome)
        {
            Hero leader = clan.Leader;
            DeclareWarDecisionOutcome warDecisionOutcome = (DeclareWarDecisionOutcome)possibleOutcome;
            float denarsToInfluence = Campaign.Current.Models.DiplomacyModel.DenarsToInfluence();
            int valueForFaction = new DeclareWarBarterable(Kingdom, FactionToDeclareWarOn).GetValueForFaction(clan);
            float num = valueForFaction * denarsToInfluence;
            return warDecisionOutcome.ShouldWarBeDeclared ? num + (leader.GetTraitLevel(DefaultTraits.Valor) * 20 - leader.GetTraitLevel(DefaultTraits.Mercy) * 10) : -num + (-leader.GetTraitLevel(DefaultTraits.Valor) * 20 + leader.GetTraitLevel(DefaultTraits.Mercy) * 10);
        }

        public new float CalculateSupport(Clan clan)
        {
            return DetermineSupport(clan, new DeclareWarDecisionOutcome(true, Kingdom, FactionToDeclareWarOn));
        }
    }
}
