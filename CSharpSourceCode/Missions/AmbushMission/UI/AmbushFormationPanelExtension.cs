using System.Linq;
using TaleWorlds.CampaignSystem;
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
        private MBBindingList<AmbushSpotPickerGroupVM> _spotPickerGroups
            = new MBBindingList<AmbushSpotPickerGroupVM>();

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
        public MBBindingList<AmbushSpotPickerGroupVM> SpotPickerGroups
        {
            get => _spotPickerGroups;
            set
            {
                if (value == _spotPickerGroups) return;
                _spotPickerGroups = value;
                _vm?.OnPropertyChangedWithValue(value, nameof(SpotPickerGroups));
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
            var vanillaFormation = panel?.Formation;
            if (vanillaFormation == null)
            {
                HasSpotAssignment = false;
                SpotLabel = "";
                return;
            }
            var controller = Mission.Current?.GetMissionBehavior<AmbushMissionController>();
            var ambushFormation = GetOrCreateAmbushFormation(controller, vanillaFormation);
            var spot = ambushFormation?.AssignedSpot;
            if (spot == null || string.IsNullOrEmpty(spot.Value.GroupTag))
            {
                HasSpotAssignment = false;
                SpotLabel = "";
                return;
            }
            HasSpotAssignment = true;
            SpotLabel = LocalizedGroupName(spot.Value.GroupTag);
        }

        /// <summary>
        /// Looks up the localized display name for a spot group tag via
        /// GameTexts (ID shape <c>tor_ambush_spawnpoint_tag.&lt;tag&gt;</c>).
        /// Falls back to the raw tag when no entry exists, so scener-added
        /// tags still render something human-readable until the strings
        /// file is extended.
        /// </summary>
        private static string LocalizedGroupName(string groupTag)
        {
            var text = TaleWorlds.Core.GameTexts.FindText(
                "tor_ambush_spawnpoint_tag", groupTag);
            var result = text?.ToString();
            return string.IsNullOrEmpty(result) || result == "ERROR"
                ? groupTag
                : result;
        }

        /// <summary>
        /// Resolves which AmbushFormation chunk this vanilla panel represents.
        /// Three cases, in order:
        /// (1) Already bound (VanillaFormation == this one). Reuse it.
        /// (2) Unbound chunk of matching class exists from the initial autochunk.
        ///     Bind it to this vanilla formation.
        /// (3) No matching chunk left (e.g. player just split a formation into a
        ///     new one). Create a new AmbushFormation, assign the first unused
        ///     spot in the matching bucket, and append it to the layout.
        /// </summary>
        private static AmbushFormation GetOrCreateAmbushFormation(
            AmbushMissionController controller,
            TaleWorlds.MountAndBlade.Formation vanilla)
        {
            if (controller?.Layout?.Formations == null) return null;
            foreach (var f in controller.Layout.Formations)
                if (f.VanillaFormation == vanilla) return f;
            foreach (var f in controller.Layout.Formations)
            {
                if (f.VanillaFormation == null && f.Class == vanilla.FormationIndex)
                {
                    f.VanillaFormation = vanilla;
                    return f;
                }
            }
            var created = new AmbushFormation
            {
                Class = vanilla.FormationIndex,
                Troops = new System.Collections.Generic.List<CharacterObject>(),
                VanillaFormation = vanilla,
                AssignedSpot = FindFirstUnusedSpot(controller),
            };
            controller.Layout.Formations.Add(created);
            return created;
        }

        private static AmbushSpot? FindFirstUnusedSpot(AmbushMissionController controller)
        {
            if (controller.SceneSpots == null) return null;
            var used = new System.Collections.Generic.HashSet<TaleWorlds.Library.Vec3>();
            foreach (var f in controller.Layout.Formations)
                if (f.AssignedSpot.HasValue) used.Add(f.AssignedSpot.Value.Position);
            foreach (var s in controller.SceneSpots)
                if (!used.Contains(s.Position)) return s;
            // All spots are already assigned. Return the first one so the
            // new formation has at least a valid spot to render.
            return controller.SceneSpots.Count > 0
                ? (AmbushSpot?)controller.SceneSpots[0]
                : null;
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
            SpotPickerGroups.Clear();
            var panel = _vm as OrderOfBattleFormationItemVM;
            var controller = Mission.Current?.GetMissionBehavior<AmbushMissionController>();
            if (panel?.Formation == null || controller == null) return;
            var ambushFormation = GetOrCreateAmbushFormation(controller, panel.Formation);
            if (ambushFormation == null) return;
            var currentPos = ambushFormation.AssignedSpot?.Position;

            // Scene-order global numbering, one group column per unique
            // GroupTag in first-appearance order. Columns stack horizontally
            // in the prefab; cells stack vertically within each column.
            var groupByTag = new System.Collections.Generic.Dictionary<string, AmbushSpotPickerGroupVM>();
            int globalNumber = 1;
            foreach (var spot in controller.SceneSpots)
            {
                if (!groupByTag.TryGetValue(spot.GroupTag, out var group))
                {
                    group = new AmbushSpotPickerGroupVM(LocalizedGroupName(spot.GroupTag));
                    groupByTag[spot.GroupTag] = group;
                    SpotPickerGroups.Add(group);
                }
                bool isCurrent = currentPos.HasValue
                                 && currentPos.Value.NearlyEquals(spot.Position, 0.01f);
                var spotLocal = spot;
                group.Items.Add(new AmbushSpotPickerItemVM(
                    number: globalNumber++,
                    isCurrent: isCurrent,
                    onSelected: _ => AssignSpot(ambushFormation, spotLocal, controller)));
            }
        }

        private void AssignSpot(AmbushFormation formation, AmbushSpot spot,
                                AmbushMissionController controller)
        {
            NLog.LogManager.GetCurrentClassLogger().Info(
                "[AmbushFormationPanel] AssignSpot class=" + formation.Class
                + " -> group " + spot.GroupTag);
            formation.AssignedSpot = spot;
            IsSpotPickerOpen = false;
            TeleportFormationAgentsToSpot(formation, spot);
            (_vm as OrderOfBattleFormationItemVM)?.RefreshValues();
        }

        /// <summary>
        /// Moves the agents of this AmbushFormation's bound vanilla Formation
        /// to the newly picked spot. Agents are already spawned during
        /// deployment setup via AmbushMissionController.OnAgentCreated using
        /// the initial layout, so reassigning AmbushFormation.AssignedSpot
        /// alone changes nothing visible. Iterates via the vanilla Formation
        /// directly so that a second formation of the same class (e.g. from
        /// a player split) does not get its agents dragged along.
        /// </summary>
        private void TeleportFormationAgentsToSpot(AmbushFormation formation, AmbushSpot spot)
        {
            var vanilla = formation.VanillaFormation;
            if (vanilla == null) return;
            int moved = 0;
            vanilla.ApplyActionOnEachUnit(agent =>
            {
                if (agent == null) return;
                agent.TeleportToPosition(spot.Position);
                moved++;
            });
            NLog.LogManager.GetCurrentClassLogger().Info(
                "[AmbushFormationPanel] teleported " + moved + " agents of class "
                + formation.Class + " to spot at " + spot.Position);
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
