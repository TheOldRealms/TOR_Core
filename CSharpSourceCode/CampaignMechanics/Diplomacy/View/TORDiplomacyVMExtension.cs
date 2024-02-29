using JetBrains.Annotations;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Diplomacy;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Extensions;
using TOR_Core.Extensions.UI;

namespace TOR_Core.CampaignMechanics.Diplomacy.View
{
    [ViewModelExtension(typeof(KingdomDiplomacyVM), "RefreshKingdomDiplomacyView")]
    public class TORDiplomacyVMExtension : BaseViewModelExtension
    {
        private static readonly TextObject _TAlliances = new TextObject("Alliances");
        [DataSourceProperty]
        public string PlayerAlliancesText { get; }

        protected MBBindingList<KingdomTruceItemVM> _playerAlliances;

        [DataSourceProperty]
        public MBBindingList<KingdomTruceItemVM> PlayerAlliances
        {
            get => _playerAlliances;
            set
            {
                _playerAlliances = value;
                _vm.OnPropertyChanged(nameof(PlayerAlliances));
            }
        }

        private string _numOfPlayerAlliancesText = null;
        [DataSourceProperty]
        public string NumOfPlayerAlliancesText
        {
            get => _numOfPlayerAlliancesText;
            set
            {
                _numOfPlayerAlliancesText = value;
                _vm.OnPropertyChanged(nameof(NumOfPlayerAlliancesText));
            }
        }
        public TORDiplomacyVMExtension(ViewModel vm) : base(vm)
        {
            _playerAlliances = new MBBindingList<KingdomTruceItemVM>();

            PlayerAlliancesText = _TAlliances.ToString();

            TORDiplomacyEvents.AllianceFormed.AddNonSerializedListener(this, _ => _vm.RefreshValues());
            TORDiplomacyEvents.AllianceBroken.AddNonSerializedListener(this, _ => _vm.RefreshValues());

            vm.RefreshValues();
        }

        public override void OnFinalize()
        {
            TORDiplomacyEvents.RemoveListeners(this);
            CampaignEventDispatcher.Instance.RemoveListeners(this);
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            if (_vm is KingdomDiplomacyVM)
            {
                var view = _vm as KingdomDiplomacyVM;
                var alliances = view.PlayerTruces.Where(item => item.Faction1.IsAlliedWith(item.Faction2)).ToList();
                foreach (var alliance in alliances) view.PlayerTruces.Remove(alliance);

                RefreshAlliances(alliances);

                GameTexts.SetVariable("STR", view.PlayerTruces.Count);
                view.NumOfPlayerTrucesText = GameTexts.FindText("str_STR_in_parentheses").ToString();
                GameTexts.SetVariable("STR", view.PlayerWars.Count);
                view.NumOfPlayerWarsText = GameTexts.FindText("str_STR_in_parentheses").ToString();
            }
        }

        private void RefreshAlliances(List<KingdomTruceItemVM> alliances)
        {
            PlayerAlliances.Clear();

            foreach (var alliance in alliances) PlayerAlliances.Add(alliance);

            GameTexts.SetVariable("STR", PlayerAlliances.Count);
            NumOfPlayerAlliancesText = GameTexts.FindText("str_STR_in_parentheses").ToString();
        }
    }
}
