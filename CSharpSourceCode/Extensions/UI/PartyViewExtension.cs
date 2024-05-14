using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyVM))]
    public class PartyViewExtension : BaseViewModelExtension
    {
        public PartyViewExtension(ViewModel vm) : base(vm)
        {

            var partyVM = vm as PartyVM;
            
            
            SpecialbuttonEventManagerHandler.Instance.SetCurrentPartyUiExtensionInstance(partyVM);
        }
    }
}