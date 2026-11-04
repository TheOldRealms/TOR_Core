using System.Linq;
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
        private bool _isSpotPickerOpen;
        private MBBindingList<AmbushSpotPickerItemVM> _spotPickerItems
            = new MBBindingList<AmbushSpotPickerItemVM>();

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

        [DataSourceProperty]
        public bool IsSpotPickerOpen
        {
            get => _isSpotPickerOpen;
            set
            {
                if (value == _isSpotPickerOpen) return;
                _isSpotPickerOpen = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(IsSpotPickerOpen));
            }
        }

        [DataSourceProperty]
        public MBBindingList<AmbushSpotPickerItemVM> SpotPickerItems
        {
            get => _spotPickerItems;
            set
            {
                if (value == _spotPickerItems) return;
                _spotPickerItems = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(SpotPickerItems));
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
            var ambushFormation = FindAmbushFormation(controller, formation.FormationIndex);
            var spot = ambushFormation?.AssignedSpot;
            if (controller == null || ambushFormation == null || !spot.HasValue
                || !controller.SpotNumbers.TryGetValue(spot.Value.Position, out var number))
            {
                HasSpotAssignment = false;
                SpotLabel = "";
                return;
            }
            HasSpotAssignment = true;
            SpotLabel = "Spot " + number;
        }

        private static AmbushFormation FindAmbushFormation(AmbushMissionController controller,
                                                            FormationClass cls)
        {
            if (controller?.Layout?.Formations == null) return null;
            foreach (var f in controller.Layout.Formations)
                if (f.Class == cls) return f;
            return null;
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
            if (IsSpotPickerOpen)
            {
                IsSpotPickerOpen = false;
                return;
            }
            PopulateSpotPickerItems();
            IsSpotPickerOpen = true;
        }

        private void PopulateSpotPickerItems()
        {
            SpotPickerItems.Clear();
            var panel = _vm as OrderOfBattleFormationItemVM;
            var controller = Mission.Current?.GetMissionBehavior<AmbushMissionController>();
            if (panel?.Formation == null || controller == null) return;
            var ambushFormation = FindAmbushFormation(controller, panel.Formation.FormationIndex);
            if (ambushFormation == null) return;
            var bucket = SpotClassExtensions
                .FromFormationClass(ambushFormation.Class)
                .Bucket();
            var currentPos = ambushFormation.AssignedSpot?.Position;
            foreach (var spot in controller.SceneSpots.Where(s => s.Bucket == bucket))
            {
                if (!controller.SpotNumbers.TryGetValue(spot.Position, out var number)) continue;
                bool isCurrent = currentPos.HasValue
                                 && currentPos.Value.NearlyEquals(spot.Position, 0.01f);
                SpotPickerItems.Add(new AmbushSpotPickerItemVM(
                    number: number,
                    isCurrent: isCurrent,
                    onSelected: item => AssignSpot(ambushFormation, spot, controller)));
            }
        }

        private void AssignSpot(AmbushFormation formation, AmbushSpot spot,
                                AmbushMissionController controller)
        {
            formation.AssignedSpot = spot;
            IsSpotPickerOpen = false;
            // Refresh the chip label to reflect the new binding. The vanilla
            // VM's RefreshValues walks through our RefreshValues too, so
            // asking the panel to refresh pushes the Recompute into
            // HasSpotAssignment + SpotLabel updates.
            (_vm as OrderOfBattleFormationItemVM)?.RefreshValues();
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
