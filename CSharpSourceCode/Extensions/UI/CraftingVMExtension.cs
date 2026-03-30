using System.Reflection;
using TaleWorlds.CampaignSystem.ViewModelCollection.WeaponCrafting;
using TaleWorlds.Library;

namespace TOR_Core.Extensions.UI
{
    [ViewModelExtension(typeof(CraftingVM))]
    public class CraftingVMExtension : BaseViewModelExtension
    {
        private static CraftingVMExtension _currentInstance;
        public static CraftingVMExtension CurrentInstance => _currentInstance;

        private static readonly MethodInfo _updateAllMethod;

        static CraftingVMExtension()
        {
            _updateAllMethod = typeof(CraftingVM).GetMethod("UpdateAll", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public CraftingVMExtension(ViewModel vm) : base(vm)
        {
            _currentInstance = this;
        }

        public override void OnFinalize()
        {
            if (_currentInstance == this)
                _currentInstance = null;
            base.OnFinalize();
        }

        /// <summary>
        /// Triggers a full UI update including stamina bars, materials, etc.
        /// </summary>
        public void TriggerUpdateAll()
        {
            _updateAllMethod?.Invoke(_vm, null);
        }
    }
}
