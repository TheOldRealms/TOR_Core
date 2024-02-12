using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.RaidingParties;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Invasions
{
    public class InvasionPartyComponent : LordPartyComponent, IInvasionParty
    {
        [SaveableProperty(1)] public Settlement Target { get; private set; }

        private InvasionPartyComponent(Settlement home, string name, Clan ownerClan, Hero leader) : base(leader, leader)
        {

        }

        private void InitializeInvasionParty(MobileParty mobileParty, int partySize, PartyTemplateObject template, Clan ownerClan, Settlement settlement)
        {
            if (ownerClan != null && template != null)
            {
                mobileParty.AddElementToMemberRoster(mobileParty.LeaderHero.CharacterObject, 1, true);
                mobileParty.Party.MobileParty.InitializeMobilePartyAroundPosition(template, settlement.Position2D, 1f, troopNumberLimit: partySize);
                mobileParty.ActualClan = ownerClan;
                mobileParty.Aggressiveness = 2.0f;
                mobileParty.Party.SetVisualAsDirty();
                mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Meat, (int)(-mobileParty.FoodChange * 20)));
                mobileParty.SetPartyUsedByQuest(true);
                mobileParty.SetWagePaymentLimit(999999);
                mobileParty.LeaderHero.ChangeHeroGold(1000000);
                Target = Settlement.Find("town_AV1");
                if(TargetIsValid())
                {
                    SetPartyAiAction.GetActionForBesiegingSettlement(MobileParty, Target);
                    mobileParty.Ai.SetDoNotMakeNewDecisions(true);
                }
            }
            else
            {
                throw new MBNullParameterException("PartyTemplateObject or owner Clan is null.");
            }
        }

        public static MobileParty CreateInvasionForce(string stringId, Settlement settlement, string name, PartyTemplateObject template, Clan owner, Hero leader, int partySize)
        {
            return MobileParty.CreateParty(stringId,
                new InvasionPartyComponent(settlement, name, owner, leader),
                mobileParty => ((InvasionPartyComponent)mobileParty.PartyComponent).InitializeInvasionParty(mobileParty, partySize, template, owner, settlement));
        }

        public void HourlyTickAI(PartyThinkParams thinkParams)
        {
            if(Party.MobileParty.BesiegedSettlement != null && Party.MobileParty.BesiegedSettlement == Target)
            {
                if (Party.MobileParty.BesiegerCamp.IsPreparationComplete)
                {
                    /*
                    AIBehaviorTuple item = new AIBehaviorTuple(Target, AiBehavior.AssaultSettlement, false);
                    float score;
                    if (thinkParams.TryGetBehaviorScore(item, out score))
                    {
                        thinkParams.SetBehaviorScore(item, score + 0.8f);
                        return;
                    }
                    ValueTuple<AIBehaviorTuple, float> valueTuple = new ValueTuple<AIBehaviorTuple, float>(item, 1f);
                    thinkParams.AddBehaviorScore(valueTuple);
                    thinkParams.DoNotChangeBehavior = true;
                    Party.MobileParty.Ai.RecalculateShortTermAi();
                    */
                }
            }
            else
            {
                if (!TargetIsValid())
                {
                    FindNewTarget();
                }
                if (TargetIsValid())
                {
                    AIBehaviorTuple item = new AIBehaviorTuple(Target, AiBehavior.BesiegeSettlement, false);
                    float score;
                    if (thinkParams.TryGetBehaviorScore(item, out score))
                    {
                        thinkParams.SetBehaviorScore(item, score + 0.8f);
                        return;
                    }
                    ValueTuple<AIBehaviorTuple, float> valueTuple = new ValueTuple<AIBehaviorTuple, float>(item, 0.8f);
                    thinkParams.AddBehaviorScore(valueTuple);
                }
            }
        }

        public bool TargetIsValid() => Target != null && !Target.IsUnderSiege && Target.IsTown && Target.MapFaction.GetStanceWith(Clan).IsAtWar;

        public void FindNewTarget()
        {
            Target = TORCommon.FindSettlementsAroundPosition(Party.Position2D, 60, x => !x.IsUnderSiege && x.IsTown && x.MapFaction.GetStanceWith(Clan).IsAtWar).GetRandomElementInefficiently();
        }
    }

    public interface IInvasionParty
    {
        void HourlyTickAI(PartyThinkParams thinkParams);
    }
}
