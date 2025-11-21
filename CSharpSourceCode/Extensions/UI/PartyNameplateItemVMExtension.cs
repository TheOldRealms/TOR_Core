using SandBox.ViewModelCollection.Nameplate;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyNameplateVM))]
    public class PartyNameplateItemVMExtension : BaseViewModelExtension
    {
        private bool _isEnlisted = false;

        public PartyNameplateItemVMExtension(ViewModel vm) : base(vm)
        {

            if (Hero.MainHero.IsEnlisted())
            {
                var pvm = vm as PartyNameplateVM;
                //pvm.IsPrisoner = true; //TODO I am not sure what needs to be set here. the IsPrisoner, was required i think for disabling the nameplate?
            }
        }

        public override void RefreshValues()
        {
            var vm = _vm as PartyNameplateVM;

        }
    }
}