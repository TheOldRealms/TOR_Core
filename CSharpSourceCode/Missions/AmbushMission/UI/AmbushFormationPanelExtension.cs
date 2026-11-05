using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

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
    // Hook Tick (per-frame) rather than RefreshValues. Vanilla's OoB
    // panel only calls RefreshValues on localization refresh and a few
    // explicit state changes — notably NOT when the class-weight lever
    // flips an empty slot into a valid formation. Tick runs every frame
    // on these panels so Recompute picks up the transition immediately
    // and the chip appears the moment the lever activates the formation.
    [ViewModelExtension(typeof(OrderOfBattleFormationItemVM), "Tick")]
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

        // Bumped whenever any panel reassigns a spot, so every extension
        // instance can notice (next time its own RefreshValues runs) that
        // somewhere a formation moved and re-dirty its cached plate
        // position. This is how sibling formations keep their troop plate
        // in sync when a share-spot reassignment shifts them laterally
        // without them being the one the user clicked.
        internal static int s_moveGeneration;
        private int _lastSeenMoveGeneration;

        /// <summary>Bump the shared move-generation counter so every
        /// panel extension re-dirties its cached plate position on the
        /// next RefreshValues. Called by the controller after Reset
        /// Deployment reassigns every formation.</summary>
        public static void BumpMoveGeneration() => s_moveGeneration++;

        private bool _warnedLifecycleGone;

        public override void RefreshValues()
        {
            base.RefreshValues();
            // Hooked on OrderOfBattleFormationItemVM.Tick so we fire per
            // frame during deployment. After mission end the VMs are
            // mid-teardown: Formation references become disposed, Mission
            // .Current flips to null, and vanilla's own Tick inside those
            // VMs starts reading dead state. Our Recompute reads from the
            // same places (Mission.Current, panel.Formation, the ambush
            // controller) and setting any [DataSourceProperty] on a
            // tearing-down VM fires OnPropertyChangedWithValue into a
            // Gauntlet layer that can wedge the UI thread. Bail the
            // moment Mission.Current is gone — our chip / spot picker is
            // a deployment-time widget, nothing we show matters after.
            if (Mission.Current == null || _vm == null)
            {
                if (!_warnedLifecycleGone)
                {
                    _warnedLifecycleGone = true;
                    NLog.LogManager.GetCurrentClassLogger().Info(
                        "[AmbushFormationPanel] UI lifecycle gone (mission="
                        + (Mission.Current != null) + " vm=" + (_vm != null)
                        + ") — skipping updates");
                }
                return;
            }
            Recompute();
            if (_lastSeenMoveGeneration != s_moveGeneration)
            {
                _lastSeenMoveGeneration = s_moveGeneration;
                (_vm as OrderOfBattleFormationItemVM)?.MakeMarkerWorldPositionDirty();
            }
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
        /// Fires an NLog line and an in-game message only when the hide
        /// reason changes, so a chip that stays hidden across many ticks
        /// produces one entry per state transition instead of 60 lines per
        /// second. The in-game echo is loud on purpose for the current
        /// debugging slice: split-formation panels aren't always showing
        /// the chip and we need the user to see why live.
        /// </summary>
        private void LogHideTransition(string reason)
        {
            if (reason == _lastHideReason) return;
            _lastHideReason = reason;
            NLog.LogManager.GetCurrentClassLogger().Info(
                "[AmbushFormationPanel] chip hidden: " + reason);
            TORCommon.Say("[Ambush] chip hidden: " + reason);
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
            var fallbackSpot = FindFirstUnusedSpot(controller);
            var created = new AmbushFormation
            {
                Class = vanilla.FormationIndex,
                Troops = new System.Collections.Generic.List<CharacterObject>(),
                VanillaFormation = vanilla,
                AssignedSpot = fallbackSpot,
            };
            controller.Layout.Formations.Add(created);
            TORCommon.Say("[Ambush] new chunk class=" + vanilla.FormationIndex
                          + " spot=" + (fallbackSpot.HasValue
                              ? fallbackSpot.Value.GroupTag ?? "(null-tag)"
                              : "(no-spot)"));
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
            // Per-spot occupancy: how many *other* ambush formations are
            // currently sitting here. Excludes the formation being
            // reassigned so its own current spot stays clickable (lets
            // the player cancel an in-progress click by re-picking the
            // same spot). Spots with MaxFormations > 0 and occupancy
            // >= MaxFormations render as disabled buttons.
            var occupancy = new System.Collections.Generic.Dictionary<Vec3, int>();
            foreach (var f in controller.Layout.Formations)
            {
                if (f == ambushFormation) continue;
                if (!f.AssignedSpot.HasValue) continue;
                var p = f.AssignedSpot.Value.Position;
                occupancy.TryGetValue(p, out int c);
                occupancy[p] = c + 1;
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
                    occupancy.TryGetValue(spot.Position, out int occ);
                    bool atCap = spot.MaxFormations > 0 && occ >= spot.MaxFormations;
                    bool isEnabled = isCurrent || !atCap;
                    var spotLocal = spot;
                    group.Items.Add(new AmbushSpotPickerItemVM(
                        number: number++,
                        isCurrent: isCurrent,
                        isEnabled: isEnabled,
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
            var oldSpot = formation.AssignedSpot;
            formation.AssignedSpot = spot;
            IsSpotPickerOpen = false;

            // Reposition every formation sitting on the newly-picked spot
            // (including this one) so share-spot formations lay out
            // side-by-side instead of stacking. If this formation just
            // vacated a different spot, re-lay-out the formations that
            // are left there too, so a two-formation spot that now only
            // has one re-centers cleanly instead of staying offset.
            RepositionSpotGroup(spot, controller);
            if (oldSpot.HasValue
                && !oldSpot.Value.Position.NearlyEquals(spot.Position, 0.01f))
            {
                RepositionSpotGroup(oldSpot.Value, controller);
            }
            s_moveGeneration++;

            var panel = _vm as OrderOfBattleFormationItemVM;
            panel?.MakeMarkerWorldPositionDirty();
            panel?.RefreshValues();
        }

        // Gap between neighbouring formation centres when two or more
        // formations share the same AmbushSpot. Narrow enough to feel
        // like one deployment "cluster" but wide enough that the two
        // formations read as separate.
        private const float ShareSpotSpacing = 10f;
        // Width used for each formation when sharing. Vanilla picks a
        // width from desired-troop-count; forcing a narrow custom width
        // via SetFormOrder gives the "two deeper columns" look the user
        // wants instead of one wide line overlapping another wide line.
        private const float ShareSpotWidth = 6f;

        /// <summary>
        /// Repositions every AmbushFormation currently assigned to the
        /// given spot. Single-formation spots place the formation at the
        /// spot's exact position with a wide line; shared spots lay the
        /// formations side-by-side along the perpendicular to the spot's
        /// facing, each compressed to <see cref="ShareSpotWidth"/> so the
        /// extra depth shows instead of overlapping wide lines.
        ///
        /// Each per-formation move mirrors vanilla
        /// BattleDeploymentHandler.AutoDeployTeamUsingDeploymentPlan:
        /// wrap in Mission.IsTeleportingAgents, SetMovementOrder(Move),
        /// SetPositioning (which also updates the formation plate's
        /// cached world point), teleport + ForceUpdate each agent, clear
        /// pending positions, finish with SetMovementOrder(Stop). A bare
        /// per-agent TeleportToPosition leaves the plate stuck on the
        /// old spot.
        /// </summary>
        public static void RepositionSpotGroup(AmbushSpot spot,
                                                AmbushMissionController controller)
        {
            var mission = Mission.Current;
            var scene = mission?.Scene;
            if (scene == null || controller?.Layout?.Formations == null) return;

            var shared = new System.Collections.Generic.List<AmbushFormation>();
            foreach (var f in controller.Layout.Formations)
            {
                if (f.VanillaFormation == null) continue;
                if (!f.AssignedSpot.HasValue) continue;
                if (!f.AssignedSpot.Value.Position.NearlyEquals(spot.Position, 0.01f))
                    continue;
                shared.Add(f);
            }
            // Deterministic ordering so the same formation ends up on
            // the same side of the spot across reassignments.
            shared.Sort((a, b) => ((int)a.Class).CompareTo((int)b.Class));

            int n = shared.Count;
            if (n == 0) return;

            // Facing convention matches AmbushMissionController's existing
            // use of yaw -> (cos, sin). Perpendicular is 90° CCW from
            // facing, so share-spot formations spread left/right of the
            // line the formation looks down.
            var facing = new Vec2((float)System.Math.Cos(spot.Yaw),
                                  (float)System.Math.Sin(spot.Yaw));
            if (facing.LengthSquared < 0.0001f) facing = new Vec2(0f, 1f);
            var perp = new Vec2(-facing.Y, facing.X);

            for (int i = 0; i < n; i++)
            {
                var f = shared[i];
                float lateral = (n == 1) ? 0f : (i - (n - 1) / 2f) * ShareSpotSpacing;
                var pos = spot.Position
                          + new Vec3(perp.X * lateral, perp.Y * lateral, 0f);
                RepositionFormation(f.VanillaFormation, pos, facing, scene,
                                    narrowWidth: n > 1 ? (float?)ShareSpotWidth : null);
                TORCommon.Say("[Ambush] " + spot.GroupTag + "[" + (i + 1) + "/" + n + "]"
                              + " class=" + f.Class
                              + " lateral=" + lateral.ToString("F1"));
            }
        }

        private static void RepositionFormation(TaleWorlds.MountAndBlade.Formation vanilla,
                                                Vec3 pos,
                                                Vec2 facing,
                                                Scene scene,
                                                float? narrowWidth)
        {
            var mission = Mission.Current;
            var worldPos = new WorldPosition(scene, pos);
            bool prevTeleporting = mission.IsTeleportingAgents;
            mission.IsTeleportingAgents = true;
            try
            {
                vanilla.SetFormOrder(narrowWidth.HasValue
                    ? FormOrder.FormOrderCustom(narrowWidth.Value)
                    : FormOrder.FormOrderWide);
                vanilla.SetMovementOrder(MovementOrder.MovementOrderMove(worldPos));
                vanilla.SetPositioning(worldPos, facing, vanilla.UnitSpacing);
                vanilla.SetFacingOrder(FacingOrder.FacingOrderLookAtDirection(facing));
                // Project each agent forward along facing by its row offset
                // so the line lies along the perpendicular to facing, same
                // orientation vanilla uses for a wide formation at spawn.
                vanilla.ApplyActionOnEachUnit(agent =>
                {
                    if (agent == null) return;
                    agent.TeleportToPosition(pos);
                    agent.ForceUpdateCachedAndFormationValues(
                        updateOnlyMovement: true,
                        arrangementChangeAllowed: false);
                });
                vanilla.SetHasPendingUnitPositions(hasPendingUnitPositions: false);
                vanilla.SetMovementOrder(MovementOrder.MovementOrderStop);
            }
            catch (System.Exception e)
            {
                TORCommon.Say("[Ambush] reposition failed: " + e.Message);
            }
            finally
            {
                mission.IsTeleportingAgents = prevTeleporting;
            }
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
