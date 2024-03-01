﻿using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Conditions.Alliance;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Cost;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Scoring;
using TOR_Core.CampaignMechanics.Diplomacy.ViewModels;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Diplomacy.ViewModelExtensions
{
    [ViewModelExtension(typeof(KingdomTruceItemVM))]
    public class TORKingdomTruceItemVMExtension : BaseViewModelExtension
    {
        [DataSourceProperty]
        public bool IsAllianceVisible { get; set; }
        [DataSourceProperty]
        public bool IsAllianceAvailable { get; set; }
        [DataSourceProperty]
        public int AllianceInfluenceCost { get; set; }
        [DataSourceProperty]
        public int AllianceGoldCost { get; set; }
        [DataSourceProperty]
        public string AllianceActionName { get; set; }

        [DataSourceProperty]
        public HintViewModel AllianceHint {  get; set; }
        [DataSourceProperty]
        public BasicTooltipViewModel AllianceScoreHint { get; set; }

        [DataSourceProperty]
        public DiplomacyPropertiesVM DiplomacyProperties { get; set; }

        protected readonly bool _isAlliance;

        public TORKingdomTruceItemVMExtension(ViewModel vm) : base(vm)
        {
            AllianceActionName = GameTexts.FindText("str_diplomacy_form_alliance_label").ToString();

            var view = (KingdomTruceItemVM)vm;
            var _faction1 = (Kingdom)view.Faction1;
            var _faction2 = (Kingdom)view.Faction2;

            if (_faction1 != null && _faction2 != null)
            {
                _isAlliance = _faction1.GetStanceWith(_faction2).IsAllied;
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();

            var view = (KingdomTruceItemVM)_vm;
            var _faction1 = (Kingdom)view.Faction1;
            var _faction2 = (Kingdom)view.Faction2;

            if (DiplomacyProperties is null)
                DiplomacyProperties = new DiplomacyPropertiesVM(_faction1, _faction2);

            DiplomacyProperties.UpdateDiplomacyProperties();
            UpdateActionAvailability();

            // War Exhaustion here if we use it
        }

        private void UpdateActionAvailability()
        {
            if (_vm is KingdomDiplomacyItemVM)
            {
                var view = (KingdomDiplomacyItemVM)_vm;
                if (_isAlliance)
                {
                    var breakAllianceException = BreakAllianceConditions.Instance.CanApplyExceptions(view).FirstOrDefault();
                    IsAllianceVisible = false;
                    return;
                }

                var _faction1 = (Kingdom)view.Faction1;
                var _faction2 = (Kingdom)view.Faction2;

                var allianceException = FormAllianceConditions.Instance.CanApplyExceptions(view).FirstOrDefault();

                IsAllianceVisible = true;
                IsAllianceAvailable = allianceException is null;

                AllianceHint = !(allianceException is null) ? new HintViewModel(allianceException) : new HintViewModel();

                var allianceCost = DiplomacyCostCalculator.DetermineCostForFormingAlliance(_faction1, _faction2, true);
                AllianceInfluenceCost = (int)allianceCost.InfluenceCost.Value;
                AllianceGoldCost = (int)allianceCost.GoldCost.Value;

                var allianceScore = AllianceScoringModel.Instance.GetScore(_faction2, _faction1, true);
                AllianceScoreHint = UpdateDiplomacyTooltip(allianceScore);
            }
        }

        private BasicTooltipViewModel UpdateDiplomacyTooltip(ExplainedNumber explainedNumber)
        {
            var list = new List<TooltipProperty>
            {
                new TooltipProperty(GameTexts.FindText("str_diplomacy_currentScore_text").ToString(), 
                    $"{explainedNumber.ResultNumber:0.##}", 0, false, TooltipProperty.TooltipPropertyFlags.Title)
            };

            foreach (var (name, number) in explainedNumber.GetLines())
                list.Add(new TooltipProperty(name, number.GetPlusPrefixed(), 0));

            list.Add(new TooltipProperty(string.Empty, string.Empty, 0, false, TooltipProperty.TooltipPropertyFlags.RundownSeperator));
            list.Add(new TooltipProperty(GameTexts.FindText("str_diplomacy_requiredScore_text").ToString(), $"{AllianceScoringModel.Instance.ScoreThreshold:0.##}", 0, false,
                TooltipProperty.TooltipPropertyFlags.RundownResult));

            return new BasicTooltipViewModel(() => list);
        }
    }
}
