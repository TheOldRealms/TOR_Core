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
        
        //Randy: This is by the MobileParty component to check, it cannot be null otherwise the raiding party cannot find its owner (taleworlds thing: IsSettlementSuitableForVisitingCondition)
        public override Hero PartyOwner
        {
            get { return Clan.Leader; }
        }

        [SaveableField(3)] private Settlement _home;
        public override Settlement HomeSettlement => _home;

        [SaveableField(4)] private string _name;
        [SaveableField(5)] private PartyTemplateObject _template;
        [SaveableField(6)] public int _partySize; //accessible to TORPartySizeModel which needs it to scale the troop counts up because native now only supports ratios up to 1.0 compared to the party template used

        private RaidingPartyComponent(Settlement home, string name, PartyTemplateObject template, int partySize)
        {
            _home = home;
            _name = name;
            _template = template;
            _partySize = partySize;
        }

        protected override void OnMobilePartySetOnCreation()
        {
            InitializeRaidingParty();
        }

        private void InitializeRaidingParty()
        {
            if (_home.OwnerClan != null && _template != null)
            {
                //party size adjustment handled in TORPartySizeModel.FindAppropriateInitialRosterForMobileParty
                MobileParty.InitializeMobilePartyAroundPosition(_template, HomeSettlement.Position, 20);
                MobileParty.ActualClan = _home.OwnerClan;
                MobileParty.Aggressiveness = 2.0f;
                MobileParty.Party.SetVisualAsDirty();
                MobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Meat, MBRandom.RandomInt(_partySize, _partySize * 2)));
            }
            else
            {
                throw new MBNullParameterException("PartyTemplateObject or owner Clan is null.");
            }
        }

        public static MobileParty CreateRaidingParty(string stringId, Settlement home, string name, PartyTemplateObject template, int partySize)
        {
            return MobileParty.CreateParty(stringId, new RaidingPartyComponent(home, name, template, partySize));
        }

        public override TextObject Name => new(_name);

        public void HourlyTickAI(PartyThinkParams thinkParams)
        {
            if (!TargetIsValid())
            {
                FindNewTarget();
            }
            if (Target != null)
            {
                AIBehaviorData item = new(Target, AiBehavior.RaidSettlement, MobileParty.NavigationType.Default, false, false, false);
                if (thinkParams.TryGetBehaviorScore(item, out float score))
                {
                    thinkParams.SetBehaviorScore(item, score + 1f);
                    return;
                }
                else
                {
                    ValueTuple<AIBehaviorData, float> valueTuple = new(item, 9999f);
                    thinkParams.AddBehaviorScore(valueTuple);
                }

                if ((bool)!Clan?.IsAtWarWith(Target?.MapFaction)) DeclareWarAction.ApplyByDefault(Clan, Target.MapFaction);
            }
            else
            {
                AIBehaviorData item = new(Target, AiBehavior.PatrolAroundPoint, MobileParty.NavigationType.Default, false, false, false);
                if (thinkParams.TryGetBehaviorScore(item, out float score))
                {
                    thinkParams.SetBehaviorScore(item, score + 1f);
                    return;
                }
                else
                {
                    ValueTuple<AIBehaviorData, float> valueTuple = new(item, 9999f);
                    thinkParams.AddBehaviorScore(valueTuple);
                }
            }
        }

        private bool TargetIsValid() => Target != null && !Target.IsRaided && Target != HomeSettlement && Target.IsVillage &&
            (!Target.IsUnderRaid || Target.LastAttackerParty == MobileParty);

        private void FindNewTarget()
        {
            Target = TORCommon.FindSettlementsAroundPosition(Party.Position.ToVec2(), 100, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            Target ??= TORCommon.FindSettlementsAroundPosition(Party.Position.ToVec2(), 150, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
        }
    }
}