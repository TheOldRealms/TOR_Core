using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyVM))]
    public class PartyVMExtension : BaseViewModelExtension
    {
        private PartyVM _pVM = null;
        public PartyVMExtension(ViewModel vm) : base(vm)
        {
            this.RefreshValues();
            
            SpecialbuttonEventManagerHandler.Instance.ButtonClickedEventHandler += RefreshExternal;
        }
        
        public void RefreshExternal(object sender, TroopEventArgs troopEventArgs)
        {
            ((PartyVM) _vm).RefreshValues();
        }

        public override void OnFinalize()
        {
            base.OnFinalize();
            SpecialbuttonEventManagerHandler.Instance.ButtonClickedEventHandler -= RefreshExternal;
        }
    }
    
    
}