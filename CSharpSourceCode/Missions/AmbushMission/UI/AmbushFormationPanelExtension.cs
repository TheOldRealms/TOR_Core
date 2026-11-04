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

        private string _lastHideReason;

        private void Recompute()
        {
            var panel = _vm as OrderOfBattleFormationItemVM;
            var vanillaFormation = panel?.Formation;
            // Hide the chip for empty / unassigned formation slots: the
            // sidebar still renders a card for every FormationClass, but
            // the ambush layout only cares about ones with troops in them.
            if (vanillaFormation == null || !PanelHasFormation(panel))
            {
                HasSpotAssignment = false;
                IsSpotPickerOpen = false;
                SpotLabel = "";
                LogHideTransition("empty-slot "
                    + (vanillaFormation == null ? "noFormation" : "noHasFormation")
                    + " class=" + vanillaFormation?.FormationIndex);
                return;
            }
            var controller = Mission.Current?.GetMissionBehavior<AmbushMissionController>();
            var ambushFormation = GetOrCreateAmbushFormation(controller, vanillaFormation);
            var spot = ambushFormation?.AssignedSpot;
            if (spot == null || string.IsNullOrEmpty(spot.Value.GroupTag))
            {
                HasSpotAssignment = false;
                SpotLabel = "";
                LogHideTransition("no-spot "
                    + (controller == null ? "noController" :
                       ambushFormation == null ? "noAmbushFormation" :
                       spot == null ? "noAssignedSpot" : "emptyGroupTag")
                    + " class=" + vanillaFormation.FormationIndex);
                return;
            }
            _lastHideReason = null;
            HasSpotAssignment = true;
            SpotLabel = LocalizedGroupName(spot.Value.GroupTag)
                       + " " + GlobalSpotNumber(controller, spot.Value);
        }

        /// <summary>
        /// Fires an NLog line only when the hide reason changes, so a
        /// chip that stays hidden across many ticks produces one entry
        /// per state transition instead of 60 lines per second.
        /// </summary>
        private void LogHideTransition(string reason)
        {
            if (reason == _lastHideReason) return;
            _lastHideReason = reason;
            NLog.LogManager.GetCurrentClassLogger().Info(
                "[AmbushFormationPanel] chip hidden: " + reason);
        }

        /// <summary>
        /// Mirrors the vanilla VM's own <c>HasFormation</c> flag (the one
        /// the prefab uses to show "Formation is empty" text at line 255).
        /// Accessed via reflection so the extension doesn't take a hard
        /// dependency on the exact property name should TaleWorlds rename
        /// it; falls back to a troop-count check if the property is gone.
        /// </summary>
        private static bool PanelHasFormation(OrderOfBattleFormationItemVM panel)
        {
            if (panel == null) return false;
            var prop = panel.GetType().GetProperty("HasFormation",
                System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic);
            if (prop != null && prop.PropertyType == typeof(bool))
                return (bool)prop.GetValue(panel);
            return panel.Formation != null && panel.Formation.CountOfUnits > 0;
        }

        /// <summary>
        /// Display number of the assigned spot, matching the picker's
        /// group-then-count layout: spots are first bucketed by GroupTag
        /// in first-appearance order, then numbered continuously across
        /// buckets starting at 1. So on a scene with Hill(4) + Street(4),
        /// the chip reads "Hill 3" ↔ third button in the Hill column and
        /// "Street 6" ↔ second button in the Street column (continues
        /// the global count, doesn't restart per group).
        /// </summary>
        private static int GlobalSpotNumber(AmbushMissionController controller, AmbushSpot spot)
        {
            var list = controller?.SceneSpots;
            if (list == null) return 0;
            var tagOrder = new System.Collections.Generic.List<string>();
            foreach (var s in list)
                if (!tagOrder.Contains(s.GroupTag)) tagOrder.Add(s.GroupTag);
            int n = 1;
            foreach (var tag in tagOrder)
                foreach (var s in list)
                {
                    if (s.GroupTag != tag) continue;
                    if (s.Position.NearlyEquals(spot.Position, 0.01f))
                        return n;
                    n++;
                }
            return 0;
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

            // Bucket SceneSpots by GroupTag in first-appearance order so
            // each column lists its own spots consecutively. Then number
            // continuously across buckets starting at 1, so Hill:1-4
            // + Street:5-8 instead of the interleaved 1,3,4,6 / 2,5,7,8
            // you'd get if we numbered in raw scene-XML order.
            var tagOrder = new System.Collections.Generic.List<string>();
            var buckets = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<AmbushSpot>>();
            foreach (var spot in controller.SceneSpots)
            {
                if (!buckets.TryGetValue(spot.GroupTag, out var bucket))
                {
                    bucket = new System.Collections.Generic.List<AmbushSpot>();
                    buckets[spot.GroupTag] = bucket;
                    tagOrder.Add(spot.GroupTag);
                }
                bucket.Add(spot);
            }
            int number = 1;
            foreach (var tag in tagOrder)
            {
                var group = new AmbushSpotPickerGroupVM(LocalizedGroupName(tag));
                SpotPickerGroups.Add(group);
                foreach (var spot in buckets[tag])
                {
                    bool isCurrent = currentPos.HasValue
                                     && currentPos.Value.NearlyEquals(spot.Position, 0.01f);
                    var spotLocal = spot;
                    group.Items.Add(new AmbushSpotPickerItemVM(
                        number: number++,
                        isCurrent: isCurrent,
                        onSelected: _ => AssignSpot(ambushFormation, spotLocal, controller)));
                }
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
