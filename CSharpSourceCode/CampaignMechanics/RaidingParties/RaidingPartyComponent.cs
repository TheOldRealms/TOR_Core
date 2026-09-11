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
    /// <remarks>
    /// Historical notes from prior work :
    /// - is_minor_faction : xml attribute must be false or absent to avoid the clan needing heroes and joining kingdoms as mercenaries.
    /// - is_outlaw : unnecessary xml attribute, native does essentially nothing with it.
    /// </remarks>
    public class RaidingPartyComponent : WarPartyComponent, IRaidingParty
    {
        [SaveableProperty(1)] public Settlement Target { get; set; }
        
        //Sly : PartyOwner can be null as long as clan.IsMinorFaction is false which exempts them from AiVisitSettlementBehavior.AiHourlyTick checks which hit NREs due to the assumption that minor factions are hero clans => checks for the owning hero when determining if a village is an enemy and an unviable target for recruitment, etc.
        public override Hero PartyOwner
        {
            get
            {
                Clan actualClan = MobileParty.ActualClan;
                if (actualClan == null) return null;

                return actualClan.Leader;
            }
        }

        [SaveableField(2)] private Settlement _home;
        public override Settlement HomeSettlement => _home;

        [SaveableField(3)] private string _name;
        [SaveableField(4)] public int _partySize; //accessible to TORPartySizeModel which needs it to scale the troop counts up because native now only supports ratios up to 1.0 compared to the party template used

        private RaidingPartyComponent(Settlement home, string name, int partySize)
        {
            _home = home;
            _name = name;
            _partySize = partySize;
        }

        protected override void OnMobilePartySetOnCreation()
        {
            InitializeRaidingParty();
        }

        private void InitializeRaidingParty()
        {
            if (_home.OwnerClan?.DefaultPartyTemplate != null)
            {
                //party size adjustment handled in TORPartySizeModel.FindAppropriateInitialRosterForMobileParty
                MobileParty.InitializeMobilePartyAroundPosition(_home.OwnerClan.DefaultPartyTemplate, HomeSettlement.Position, 20);
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

        public static MobileParty CreateRaidingParty(string stringId, Settlement home, string name, int partySize)
        {
            return MobileParty.CreateParty(stringId, new RaidingPartyComponent(home, name, partySize));
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