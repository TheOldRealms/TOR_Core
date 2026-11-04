using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TOR_Core.Extensions.UI;

namespace TOR_Core.Missions.AmbushMission.UI
{
    /// <summary>
    /// Adds a per-panel spot label to vanilla's OrderOfBattleFormationItemVM,
    /// so the ambusher sees which scene spot their selected formation is
    /// currently bound to. Reads the controller's AmbushLayoutResult every
    /// RefreshValues tick, finds an AmbushFormation whose FormationClass
    /// matches this panel's vanilla Formation, and renders the spot number
    /// into SpotLabel. Picks the first matching chunk when a class was
    /// split (follow-up slice can switch to per-vanilla-index chunking
    /// so each panel binds 1:1 to a chunk).
    /// </summary>
    [ViewModelExtension(typeof(OrderOfBattleFormationItemVM), "RefreshValues")]
    public class AmbushFormationPanelExtension : BaseViewModelExtension
    {
        private string _spotLabel = "";
        private bool _hasSpotAssignment;

        public AmbushFormationPanelExtension(ViewModel vm) : base(vm) { }

        [DataSourceProperty]
        public string SpotLabel
        {
            get => _spotLabel;
            set
            {
                if (value == _spotLabel) return;
                _spotLabel = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(SpotLabel));
            }
        }

        [DataSourceProperty]
        public bool HasSpotAssignment
        {
            get => _hasSpotAssignment;
            set
            {
                if (value == _hasSpotAssignment) return;
                _hasSpotAssignment = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(HasSpotAssignment));
            }
        }

        public override void RefreshValues()
        {
            base.RefreshValues();
            Recompute();
        }

        private void Recompute()
        {
            var panel = _vm as OrderOfBattleFormationItemVM;
            var formation = panel?.Formation;
            if (formation == null)
            {
                HasSpotAssignment = false;
                SpotLabel = "";
                return;
            }
            var controller = Mission.Current?.GetMissionBehavior<AmbushMissionController>();
            var layout = controller?.Layout;
            if (layout == null)
            {
                HasSpotAssignment = false;
                SpotLabel = "";
                return;
            }
            var targetClass = formation.FormationIndex;
            var spotNumber = FindSpotNumberForClass(layout, targetClass);
            if (spotNumber < 0)
            {
                HasSpotAssignment = false;
                SpotLabel = "";
                return;
            }
            HasSpotAssignment = true;
            SpotLabel = "Spot " + (spotNumber + 1);
        }

        private static int FindSpotNumberForClass(AmbushLayoutResult layout, FormationClass cls)
        {
            int index = 0;
            foreach (var kv in layout.DefaultAssignments)
            {
                if (kv.Key.Class == cls) return index;
                index++;
            }
            return -1;
        }

        /// <summary>
        /// Click handler for the spot chip in the OoB prefab override.
        /// Resolved by the TOR extension framework via reflection
        /// (BaseViewModelExtension.ExecuteCommand). Selects the formation
        /// first (same effect as clicking the parent panel) so the chip
        /// click doubles as a formation picker; then opens the spot
        /// picker (dropdown + reassignment logic come in follow-ups).
        /// Selecting first also fixes a sporadic "click not registered"
        /// behaviour where the chip's ButtonWidget would not fire until
        /// the formation had been activated some other way.
        /// </summary>
        public void ExecuteOpenSpotPicker()
        {
            var panel = _vm as OrderOfBattleFormationItemVM;
            if (panel == null) return;
            InvokeVanillaSelection(panel);
            NLog.LogManager.GetCurrentClassLogger().Info(
                "[AmbushFormationPanel] ExecuteOpenSpotPicker fired, class="
                + panel.Formation?.FormationIndex);
        }

        private static void InvokeVanillaSelection(OrderOfBattleFormationItemVM panel)
        {
            var method = typeof(OrderOfBattleFormationItemVM).GetMethod(
                "ExecuteSelection",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic);
            method?.Invoke(panel, null);
        }
    }
}
