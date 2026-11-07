using System.Text;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Ambush
{
    public class AmbushButtonVM : ViewModel
    {
        private const string EyeSpritePrefix = "ambush_eye_";
        private const int EyeFullyExposedIndex = 9;    // wide open: exposed / low concealment
        private const int EyeFullyCoveredIndex = 0;    // closed: perfectly hidden

        // UI-layer thresholds — not on the gameplay model because they don't
        // affect any math, just when icons light. A UI reskin / variant widget
        // can change these without touching the ambush model.

        /// <summary>How many game-hours after a detection-roll success the
        /// Spotted event icon stays lit. 0.5 = ~2 quarter-hour ticks —
        /// visible long enough to notice, short enough to not lie.</summary>
        private const float SpottedDisplayHours = 0.5f;

        /// <summary>Minimum simultaneous hostile observers in detection
        /// range before the Alert icon lights. 1 would fire on every
        /// patrol; 2+ reads as "area is crowded."</summary>
        private const int AlertObserverCountThreshold = 2;

        /// <summary>Map-unit distance within which a single hostile observer
        /// lights the Enemies-Close event icon. Tighter than
        /// <c>TORAmbushModel.DetectionScanRadius</c> (default ~10u); this
        /// fires when a party is IMMINENTLY close — about a third of the
        /// detection ring — regardless of how many observers are in the
        /// wider range.</summary>
        private const float CloseEnemyProximityRadius = 3.5f;

        // ---- Backing fields ----
        private bool _isVisible = true;
        private bool _isStealthActive;
        private bool _isStealthPreparing;
        private bool _isOnCooldown;
        private bool _isBorderRed;
        private string _eyeSprite = EyeSpritePrefix + EyeFullyExposedIndex;
        private BasicTooltipViewModel _eyeTooltip;

        // Event cluster (left)
        private bool _isEventSpotted;
        private bool _isEventAlert;
        private bool _isEventEnemiesClose;
        private bool _isEventCooldown;
        private bool _isEventBackstab;
        private BasicTooltipViewModel _spottedTooltip;
        private BasicTooltipViewModel _alertTooltip;
        private BasicTooltipViewModel _enemiesCloseTooltip;
        private BasicTooltipViewModel _cooldownTooltip;
        private BasicTooltipViewModel _backstabTooltip;

        // Terrain cluster (right, mutually exclusive)
        private bool _isTerrainForest;
        private bool _isTerrainMountain;
        private bool _isTerrainCanyon;
        private bool _isTerrainSwamp;
        private bool _isTerrainOpen;
        private BasicTooltipViewModel _terrainTooltip;

        // Context cluster (right, stackable). Settlement has two tiers —
        // hostile and neutral — with separate sprites + distinct tooltips
        // but the same proximity radius (TORAmbushModel.SettlementProximityRadius).
        private bool _isContextSnowy;
        private bool _isContextRainy;
        private bool _isContextCursed;
        private bool _isContextSettlementHostile;
        private bool _isContextSettlementNeutral;
        private BasicTooltipViewModel _snowyTooltip;
        private BasicTooltipViewModel _rainyTooltip;
        private BasicTooltipViewModel _cursedTooltip;
        private BasicTooltipViewModel _settlementHostileTooltip;
        private BasicTooltipViewModel _settlementNeutralTooltip;

        public AmbushButtonVM()
        {
            _eyeTooltip = new BasicTooltipViewModel(ComputeEyeBreakdownText);
            _spottedTooltip = new BasicTooltipViewModel(() => TORTextHelper.GetText(
                "tor_ambush_widget_tt_spotted", "You were spotted recently. Enemies are alert."));
            _alertTooltip = new BasicTooltipViewModel(ComputeAlertText);
            _enemiesCloseTooltip = new BasicTooltipViewModel(() => TORTextHelper.GetText(
                "tor_ambush_widget_tt_enemies_close",
                "A hostile party is very close — detection risk is high."));
            _cooldownTooltip = new BasicTooltipViewModel(ComputeCooldownText);
            _backstabTooltip = new BasicTooltipViewModel(ComputeBackstabText);
            _terrainTooltip = new BasicTooltipViewModel(ComputeTerrainText);
            _snowyTooltip = new BasicTooltipViewModel(() => TORTextHelper.GetText(
                "tor_ambush_widget_tt_snowy", "Snowy weather."));
            _rainyTooltip = new BasicTooltipViewModel(() => TORTextHelper.GetText(
                "tor_ambush_widget_tt_rainy", "Rainy weather."));
            _cursedTooltip = new BasicTooltipViewModel(() => TORTextHelper.GetText(
                "tor_ambush_widget_tt_cursed", "You are inside a cursed region."));
            _settlementHostileTooltip = new BasicTooltipViewModel(() => TORTextHelper.GetText(
                "tor_ambush_widget_tt_settlement_hostile",
                "A hostile settlement is nearby — garrisons are watching."));
            _settlementNeutralTooltip = new BasicTooltipViewModel(() => TORTextHelper.GetText(
                "tor_ambush_widget_tt_settlement_neutral",
                "A neutral settlement is nearby — passers-by notice more than empty roads do."));
            Refresh();
        }

        // ---------- Properties exposed to Gauntlet ----------

        [DataSourceProperty] public bool IsVisible { get => _isVisible; set { if (_isVisible == value) return; _isVisible = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsStealthActive { get => _isStealthActive; set { if (_isStealthActive == value) return; _isStealthActive = value; OnPropertyChangedWithValue(value); OnPropertyChanged(nameof(BorderColor)); } }
        [DataSourceProperty] public bool IsStealthPreparing { get => _isStealthPreparing; set { if (_isStealthPreparing == value) return; _isStealthPreparing = value; OnPropertyChangedWithValue(value); OnPropertyChanged(nameof(BorderColor)); } }
        [DataSourceProperty] public bool IsOnCooldown { get => _isOnCooldown; set { if (_isOnCooldown == value) return; _isOnCooldown = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsBorderRed { get => _isBorderRed; set { if (_isBorderRed == value) return; _isBorderRed = value; OnPropertyChangedWithValue(value); OnPropertyChanged(nameof(BorderColor)); } }

        /// <summary>Tint for the triangle backdrop. Three-state priority:
        /// gold when the player has committed to stealth (either actively
        /// hidden OR mid-prep — both count as "you chose stealth and it's
        /// taking hold"), red when ambush is unavailable (cooldown /
        /// backstab guarding), dark neutral when ambush is simply available.
        /// Bound to the Widget Color attribute in <c>AmbushStealthWidget.xml</c>.
        /// Pattern mirrors <c>GUI/Prefabs/Mission/FormationMarker.xml</c>'s
        /// VM-bound Brush.GlobalColor for team colour — proven to work
        /// in TOR's Gauntlet.</summary>
        [DataSourceProperty]
        public string BorderColor =>
            (_isStealthActive || _isStealthPreparing) ? "#D4A830FF" :  // gold — committed
            _isBorderRed                              ? "#B03030FF" :  // red — unavailable
                                                        "#1A1A1AC0"; // neutral dark — available
        [DataSourceProperty] public string EyeSprite { get => _eyeSprite; set { if (_eyeSprite == value) return; _eyeSprite = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel EyeTooltip { get => _eyeTooltip; set { if (_eyeTooltip == value) return; _eyeTooltip = value; OnPropertyChangedWithValue(value); } }

        [DataSourceProperty] public bool IsEventSpotted { get => _isEventSpotted; set { if (_isEventSpotted == value) return; _isEventSpotted = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsEventAlert { get => _isEventAlert; set { if (_isEventAlert == value) return; _isEventAlert = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsEventEnemiesClose { get => _isEventEnemiesClose; set { if (_isEventEnemiesClose == value) return; _isEventEnemiesClose = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsEventCooldown { get => _isEventCooldown; set { if (_isEventCooldown == value) return; _isEventCooldown = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsEventBackstab { get => _isEventBackstab; set { if (_isEventBackstab == value) return; _isEventBackstab = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel SpottedTooltip { get => _spottedTooltip; set { _spottedTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel AlertTooltip { get => _alertTooltip; set { _alertTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel EnemiesCloseTooltip { get => _enemiesCloseTooltip; set { _enemiesCloseTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel CooldownTooltip { get => _cooldownTooltip; set { _cooldownTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel BackstabTooltip { get => _backstabTooltip; set { _backstabTooltip = value; OnPropertyChangedWithValue(value); } }

        [DataSourceProperty] public bool IsTerrainForest { get => _isTerrainForest; set { if (_isTerrainForest == value) return; _isTerrainForest = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsTerrainMountain { get => _isTerrainMountain; set { if (_isTerrainMountain == value) return; _isTerrainMountain = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsTerrainCanyon { get => _isTerrainCanyon; set { if (_isTerrainCanyon == value) return; _isTerrainCanyon = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsTerrainSwamp { get => _isTerrainSwamp; set { if (_isTerrainSwamp == value) return; _isTerrainSwamp = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsTerrainOpen { get => _isTerrainOpen; set { if (_isTerrainOpen == value) return; _isTerrainOpen = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel TerrainTooltip { get => _terrainTooltip; set { _terrainTooltip = value; OnPropertyChangedWithValue(value); } }

        [DataSourceProperty] public bool IsContextSnowy { get => _isContextSnowy; set { if (_isContextSnowy == value) return; _isContextSnowy = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsContextRainy { get => _isContextRainy; set { if (_isContextRainy == value) return; _isContextRainy = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsContextCursed { get => _isContextCursed; set { if (_isContextCursed == value) return; _isContextCursed = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsContextSettlementHostile { get => _isContextSettlementHostile; set { if (_isContextSettlementHostile == value) return; _isContextSettlementHostile = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public bool IsContextSettlementNeutral { get => _isContextSettlementNeutral; set { if (_isContextSettlementNeutral == value) return; _isContextSettlementNeutral = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel SnowyTooltip { get => _snowyTooltip; set { _snowyTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel RainyTooltip { get => _rainyTooltip; set { _rainyTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel CursedTooltip { get => _cursedTooltip; set { _cursedTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel SettlementHostileTooltip { get => _settlementHostileTooltip; set { _settlementHostileTooltip = value; OnPropertyChangedWithValue(value); } }
        [DataSourceProperty] public BasicTooltipViewModel SettlementNeutralTooltip { get => _settlementNeutralTooltip; set { _settlementNeutralTooltip = value; OnPropertyChangedWithValue(value); } }

        // ---------- Click handler ----------

        public void ExecuteToggle()
        {
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()?.OnStealthHotkeyPressed();
        }

        // ---------- Refresh (called per OnMapScreenUpdate from AmbushMapView) ----------

        public void Refresh()
        {
            var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (ambush == null)
            {
                IsVisible = false;
                return;
            }

            IsVisible = CanShow();
            if (!IsVisible) return;

            var mainParty = MobileParty.MainParty;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (mainParty == null || model == null) return;

            IsStealthActive = ambush.IsPlayerInStealth;
            IsStealthPreparing = ambush.IsPlayerPreppingStealth;
            IsOnCooldown = ambush.IsOnAmbushCooldown;
            IsBorderRed = IsOnCooldown;    // §21: border red when ambush unavailable

            EyeSprite = ComputeEyeSprite(ambush, mainParty, model);

            UpdateEventFlags(ambush, model);
            UpdateTerrainFlags(mainParty);
            UpdateContextFlags(mainParty, model);
        }

        // ---------- Eye openness ----------

        private static string ComputeEyeSprite(AmbushCampaignBehavior ambush, MobileParty mainParty, TORAmbushModel model)
        {
            // §21: eye semantics flip with stealth state.
            //
            //   NOT stealthed → how likely you'd succeed at entering stealth right now.
            //                    High chance → closed eye (will slip in cleanly).
            //                    Low chance  → open eye (visible, hard to hide).
            //
            //   IN stealth    → aggregated chance of being detected on the next
            //                    quarter-hour tick (1 - ∏(1 - p_i) across every
            //                    nearby observer). High detection chance → open eye
            //                    (about to be seen); low chance → closed eye (safe).
            float displayValue;
            if (ambush.IsPlayerInStealth)
            {
                float detection = model.CalculateAggregatedDetectionChancePerTick(
                    mainParty, ambush.CachedObserversInRange, ambush.StealthEnteredAt);
                displayValue = detection;   // high detection → high idx = open eye
            }
            else
            {
                float entry = model.CalculateEntryChance(mainParty, ambush.CachedObserversInRange);
                displayValue = 1f - entry;  // high entry → low idx = closed eye
            }

            int idx = (int)(displayValue * 10f);
            if (idx < EyeFullyCoveredIndex) idx = EyeFullyCoveredIndex;
            if (idx > EyeFullyExposedIndex) idx = EyeFullyExposedIndex;
            return EyeSpritePrefix + idx;
        }

        // ---------- Flag updates ----------

        private void UpdateEventFlags(AmbushCampaignBehavior ambush, TORAmbushModel model)
        {
            // Spotted — timestamp within SpottedDisplayHours of now.
            var spottedAt = ambush.PlayerSpottedAt;
            IsEventSpotted = spottedAt != CampaignTime.Zero
                             && (CampaignTime.Now - spottedAt).ToHours < SpottedDisplayHours;

            // Alert — observer count clears threshold.
            IsEventAlert = ambush.CachedObserversInRange.Count >= AlertObserverCountThreshold;

            // Enemies close — any observer within the tight proximity radius.
            // Fires independently of Alert (the two can co-light: one very close
            // enemy plus multiple others in the wider detection range).
            IsEventEnemiesClose = HasObserverWithinRadius(ambush, CloseEnemyProximityRadius);

            // Backstab — a recent record where the player was the victim (not repelled)
            // is still inside its guarding window. Takes priority as a distinct icon
            // over Cooldown even though the underlying mechanic is the same gate.
            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            IsEventBackstab = ai?.GetMostRecentActiveVictimRecord(MobileParty.MainParty) != null;

            // Cooldown — any active cooldown that isn't already surfaced by the
            // backstab icon. Keeps the two icons semantically distinct.
            IsEventCooldown = ambush.IsOnAmbushCooldown && !IsEventBackstab;
        }

        private void UpdateTerrainFlags(MobileParty mainParty)
        {
            var face = mainParty.CurrentNavigationFace;
            if (!face.IsValid())
            {
                IsTerrainForest = IsTerrainMountain = IsTerrainCanyon = IsTerrainSwamp = IsTerrainOpen = false;
                return;
            }
            var terrain = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(face);
            IsTerrainForest   = terrain == TerrainType.Forest;
            IsTerrainMountain = terrain == TerrainType.Mountain;
            IsTerrainCanyon   = terrain == TerrainType.Canyon;
            IsTerrainSwamp    = terrain == TerrainType.Swamp;
            IsTerrainOpen     = terrain == TerrainType.Plain || terrain == TerrainType.Desert
                                || terrain == TerrainType.Steppe || terrain == TerrainType.Dune;
        }

        private void UpdateContextFlags(MobileParty mainParty, TORAmbushModel model)
        {
            // Weather — snowy / rainy per vanilla MapWeatherModel at party position.
            var weatherModel = Campaign.Current.Models.MapWeatherModel;
            var weather = weatherModel.GetWeatherEventInPosition(mainParty.Position.ToVec2());
            IsContextSnowy = weather == MapWeatherModel.WeatherEvent.Snowy
                             || weather == MapWeatherModel.WeatherEvent.Blizzard;
            IsContextRainy = weather == MapWeatherModel.WeatherEvent.LightRain
                             || weather == MapWeatherModel.WeatherEvent.HeavyRain;

            // Cursed region — pattern lifted from MobilePartyExtensions.cs:76.
            IsContextCursed = TORCommon.FindNearestSettlement(
                mainParty, TORConstants.DEFAULT_CURSE_RADIUS,
                x => x.SettlementComponent is CursedSiteComponent) != null;

            // Settlement nearby — two-tier via model's classifier. Hostile wins
            // over neutral when both are in range (model's priority).
            var kind = model.GetNearbySettlementKind(mainParty);
            IsContextSettlementHostile = kind == SettlementProximityKind.Hostile;
            IsContextSettlementNeutral = kind == SettlementProximityKind.Neutral;
        }

        /// <summary>True when at least one cached hostile observer sits within
        /// <paramref name="radius"/> of the main party. Reads from the behavior's
        /// cached observer list (populated once per quarter-hour tick) so it's
        /// cheap to call per-frame from Refresh.</summary>
        private static bool HasObserverWithinRadius(AmbushCampaignBehavior ambush, float radius)
        {
            var main = MobileParty.MainParty;
            if (main == null) return false;
            var mainPos = main.Position.ToVec2();
            float radiusSq = radius * radius;
            foreach (var observer in ambush.CachedObserversInRange)
            {
                if (observer == null) continue;
                if (observer.Position.ToVec2().DistanceSquared(mainPos) <= radiusSq) return true;
            }
            return false;
        }

        // ---------- Tooltip text computations ----------

        private string ComputeEyeBreakdownText()
        {
            var mainParty = MobileParty.MainParty;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (mainParty == null || model == null || ambush == null)
                return TORTextHelper.GetText("tor_ambush_widget_tt_eye_unavailable",
                                              "Stealth information unavailable.");

            return IsStealthActive
                ? ComputeDetectionBreakdown(mainParty, model, ambush)
                : ComputeEntryBreakdown(mainParty, model, ambush);
        }

        private string ComputeEntryBreakdown(MobileParty mainParty, TORAmbushModel model, AmbushCampaignBehavior ambush)
        {
            var observers = ambush.CachedObserversInRange;
            float chance = model.CalculateEntryChance(mainParty, observers);

            // Use GetTextObject (returns TextObject) rather than GetText (returns string)
            // so the variable slots are honoured — wrapping a stringified result in a new
            // TextObject does NOT reliably re-parse {VAR} tokens.
            var sb = new StringBuilder();
            var header = TORTextHelper.GetTextObject(
                "tor_ambush_widget_tt_eye_header", "Stealth entry chance: {PCT}%");
            header.SetTextVariable("PCT", (int)(chance * 100f));
            sb.AppendLine(header.ToString());

            var baseLine = TORTextHelper.GetTextObject(
                "tor_ambush_widget_tt_eye_base", "  Base: {PCT}%");
            baseLine.SetTextVariable("PCT", (int)(model.BaseEntryChance * 100f));
            sb.AppendLine(baseLine.ToString());

            var obsLine = TORTextHelper.GetTextObject(
                "tor_ambush_widget_tt_eye_observers", "  Hostile observers in range: {COUNT}");
            obsLine.SetTextVariable("COUNT", observers.Count);
            sb.AppendLine(obsLine.ToString());

            if (Campaign.Current.IsNight)
            {
                var nightLine = TORTextHelper.GetTextObject(
                    "tor_ambush_widget_tt_eye_night", "  Night: +{PCT}%");
                nightLine.SetTextVariable("PCT", (int)(model.NightTimeBonus * 100f));
                sb.AppendLine(nightLine.ToString());
            }

            sb.AppendLine();
            sb.Append(TORTextHelper.GetText("tor_ambush_widget_tt_eye_click_enter",
                                             "(Click to enter stealth)"));
            return sb.ToString();
        }

        private string ComputeDetectionBreakdown(MobileParty mainParty, TORAmbushModel model, AmbushCampaignBehavior ambush)
        {
            var observers = ambush.CachedObserversInRange;
            float aggregate = model.CalculateAggregatedDetectionChancePerTick(
                mainParty, observers, ambush.StealthEnteredAt);

            var sb = new StringBuilder();
            var header = TORTextHelper.GetTextObject(
                "tor_ambush_widget_tt_eye_detection_header",
                "Chance of being spotted next tick: {PCT}%");
            header.SetTextVariable("PCT", (int)(aggregate * 100f));
            sb.AppendLine(header.ToString());

            var obsLine = TORTextHelper.GetTextObject(
                "tor_ambush_widget_tt_eye_observers", "  Hostile observers in range: {COUNT}");
            obsLine.SetTextVariable("COUNT", observers.Count);
            sb.AppendLine(obsLine.ToString());

            // Note any strong modifiers currently contributing. (Settlement nearby is
            // likely the biggest stealthed-side signal that isn't already surfaced
            // by the observer count — call it out explicitly.)
            var settlementKind = model.GetNearbySettlementKind(mainParty);
            if (settlementKind == SettlementProximityKind.Hostile)
                sb.AppendLine(TORTextHelper.GetText(
                    "tor_ambush_widget_tt_eye_settlement_hostile",
                    "  Hostile settlement nearby: strong pressure"));
            else if (settlementKind == SettlementProximityKind.Neutral)
                sb.AppendLine(TORTextHelper.GetText(
                    "tor_ambush_widget_tt_eye_settlement_neutral",
                    "  Neutral settlement nearby: slight pressure"));

            if (Campaign.Current.IsNight)
                sb.AppendLine(TORTextHelper.GetText(
                    "tor_ambush_widget_tt_eye_night_stealth",
                    "  Night: detection reduced"));

            sb.AppendLine();
            sb.Append(TORTextHelper.GetText("tor_ambush_widget_tt_eye_click_exit",
                                             "(In stealth — click to break cover)"));
            return sb.ToString();
        }

        private string ComputeAlertText()
        {
            var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            int count = ambush?.CachedObserversInRange.Count ?? 0;
            var text = TORTextHelper.GetTextObject(
                "tor_ambush_widget_tt_alert", "{COUNT} hostile parties in detection range.");
            text.SetTextVariable("COUNT", count);
            return text.ToString();
        }

        private string ComputeCooldownText()
        {
            var ambush = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            var until = ambush?.PlayerAmbushCooldownUntil;
            if (until.HasValue && until.Value.IsFuture)
            {
                int hours = (int)System.Math.Ceiling((until.Value - CampaignTime.Now).ToHours);
                if (hours > 0)
                {
                    var text = TORTextHelper.GetTextObject(
                        "tor_ambush_widget_tt_cooldown_hours",
                        "Stealth cooling down — available in {HOURS}h.");
                    text.SetTextVariable("HOURS", hours);
                    return text.ToString();
                }
            }
            return TORTextHelper.GetText("tor_ambush_widget_tt_cooldown_now",
                                          "Stealth cooling down.");
        }

        private string ComputeBackstabText()
        {
            var ai = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            var rec = ai?.GetMostRecentActiveVictimRecord(MobileParty.MainParty);
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (rec == null || model == null)
                return TORTextHelper.GetText("tor_ambush_widget_tt_backstab_now",
                                              "Recently ambushed — enemies are still searching.");

            float cd = model.GetAmbushCooldownHours(rec);
            float elapsed = (float)(CampaignTime.Now - rec.At).ToHours;
            int remaining = (int)System.Math.Ceiling(cd - elapsed);
            if (remaining < 0) remaining = 0;
            var text = TORTextHelper.GetTextObject(
                "tor_ambush_widget_tt_backstab_hours",
                "Recently ambushed — enemies still searching. Clear in {HOURS}h.");
            text.SetTextVariable("HOURS", remaining);
            return text.ToString();
        }

        private string ComputeTerrainText()
        {
            if (IsTerrainForest)   return TORTextHelper.GetText("tor_ambush_widget_tt_terrain_forest",
                                                                 "Forest — concealing terrain, bonus to entry chance.");
            if (IsTerrainMountain) return TORTextHelper.GetText("tor_ambush_widget_tt_terrain_mountain",
                                                                 "Mountain — concealing terrain, bonus to entry chance.");
            if (IsTerrainCanyon)   return TORTextHelper.GetText("tor_ambush_widget_tt_terrain_canyon",
                                                                 "Canyon — concealing terrain, bonus to entry chance.");
            if (IsTerrainSwamp)    return TORTextHelper.GetText("tor_ambush_widget_tt_terrain_swamp",
                                                                 "Swamp — concealing terrain, bonus to entry chance.");
            if (IsTerrainOpen)     return TORTextHelper.GetText("tor_ambush_widget_tt_terrain_open",
                                                                 "Open ground — exposing, penalty to entry chance.");
            return "";
        }

        // ---------- Visibility gate ----------

        private static bool CanShow()
        {
            if (Hero.MainHero == null || Hero.MainHero.IsPrisoner) return false;
            if (TaleWorlds.MountAndBlade.Mission.Current != null) return false;
            if (MobileParty.MainParty == null || !MobileParty.MainParty.IsActive) return false;
            if (MobileParty.MainParty.CurrentSettlement != null) return false;
            if (MapEvent.PlayerMapEvent != null) return false;
            return true;
        }
    }
}
