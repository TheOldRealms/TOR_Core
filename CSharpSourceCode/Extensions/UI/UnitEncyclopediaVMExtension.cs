using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(EncyclopediaUnitPageVM))]
    public class UnitEncyclopediaVMExtension : BaseViewModelExtension
    {
        private CharacterObject _characterObject;
        private BasicTooltipViewModel _extendedInfoHint;

        public UnitEncyclopediaVMExtension(ViewModel vm) : base(vm)
        {
            RefreshValues();
        }
        
        public override void RefreshValues()
        {
            var unitVM = _vm as EncyclopediaUnitPageVM;
            if (_characterObject == null) _characterObject = unitVM.Obj as CharacterObject;

            if (_characterObject == null) return;

            var extendedInfoList = _characterObject.GetExtendedInfoToolTipText();
            if (!extendedInfoList.IsEmpty()) ExtendedInfoHint = new BasicTooltipViewModel(() => extendedInfoList);
        }
        
        public CharacterObject GetCharacterObject()
        {
            return _characterObject;
        }
        
        [DataSourceProperty]
        public BasicTooltipViewModel ExtendedInfoHint
        {
            get => _extendedInfoHint;
            set
            {
                if (value == _extendedInfoHint)
                    return;
                _extendedInfoHint = value;
                _vm.OnPropertyChangedWithValue(nameof(ExtendedInfoHint));
            }
        }
    }
}