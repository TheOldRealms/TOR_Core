using System.Collections.Generic;
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
        protected bool _isAllianceVisible { get; set; }
        [DataSourceProperty]
        public bool IsAllianceVisible
        {
            get => _isAllianceVisible;
            set
            {
                _isAllianceVisible = value;
                _vm.OnPropertyChanged(nameof(IsAllianceVisible));
            }
        }
        protected bool _isAllianceAvailable { get; set; }
        [DataSourceProperty]
        public bool IsAllianceAvailable
        {
            get => _isAllianceAvailable;
            set
            {
                _isAllianceAvailable = value;
                _vm.OnPropertyChanged(nameof(IsAllianceAvailable));
            }
        }
        protected int _allianceInfluenceCost { get; set; }
        [DataSourceProperty]
        public int AllianceInfluenceCost
        {
            get => _allianceInfluenceCost;
            set
            {
                _allianceInfluenceCost = value;
                _vm.OnPropertyChanged(nameof(AllianceInfluenceCost));
            }
        }
        protected int _allianceGoldCost { get; set; }
        [DataSourceProperty]
        public int AllianceGoldCost
        {
            get => _allianceGoldCost;
            set
            {
                _allianceGoldCost = value;
                _vm.OnPropertyChanged(nameof(AllianceGoldCost));
            }
        }
        protected string _allianceActionName { get; set; }
        [DataSourceProperty]
        public string AllianceActionName
        {
            get => _allianceActionName;
            set
            {
                _allianceActionName = value;
                _vm.OnPropertyChanged(nameof(AllianceActionName));
            }
        }

        protected HintViewModel _allianceHint;
        [DataSourceProperty]
        public HintViewModel AllianceHint
        {
            get => _allianceHint;
            set
            {
                _allianceHint = value;
                _vm.OnPropertyChanged(nameof(AllianceHint));
            }
        }
        protected BasicTooltipViewModel _allianceScoreHint;
        [DataSourceProperty]
        public BasicTooltipViewModel AllianceScoreHint
        {
            get => _allianceScoreHint;
            set
            {
                _allianceScoreHint = value;
                _vm.OnPropertyChanged(nameof(AllianceScoreHint));
            }
        }

        [DataSourceProperty]
        public DiplomacyPropertiesVM DiplomacyProperties { get; set; }

        public TORKingdomTruceItemVMExtension(ViewModel vm) : base(vm)
        {
            AllianceActionName = GameTexts.FindText("str_diplomacy_formAlliance_label").ToString();
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
                var _faction1 = (Kingdom)view.Faction1;
                var _faction2 = (Kingdom)view.Faction2;

                if (_faction1 != _faction2 && _faction1.GetStanceWith(_faction2).IsAllied)
                {
                    BreakAllianceConditions.Instance.CanApplyExceptions(view).FirstOrDefault();
                    IsAllianceVisible = false;
                    return;
                }

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
