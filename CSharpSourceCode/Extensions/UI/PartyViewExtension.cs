using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics;
using TOR_Core.HarmonyPatches;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(PartyVM))]
    public class PartyViewExtension : BaseViewModelExtension
    {
        public static PartyVM ViewModelInstance { get; private set; }

        public PartyViewExtension(ViewModel vm) : base(vm)
        {
            var partyVm = vm as PartyVM;
            ViewModelInstance = partyVm;
        }
        
        public override void OnFinalize()
        {
            base.OnFinalize();
            ViewModelInstance = null;
        }
    }
}