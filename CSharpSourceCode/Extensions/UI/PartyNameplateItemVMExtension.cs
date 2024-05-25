using System;
using SandBox.ViewModelCollection.Nameplate;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.ServeAsAMerc;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyNameplateVM))]
    public class PartyNameplateItemVMExtension : BaseViewModelExtension
    {
        private bool _isEnlisted = false;
        
        public PartyNameplateItemVMExtension(ViewModel vm) : base(vm)
        {
            var hireling = Campaign.Current.CampaignBehaviorManager.GetBehavior<ServeAsAHirelingCampaignBehavior>();
            if (hireling != null)
            {
                if (hireling.IsEnlisted())
                {
                    vm.SetPropertyValue("IsPrisoner",true);   // feels sad to write this, but the nameplate disappears
                }
            }
            RefreshValues();
        }

        public override void RefreshValues()
        {
            var vm = _vm as PartyNameplateVM;
            
            var hireling = Campaign.Current.CampaignBehaviorManager.GetBehavior<ServeAsAHirelingCampaignBehavior>();
            if (hireling != null)
            {
                if (hireling.IsEnlisted())
                {
                    vm.SetPropertyValue("IsPrisoner",true);   // feels sad to write this, but the nameplate disappears
                }
            }
            
        }
    }
}