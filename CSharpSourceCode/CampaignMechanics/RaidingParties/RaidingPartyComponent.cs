using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.SaveSystem;
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

        [SaveableField(4)] private string _name;

        private RaidingPartyComponent(Settlement home, string name, Clan ownerClan)
        {
            _home = home;
            _name = name;
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

        public override TextObject Name => new(_name);

        public void HourlyTickAI(PartyThinkParams thinkParams)
        {
            if(!TargetIsValid())
            {
                FindNewTarget();
            }
            if(Target != null)
            {
                AIBehaviorTuple item = new(Target, AiBehavior.RaidSettlement, false);
                if (thinkParams.TryGetBehaviorScore(item, out float score))
                {
                    thinkParams.SetBehaviorScore(item, score + 1f);
                    return;
                }
                else
                {
                    ValueTuple<AIBehaviorTuple, float> valueTuple = new(item, 9999f);
                    thinkParams.AddBehaviorScore(valueTuple);
                }

                if ((bool)!Clan?.IsAtWarWith(Target?.MapFaction)) DeclareWarAction.ApplyByDefault(Clan, Target.MapFaction);
            }
            else
            {
                AIBehaviorTuple item = new(HomeSettlement, AiBehavior.PatrolAroundPoint, false);
                if (thinkParams.TryGetBehaviorScore(item, out float score))
                {
                    thinkParams.SetBehaviorScore(item, score + 1f);
                    return;
                }
                else
                {
                    ValueTuple<AIBehaviorTuple, float> valueTuple = new(item, 9999f);
                    thinkParams.AddBehaviorScore(valueTuple);
                }
            }
        }

        private bool TargetIsValid() => Target != null && !Target.IsRaided && Target != HomeSettlement && Target.IsVillage && 
            (!Target.IsUnderRaid || Target.LastAttackerParty == MobileParty);

        private void FindNewTarget()
        {
            Target = TORCommon.FindSettlementsAroundPosition(Party.Position2D, 100, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
        }
    }
}
