using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using NLog;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// Phase F AI side. Runs the quarter-hourly decision tick that puts
    /// eligible lord/bandit parties into stealth stance and triggers the
    /// ambushed mission when a stealthed AI party spots the player within
    /// strike range.
    ///
    /// Slice 2a scope: player as target only. AI-vs-AI ambush (headless
    /// skirmish resolver) is Slice 2b.
    ///
    /// Design: TORTasks/ambush-feature/ai-ambushes-player-design.md § 7.
    /// </summary>
    public class AiAmbushBehavior : CampaignBehaviorBase
    {
        // Parties currently in stealth stance → the time they entered.
        // Serialized so stealth survives save/load. Keyed by MobileParty
        // directly (not MBGUID) because MBObjectManager.GetObject does
        // not resolve live MobileParty objects — only CharacterObjects /
        // Items / Settlements are reliably in the manager under their
        // Id. The vanilla idiom for party-keyed state is a Dictionary<
        // MobileParty, …>; TOR already uses it in RetreatPatches.
        private Dictionary<MobileParty, CampaignTime> _stealthedParties
            = new Dictionary<MobileParty, CampaignTime>();

        // §19 preparation window. Parties mid-setup live here instead of
        // _stealthedParties — detection + strike-trigger ignore them. On
        // quarter-hourly tick, if the prep window elapsed the party moves
        // from here into _stealthedParties. Only start time stored; the
        // total prep window is recomputed from the model on each tick
        // (party is held in place, so size/composition don't drift).
        private Dictionary<MobileParty, CampaignTime> _preppingPartiesStartedAt
            = new Dictionary<MobileParty, CampaignTime>();

        // Player-spotted parties — timestamp when the quarter-hourly
        // detection roll succeeded. Within model.PlayerDetectionRevealHours
        // from this timestamp, TORMapVisibilityModel.IsVisibilityPersistent
        // returns true for the party, holding them visible on the
        // player's map despite fog-of-war distance. Short-lived: 2h by
        // default. Design: ai-ambushes-player-design.md §6.
        private Dictionary<MobileParty, CampaignTime> _playerDetectedParties
            = new Dictionary<MobileParty, CampaignTime>();

        // The single source of truth for all ambush-cooldown and
        // reveal logic. Every record is one ambush incident; every
        // query ("is party X on cooldown?", "is party Y a repelled
        // ambusher we should keep visible?", "has the player been
        // ambushed recently?") derives from scanning this list.
        //
        // Written at four points (LeaveAiStealth / TriggerAmbushAgainst-
        // Player / TriggerSkirmishAgainstAiParty / Phase D & F outcome
        // handlers); pruned on OnWeeklyTick to drop records older than
        // MaxRecordAgeHours.
        private List<AmbushRecord> _ambushRecords = new List<AmbushRecord>();

        // How long a stealthed AI party stays hidden before giving up and
        // leaving stealth on its own (no viable target walked past). Half
        // a game-day — long enough for one patrol pass to walk through
        // the strike radius, short enough that a kingdom's lords don't
        // sit frozen out of the vanilla AI pipeline (recruitment,
        // army-forming, kingdom council) for days at a time.
        private const float MaxStealthHoldHours = 12f;

        // Minimum healthy troop count for a party to count as "ambush-
        // capable". Below this, an ambush is logically possible but
        // produces a near-empty mission (healthy agents are the only ones
        // the spawn pipeline uses). 20 keeps the fiction tight — the
        // ambusher must have a real fighting force, not 5 stragglers.
        private const int MinHealthyForAmbush = 20;

        // Max AI-vs-AI skirmishes allowed to fire in a single tick. Each
        // one calls CreateFieldBattleEvent + roster mutations; stacking
        // many in one tick can hang vanilla's engagement pipeline.
        private const int MaxSkirmishesPerTick = 2;

        // Weekly-sweep retention. Any record older than this gets
        // dropped from _ambushRecords by OnWeeklyTick. 72h (3 game-days)
        // is well past any active cooldown or reveal window — all live
        // queries use ≤ 6h — so no in-use record gets caught by mistake.
        private const float MaxRecordAgeHours = 72f;

        // Transient tracker: the AI party currently ambushing the player.
        // Not serialized — only meaningful for the duration of an active
        // Phase F mission (set in TriggerAmbushAgainstPlayer, read by
        // AmbushCampaignBehavior.OnAmbushedMissionEnd to call RecordAmbush
        // with the right (ambusher, victim, repelled) tuple, cleared on
        // mission close). Save/load during a mission resets it to null,
        // which is correct — the mission can't survive save anyway.
        public MobileParty CurrentAmbusherOnPlayer { get; set; }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, OnQuarterHourlyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemoved);
        }

        // ---- Record authoring ----

        /// <summary>Append a resolved ambush record to the shared
        /// history. Normal write path is the <c>AmbushResolved</c> event
        /// — the <see cref="AmbushCampaignBehavior"/> subscriber calls
        /// this with the record carried on the event. Direct callers
        /// are the solo stealth-exit cooldown stamp (not a resolution).
        /// Records are dumb data — cooldown length, shield window, and
        /// instant-ambush eligibility are derived at query time by
        /// <see cref="Models.TORAmbushModel"/>.</summary>
        public void RecordAmbush(AmbushRecord record)
        {
            if (record == null) return;
            if (record.Ambusher == null && record.Victim == null) return;
            _ambushRecords.Add(record);
        }

        /// <summary>Most recent record where <paramref name="party"/>
        /// is Ambusher or Victim. Null if no record involves them in
        /// <paramref name="windowHours"/>; pass a large number to
        /// search unbounded. Used by hooded-opportunity + disorganize
        /// shield consumers who want the latest classified outcome.
        /// </summary>
        public AmbushRecord GetMostRecentRecordInvolving(MobileParty party, float windowHours = float.MaxValue)
        {
            if (party == null) return null;
            var now = CampaignTime.Now;
            AmbushRecord best = null;
            for (int i = _ambushRecords.Count - 1; i >= 0; i--)
            {
                var rec = _ambushRecords[i];
                if (!rec.Involves(party)) continue;
                if ((now - rec.At).ToHours > windowHours) continue;
                if (best == null || rec.At > best.At) best = rec;
            }
            return best;
        }

        // ---- Record queries ----

        /// <summary>True if <paramref name="party"/> is involved in any
        /// record (as ambusher or victim) whose cooldown is still
        /// active — the record's own cooldown length is computed by
        /// the model from the record's data (loudness etc.).</summary>
        public bool IsOnAmbushCooldown(MobileParty party)
        {
            if (party == null) return false;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return false;
            var now = CampaignTime.Now;
            for (int i = _ambushRecords.Count - 1; i >= 0; i--)
            {
                var rec = _ambushRecords[i];
                if (!rec.Involves(party)) continue;
                if ((now - rec.At).ToHours < model.GetAmbushCooldownHours(rec)) return true;
            }
            return false;
        }

        /// <summary>Most recent record where <paramref name="victim"/> was the
        /// VICTIM of an ambush that still lies within its active cooldown
        /// window, AND the ambusher was not repelled. Used by the eye-widget
        /// Backstab icon to light while the player is still in the post-
        /// ambush guarding window — mechanically a cooldown, fictionally
        /// "they just hit you, scouts are still searching." Returns
        /// <c>null</c> if no such record. See
        /// <see cref="Models.TORAmbushModel.AmbushGuardingHours"/>.</summary>
        /// <summary>
        /// True when <paramref name="party"/> is the ambusher of a
        /// non-repelled record created within
        /// <paramref name="windowMinutes"/>. Read by
        /// <see cref="Models.TORPartyImpairmentModel.CanGetDisorganized"/>
        /// to shield recent strikers from vanilla's
        /// DisorganizedStateCampaignBehavior.OnMapEventEnd re-apply.
        /// Uses the existing <see cref="AmbushRecord"/> list rather
        /// than a parallel map so there's one source of truth for
        /// "who struck when."
        /// </summary>
        public bool IsRecentSuccessfulAmbusher(MobileParty party, float windowMinutes)
        {
            if (party == null) return false;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return false;
            var now = CampaignTime.Now;
            for (int i = _ambushRecords.Count - 1; i >= 0; i--)
            {
                var rec = _ambushRecords[i];
                if (rec.Ambusher != party) continue;
                if (!model.IsSuccessfulStrike(rec)) continue;
                if ((now - rec.At).ToMinutes > windowMinutes) continue;
                return true;
            }
            return false;
        }

        public AmbushRecord GetMostRecentActiveVictimRecord(MobileParty victim)
        {
            if (victim == null) return null;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return null;
            var now = CampaignTime.Now;
            AmbushRecord best = null;
            for (int i = _ambushRecords.Count - 1; i >= 0; i--)
            {
                var rec = _ambushRecords[i];
                if (rec.Victim != victim) continue;
                if (rec.AmbusherRepelled) continue;
                if ((now - rec.At).ToHours >= model.GetAmbushCooldownHours(rec)) continue;
                if (best == null || rec.At > best.At) best = rec;
            }
            return best;
        }

        /// <summary>Game-time at which <paramref name="party"/>'s
        /// current cooldown expires. Returns <c>CampaignTime.Zero</c>
        /// if no active record involves the party. Used by UI
        /// (AmbushButtonVM) to render "available in Xh".</summary>
        public CampaignTime GetCooldownExpiresAt(MobileParty party)
        {
            if (party == null) return CampaignTime.Zero;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return CampaignTime.Zero;
            var now = CampaignTime.Now;
            CampaignTime latest = CampaignTime.Zero;
            foreach (var rec in _ambushRecords)
            {
                if (!rec.Involves(party)) continue;
                float cd = model.GetAmbushCooldownHours(rec);
                if ((now - rec.At).ToHours >= cd) continue;
                var expires = rec.At + CampaignTime.Hours(cd);
                if (expires > latest) latest = expires;
            }
            return latest;
        }

        /// <summary>True if there's a recent record where
        /// <paramref name="party"/> was the ambusher AND the attempt
        /// was repelled. Used by <c>TORMapVisibilityModel.IsVisibilityPersistent</c>
        /// to force-reveal the party on the campaign map. Shares the
        /// cooldown window — alertness and visibility expire together.</summary>
        public bool IsPartyRevealed(MobileParty party)
        {
            if (party == null) return false;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return false;
            var now = CampaignTime.Now;
            for (int i = _ambushRecords.Count - 1; i >= 0; i--)
            {
                var rec = _ambushRecords[i];
                if (!(rec.AmbusherRepelled && rec.Ambusher == party)) continue;
                if ((now - rec.At).ToHours < model.GetAmbushCooldownHours(rec)) return true;
            }
            return false;
        }

        /// <summary>Weekly orphan sweep. Drops any record older than
        /// <see cref="MaxRecordAgeHours"/>. Live cooldown / reveal
        /// queries only look at the recent window (≤ model cooldown)
        /// so stale records don't affect behaviour, but they'd
        /// otherwise accumulate indefinitely across long campaigns.</summary>
        private void OnWeeklyTick()
        {
            if (_ambushRecords.Count == 0) return;
            var now = CampaignTime.Now;
            int before = _ambushRecords.Count;
            _ambushRecords.RemoveAll(r => (now - r.At).ToHours >= MaxRecordAgeHours);
            int dropped = before - _ambushRecords.Count;
            if (dropped > 0)
                TORCommon.Log(LogLevel.Info, "[AI Ambush] weekly sweep: dropped " + dropped + " stale ambush records (kept " + _ambushRecords.Count + ")");
        }

        /// <summary>
        /// Fires on every save-game load. One-shot sanity scrub: for every
        /// party we think is stealthed, make sure it's actually clearable
        /// if the game state no longer matches. The earlier dict-mutation
        /// bug could leave parties with SetDoNotMakeNewDecisions(true) +
        /// IgnoreByOtherPartiesTill(+100 years) stuck in Hold mode, which
        /// manifests as "no party moves anymore". Call LeaveAiStealth on
        /// anything we're tracking so a loaded game starts clean.
        /// </summary>
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Precompute per-troop base stealth scores for the whole
            // CharacterObject roster. All factors in the base layer
            // (race / mount / knightly / weight / MasterAmbusher) are
            // static per troop StringId, so one sweep at session launch
            // avoids per-tick compute during decision ticks.
            AmbushTroopStealthScore.PrecomputeAll();
            TORCommon.Log(LogLevel.Info, "[AI Ambush] stealth-score base precomputed");

            // Session-launch sanity scrub for any mid-prep parties — same
            // reasoning as the stealthed clear below. Prep is transient
            // state; a loaded game shouldn't resume someone else's
            // half-committed setup.
            if (_preppingPartiesStartedAt.Count > 0)
            {
                var prepSnapshot = new List<MobileParty>(_preppingPartiesStartedAt.Keys);
                foreach (var party in prepSnapshot)
                {
                    _preppingPartiesStartedAt.Remove(party);
                    if (party != null && party.IsActive)
                    {
                        party.Ai.SetDoNotMakeNewDecisions(false);
                        party.IgnoreByOtherPartiesTill(CampaignTime.Zero);
                    }
                }
                TORCommon.Log(LogLevel.Info, "[AI Ambush] OnSessionLaunched: cleared "
                    + prepSnapshot.Count + " prepping parties from loaded save");
            }

            if (_stealthedParties.Count == 0) return;
            var snapshot = new List<MobileParty>(_stealthedParties.Keys);
            foreach (var party in snapshot)
            {
                if (party != null && party.IsActive) LeaveAiStealth(party);
                else _stealthedParties.Remove(party);
            }
            TORCommon.Log(LogLevel.Info, "[AI Ambush] OnSessionLaunched: cleared "
                + snapshot.Count + " stealthed parties from loaded save");
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_stealthedParties", ref _stealthedParties);
            dataStore.SyncData("_ambushRecords", ref _ambushRecords);
            dataStore.SyncData("_preppingPartiesStartedAt", ref _preppingPartiesStartedAt);
            dataStore.SyncData("_playerDetectedParties", ref _playerDetectedParties);
        }

        /// <summary>True while <paramref name="party"/> is in AI stealth
        /// stance. Readers (future Slice 3 reveal, Slice 2b auto-resolve
        /// bonus) query via this; nothing else should poke the dict.</summary>
        public bool IsPartyStealthed(MobileParty party)
            => party != null && _stealthedParties.ContainsKey(party);

        /// <summary>Game-time at which <paramref name="party"/> entered
        /// stealth. Used by the player-side detection pass to feed the
        /// model's duration-multiplier term (longer stealth = easier to
        /// spot). Returns <c>CampaignTime.Zero</c> for non-stealthed
        /// parties.</summary>
        public CampaignTime GetStealthEnteredAt(MobileParty party)
            => (party != null && _stealthedParties.TryGetValue(party, out var at))
                ? at : CampaignTime.Zero;

        /// <summary>Enumerate stealthed AI parties within
        /// <paramref name="radius"/> of <paramref name="centerParty"/>'s
        /// position. Used by the player-detection pass in
        /// <c>AmbushCampaignBehavior</c>.</summary>
        public IEnumerable<MobileParty> StealthedPartiesInRange(MobileParty centerParty, float radius)
        {
            if (centerParty == null || _stealthedParties.Count == 0) yield break;
            var center = centerParty.Position.ToVec2();
            float radiusSq = radius * radius;
            foreach (var p in _stealthedParties.Keys)
            {
                if (p == null || !p.IsActive) continue;
                if (p.Position.ToVec2().DistanceSquared(center) > radiusSq) continue;
                yield return p;
            }
        }

        /// <summary>Public break-stealth path for the player-detection
        /// pass on <c>AmbushCampaignBehavior</c>. Wraps the private
        /// <c>LeaveAiStealth</c> so detection can clear the stealth state
        /// and stamp the cooldown via the standard mechanism. Separate
        /// method so we can add detection-specific effects (persistent
        /// reveal window, special narration, Scouting-dependent reveal
        /// radius) without touching the broader LeaveAiStealth flow.
        /// </summary>
        public void BreakStealthByDetection(MobileParty party)
        {
            if (party == null || !_stealthedParties.ContainsKey(party)) return;
            LeaveAiStealth(party);
        }

        /// <summary>Timestamp a player-detection event on
        /// <paramref name="party"/>. Within
        /// <c>TORAmbushModel.PlayerDetectionRevealHours</c> from now,
        /// <see cref="IsRecentlyPlayerDetected"/> returns true, which
        /// <c>TORMapVisibilityModel.IsVisibilityPersistent</c> uses to
        /// hold the party visible on the player's map despite fog-of-war.
        /// Called from <c>AmbushCampaignBehavior.RunPlayerDetectsAiPass</c>
        /// right after the detection roll succeeds. Idempotent — a repeat
        /// detection resets the window.</summary>
        /// <summary>Enumerate currently-prepping AI parties. Used by
        /// <c>AmbushCampaignBehavior.OnSessionLaunched</c> to restore the
        /// runtime-only bluff shadow after save/load.</summary>
        public IEnumerable<MobileParty> PreppingParties => _preppingPartiesStartedAt.Keys;

        public void RecordPlayerDetection(MobileParty party)
        {
            if (party == null) return;
            _playerDetectedParties[party] = CampaignTime.Now;
        }

        /// <summary>True if <paramref name="party"/> was spotted by the
        /// player within the <c>PlayerDetectionRevealHours</c> window.
        /// Called from <see cref="TORMapVisibilityModel.IsVisibilityPersistent"/>.
        /// Lazy prune: expired entries are dropped here instead of in a
        /// dedicated tick so the window check and the cleanup share the
        /// same pass.</summary>
        public bool IsRecentlyPlayerDetected(MobileParty party)
        {
            if (party == null || _playerDetectedParties.Count == 0) return false;
            if (!_playerDetectedParties.TryGetValue(party, out var at)) return false;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            float windowHours = model?.PlayerDetectionRevealHours ?? 2f;
            if ((CampaignTime.Now - at).ToHours < windowHours) return true;
            _playerDetectedParties.Remove(party);
            return false;
        }

        /// <summary>Count already-stealthed friendly parties within
        /// <paramref name="radius"/> of <paramref name="party"/>. Used by
        /// the cluster penalty — too many friends hiding in the same
        /// spot makes the position obvious. "Friendly" = same MapFaction
        /// (allies, same-kingdom lords).</summary>
        public int CountStealthedFriendliesInRadius(MobileParty party, float radius)
        {
            if (party == null || _stealthedParties.Count == 0) return 0;
            var faction = party.MapFaction;
            if (faction == null) return 0;
            var selfPos = party.Position.ToVec2();
            float radiusSq = radius * radius;
            int n = 0;
            foreach (var other in _stealthedParties.Keys)
            {
                if (other == null || !other.IsActive || other == party) continue;
                if (other.MapFaction != faction) continue;
                if (other.Position.ToVec2().DistanceSquared(selfPos) > radiusSq) continue;
                n++;
            }
            return n;
        }

        // Shorthand alias — kept so the TickDecisions / FindClosest-
        // HostileAiParty call-sites read like before.
        private bool IsOnCooldown(MobileParty party) => IsOnAmbushCooldown(party);

        /// <summary>§19 — true if any hostile within
        /// <c>AiPrepPursuitRejectionRadius</c> would close the distance
        /// to <paramref name="party"/> within <paramref name="prepMinutes"/>
        /// at its current speed, AND is actually moving toward the party
        /// (dot of normalized velocity vs vector-to-ambusher &gt; 0).</summary>
        private bool WouldPursuerInterceptBeforePrepCompletes(MobileParty party,
            float prepMinutes, TORAmbushModel model)
        {
            var selfPos = party.Position.ToVec2();
            float rejectionRadius = model.AiPrepPursuitRejectionRadius;
            var query = MobileParty.StartFindingLocatablesAroundPosition(selfPos, rejectionRadius);
            for (var p = MobileParty.FindNextLocatable(ref query); p != null;
                 p = MobileParty.FindNextLocatable(ref query))
            {
                if (p == null || !p.IsActive || p == party) continue;
                if (p.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, p.MapFaction)) continue;

                float dist = (p.Position.ToVec2() - selfPos).Length;
                // Vanilla MovementSpeed is in map-u/hour (reads ~5-6 for
                // normal parties). Convert to u/minute for prep math.
                float speedPerMinute = p.Speed / 60f;
                float interceptDistance = prepMinutes * speedPerMinute;
                if (dist > interceptDistance) continue;

                // Direction gate — a pursuer moving away isn't a credible
                // threat. Normalized vector-to-ambusher dotted with
                // normalized pursuer-heading: dot > 0 means pursuer is
                // headed toward us. If TargetPosition is effectively the
                // pursuer's current position (idle party), skip the gate
                // and treat distance alone as the signal.
                var pPos = p.Position.ToVec2();
                var toSelf = (selfPos - pPos).Normalized();
                var headingRaw = p.TargetPosition.ToVec2() - pPos;
                if (headingRaw.LengthSquared > 0.01f)
                {
                    var heading = headingRaw.Normalized();
                    float dot = heading.x * toSelf.x + heading.y * toSelf.y;
                    if (dot < 0f) continue;  // pursuer heading away
                }
                return true;
            }
            return false;
        }

        private void OnQuarterHourlyTick()
        {
            if (Campaign.Current == null) return;

            TORCommon.Log(LogLevel.Info, "[AI Ambush] tick: start, stealthed=" + _stealthedParties.Count
                + " prepping=" + _preppingPartiesStartedAt.Count);
            PruneStealthedParties();
            TORCommon.Log(LogLevel.Info, "[AI Ambush] tick: pruned, stealthed=" + _stealthedParties.Count);
            TickPrepping();
            TickDecisions();
            TORCommon.Log(LogLevel.Info, "[AI Ambush] tick: decisions done, stealthed=" + _stealthedParties.Count);
            TickTargetDetection();
            TORCommon.Log(LogLevel.Info, "[AI Ambush] tick: targets done, stealthed=" + _stealthedParties.Count);
        }

        private void TickDecisions()
        {
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            foreach (var party in MobileParty.All)
            {
                if (!IsEligibleAmbusher(party)) continue;
                if (_stealthedParties.ContainsKey(party)) continue;
                if (_preppingPartiesStartedAt.ContainsKey(party)) continue;
                if (IsOnCooldown(party)) continue;

                float propensity = model.GetAmbushPropensity(party);
                if (propensity <= 0f) continue;

                // Reactive-stealth gate: branch on archetype.
                // - Striker (lord parties): opportunity + viability. Only
                //   stealths when a weak-enough target is in range to
                //   actually ambush.
                // - Hider (bandit parties): just "a hostile is nearby".
                //   Bandits hide reflexively from any threat, no viability
                //   check — they're not planning a strike against AI.
                var nearbyHostiles = CollectNearbyHostiles(party, model.AiOpportunityScanRadius);
                if (nearbyHostiles.Count == 0) continue;
                if (!DontStrikeAIParties(party))
                {
                    var bestTarget = FindBestEntryCandidate(party, nearbyHostiles, model, out float bestChance);
                    if (bestTarget == null || bestChance < model.AiMinEntryViabilityChance) continue;
                }

                // propensity gate (cheap).
                if (MBRandom.RandomFloat >= propensity * model.AiDecisionTickFraction) continue;

                // Entry chance roll — observer-penalty uses the same list
                // we already collected (scoped by DetectionScanRadius this
                // time, since that's what the entry calc expects).
                var observers = CollectNearbyHostiles(party, model.DetectionScanRadius);
                float entryChance = model.CalculateEntryChance(party, observers);
                // Cluster penalty: too many friendlies already stealthed
                // in the same area makes this one more obvious. Compounds
                // the base entry chance down; clamp against the min floor
                // so a dense cluster can't zero us out below the model's
                // own minimum.
                int clusterCount = CountStealthedFriendliesInRadius(party, model.FriendlyStealthClusterRadius);
                entryChance -= model.FriendlyStealthClusterPenalty(clusterCount);
                if (entryChance < model.MinEntryChance) entryChance = model.MinEntryChance;
                if (MBRandom.RandomFloat >= entryChance) continue;

                // §19 chase-geometry gate — reject stealth entry if any
                // hostile is close enough to intercept during the prep
                // window. Fixes the "AI cheeses the chase by vanishing
                // into stealth" exploit: a pursued party can't commit to
                // setup when a pursuer would overrun the position before
                // it's set.
                float prepMinutes = model.CalculatePreparationMinutes(party);
                if (WouldPursuerInterceptBeforePrepCompletes(party, prepMinutes, model))
                    continue;

                StartAiStealthPrep(party, prepMinutes);
            }
        }

        /// <summary>Pick the hostile from <paramref name="candidates"/> with
        /// the highest predicted CalculateAmbushSuccessChance. Returns null
        /// if none of them are plausible targets (e.g. all in encounters /
        /// settlements). <paramref name="bestChance"/> is 0 when null is
        /// returned.</summary>
        private static MobileParty FindBestEntryCandidate(MobileParty ambusher,
            List<MobileParty> candidates, TORAmbushModel model, out float bestChance)
        {
            MobileParty best = null;
            bestChance = 0f;
            foreach (var cand in candidates)
            {
                if (cand == null || !cand.IsActive) continue;
                if (cand.CurrentSettlement != null) continue;
                if (cand.MapEvent != null) continue;
                // CalculateAmbushSuccessChance takes a CampaignTime for
                // "stealth entered at" — we haven't entered yet, so pass
                // the current time (the "fresh entry" value, no duration
                // bonus / malus).
                float chance = model.CalculateAmbushSuccessChance(ambusher, cand, CampaignTime.Now);
                if (chance > bestChance)
                {
                    bestChance = chance;
                    best = cand;
                }
            }
            return best;
        }

        private void TickTargetDetection()
        {
            if (_stealthedParties.Count == 0) return;

            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;
            bool playerAvailable = IsPlayerAvailableAsTarget();
            var main = MobileParty.MainParty;
            Vec2 mainPos = main != null ? main.Position.ToVec2() : Vec2.Zero;

            // Grab a snapshot — triggering an ambush mutates the dict.
            var snapshot = new List<MobileParty>(_stealthedParties.Keys);
            int skirmishesFired = 0;
            foreach (var ambusher in snapshot)
            {
                if (ambusher == null || !ambusher.IsActive) continue;
                if (ambusher.MapFaction == null) continue;
                // Ambusher may have been dragged into a map event by
                // vanilla engagement AI while stealthed — IgnoreByOther-
                // PartiesTill isn't honoured by every vanilla path.
                // Drop them from stealth; vanilla will handle the event.
                if (ambusher.MapEvent != null)
                {
                    LeaveAiStealth(ambusher);
                    continue;
                }
                // Belt-and-suspenders: same for settlement entry. Prune
                // handles this on the next tick, but between prune and
                // target-detection a party could technically have been
                // pulled into a settlement (quest reaction, defender call).
                if (ambusher.CurrentSettlement != null)
                {
                    LeaveAiStealth(ambusher);
                    continue;
                }
                // Post-skirmish-wounding may have dropped the ambusher
                // below the fighting threshold since the last tick.
                if (ambusher.MemberRoster == null
                    || ambusher.MemberRoster.TotalHealthyCount < MinHealthyForAmbush)
                {
                    LeaveAiStealth(ambusher);
                    continue;
                }

                // Strike range is per-ambusher — scales with the
                // ambusher's effective Scouting. A high-Scouting party
                // reaches further.
                float strikeRadius = model.CalculateStrikeRange(ambusher);
                float strikeRadiusSq = strikeRadius * strikeRadius;

                // Priority 1: player in strike range. Fiction: the player
                // always counts as a notable target, so a stealthed party
                // springs on them when they walk past. Honors vanilla's
                // per-party post-encounter cooldown — the captor party
                // gets 12 h of DoNotAttackMainPartyUntil set by the
                // escape-captivity menu, which blocks regular engagement
                // and should block ambush engagement too.
                if (playerAvailable
                    && FactionManager.IsAtWarAgainstFaction(ambusher.MapFaction, main.MapFaction)
                    && ambusher.Ai.DoNotAttackMainPartyUntil.IsPast
                    && ambusher.Position.ToVec2().DistanceSquared(mainPos) <= strikeRadiusSq)
                {
                    TriggerAmbushAgainstPlayer(ambusher);
                    // One player-ambush per tick: PlayerEncounter is now
                    // occupied, no other stealthed party can trigger.
                    playerAvailable = false;
                    continue;
                }

                // Priority 2: closest AI victim — strikers only. Bandits
                // (hiders) don't initiate AI-vs-AI skirmishes; they just
                // stay hidden until the hostile moves away or stealth
                // times out. Multiple skirmishes CAN fire in a tick but
                // we cap them — each one touches vanilla's engagement
                // pipeline (CreateFieldBattleEvent + roster mutation).
                if (DontStrikeAIParties(ambusher)) continue;
                if (skirmishesFired >= MaxSkirmishesPerTick) continue;
                var aiVictim = FindClosestHostileAiParty(ambusher, strikeRadius);
                if (aiVictim != null)
                {
                    TriggerSkirmishAgainstAiParty(ambusher, aiVictim);
                    skirmishesFired++;
                }
            }
        }

        private static bool IsPlayerAvailableAsTarget()
        {
            var main = MobileParty.MainParty;
            if (main == null || !main.IsActive) return false;
            if (main.CurrentSettlement != null) return false;
            if (MapEvent.PlayerMapEvent != null) return false;
            if (TaleWorlds.MountAndBlade.Mission.Current != null) return false;
            if (main.MapFaction == null) return false;

            // Don't ambush a stationary player. Fiction: an ambush is
            // sprung on a column on the move; a camped party has their
            // perimeter set. Mechanic: IsMoving on MainParty returns
            // !Campaign.IsMainPartyWaiting, which is the clean "time is
            // flowing while the player is idle on the map" check.
            if (!main.IsMoving) return false;

            // Don't ambush a player already in stealth stance — would push
            // the AmbushedIntroMenu on top of the active stealth wait-menu,
            // which races the vanilla game-menu stack and can crash the
            // engine. Two parties lying in wait don't surprise each other
            // anyway; thematically fine.
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null && behavior.IsPlayerInStealth) return false;

            // Player-side cooldown: just drove off / was wounded by an
            // ambush recently, OR has their own stealth cooldown active.
            // Either way, the player is "alert" and can't be surprised
            // again for the cooldown window. Prevents the "two different
            // ambushers back-to-back" scenario entirely.
            //
            // Positive-evidence log: seeing this line in TOR_log confirms
            // the AmbushGuarding (TORAmbushModel.AmbushGuardingHours)
            // protection is firing for Phase F victim records. If the
            // 3h window elapses or no record is written, the gate falls
            // through and the AI tick continues as normal — meaning the
            // ABSENCE of this line across a few successful post-flee
            // ticks is not itself evidence of a bug. The user cannot
            // directly force a re-ambush within the window; use the
            // log line as cross-check.
            if (behavior != null && behavior.IsOnAmbushCooldown)
            {
                TORCommon.Log(LogLevel.Info, "[AI Ambush] player skip: on guarding cooldown");
                return false;
            }

            // Only ambush when the player is actually on the map screen.
            // If any other GameState is active (options, inventory, encounter,
            // conversation, trade, clan screen), opening a PlayerEncounter
            // and activating a game menu under a modal UI corrupts the
            // screen stack — observed as hard freezes under Proton/Vulkan.
            if (!(Game.Current?.GameStateManager?.ActiveState is MapState)) return false;

            return true;
        }

        private MobileParty FindClosestHostileAiParty(MobileParty ambusher, float strikeRadius)
        {
            float strikeRadiusSq = strikeRadius * strikeRadius;
            var ambusherPos = ambusher.Position.ToVec2();
            MobileParty closest = null;
            float closestDistSq = strikeRadiusSq;
            // LocatorGrid radius query — touches only the grid cells
            // overlapping the strike radius (~5-unit cells, grid-wide).
            // Keeps cost proportional to local density, not world size.
            var query = MobileParty.StartFindingLocatablesAroundPosition(ambusherPos, strikeRadius);
            for (var p = MobileParty.FindNextLocatable(ref query); p != null;
                 p = MobileParty.FindNextLocatable(ref query))
            {
                if (p == null || !p.IsActive || p == ambusher) continue;
                if (p == MobileParty.MainParty) continue;    // player handled above
                if (p.CurrentSettlement != null) continue;
                if (p.MapEvent != null) continue;
                if (p.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(ambusher.MapFaction, p.MapFaction)) continue;
                // Lord parties only — ambushing a bandit party reads as
                // immersion-breaking (a Warhammer lord doesn't lie in
                // wait to spring on looters; they just ride them down).
                // Caravans / villagers / militia keep their vanilla
                // engagement paths too.
                if (!p.IsLordParty) continue;
                if (p.MemberRoster == null || p.MemberRoster.TotalHealthyCount < MinHealthyForAmbush) continue;
                // Victim-side cooldown: don't pile onto a party that was
                // just ambushed by someone else, or who recently stealthed
                // themselves. _partyCooldownUntil now covers both roles.
                if (IsOnCooldown(p)) continue;
                float distSq = p.Position.ToVec2().DistanceSquared(ambusherPos);
                if (distSq > closestDistSq) continue;
                closest = p;
                closestDistSq = distSq;
            }
            return closest;
        }

        /// <summary>
        /// Fire a headless AI-vs-AI ambush skirmish. One chance roll
        /// against <c>CalculateAmbushSuccessChance</c> picks an outcome
        /// tier via <c>RollOutcomeTier</c>; damage is tier-scaled from
        /// the single skirmish base. Terrible fail flips the damage
        /// onto the ambusher — a goblin band that charged a scout
        /// patrol gets cut down for their trouble. Design: campaign-
        /// stealth-design.md § 13.
        /// </summary>
        private void TriggerSkirmishAgainstAiParty(MobileParty ambusher, MobileParty victim)
        {
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            // Roll outcome tier from the chance curve.
            float chance = model.CalculateAmbushSuccessChance(ambusher, victim, CampaignTime.Now);
            var tier = model.RollOutcomeTier(chance, MBRandom.RandomFloat);

            var (ambusherWounds, victimWounds) = model.CalculateSkirmishCasualties(ambusher, victim, tier);
            WoundTroops(ambusher.Party, ambusherWounds);
            WoundTroops(victim.Party, victimWounds);

            // Press-attack decision after wounding — terrible fail or
            // minor fail usually leaves the ambusher below the follow-
            // up threshold and forces a retreat naturally.
            bool press = model.ShouldPressAttackAfterSkirmish(ambusher, victim);
            LeaveAiStealth(ambusher);

            // Resolve + dispatch. AmbushCampaignBehavior's single
            // AmbushResolved subscriber owns the append to the history
            // list AND the shared post-strike effects (disorganize,
            // reveal, cooldown queries). TerribleFail flips the
            // Resolution to AmbusherDefeated. AI-on-AI has no
            // extraction mechanic, so successful outcomes map to
            // AmbusherTotalVictory regardless of residual victim
            // troops.
            bool victimWonExchange = tier == AmbushOutcomeTier.TerribleFail;
            var resolution = victimWonExchange
                ? AmbushResolution.AmbusherDefeated
                : AmbushResolution.AmbusherTotalVictory;
            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now, ambusher, victim, resolution));

            TORCommon.Say("[AI Ambush] " + PartyLabel(ambusher) + " struck "
                          + PartyLabel(victim) + " [" + tier + "]: wounded " + victimWounds
                          + " (took " + ambusherWounds + ") — "
                          + (press ? "pressing the attack." : "fading back into cover."));
            TORCommon.Log(LogLevel.Info, "[AI Ambush] skirmish @ "
                + ambusher.Position.ToVec2() + " ambusher=" + PartyLabel(ambusher)
                + " victim=" + PartyLabel(victim) + " tier=" + tier
                + " chance=" + chance.ToString("F2") + " press=" + press);

            // Only open the field battle if neither party has drifted into
            // a map event during this tick (another skirmish, player
            // engagement, etc.). CreateFieldBattleEvent on a party already
            // in a MapEvent hangs vanilla.
            if (press && ambusher.MapEvent == null && victim.MapEvent == null)
            {
                TORCommon.Log(LogLevel.Info, "[AI Ambush] press-attack opening field battle: "
                    + PartyLabel(ambusher) + " -> " + PartyLabel(victim));
                // Teleport the ambusher onto the victim and open a vanilla
                // field battle. The vanilla engagement AI + auto-resolve
                // take over from here — nearby allied-party reinforcements,
                // morale, composition all fold in naturally.
                ambusher.Position = victim.Position;
                FieldBattleEventComponent.CreateFieldBattleEvent(ambusher.Party, victim.Party);
                TORCommon.Log(LogLevel.Info, "[AI Ambush] press-attack field battle opened");
            }
            // Retreat branch (and the drifted-into-map-event guard above):
            // nothing further — ambusher is out of stealth, victim is
            // weakened. Both parties resume vanilla AI from here.
        }

        private static string PartyLabel(MobileParty p)
        {
            if (p == null) return "?";
            if (p.LeaderHero != null) return p.LeaderHero.Name.ToString();
            return p.Name?.ToString() ?? "?";
        }

        private static string CultureLabel(MobileParty p)
        {
            return p?.MapFaction?.Culture?.StringId ?? "?";
        }

        private static void WoundTroops(PartyBase party, int count)
        {
            if (party?.MemberRoster == null || count <= 0) return;
            // Snapshot the elements so we don't mutate the roster while
            // iterating it. Hero troop elements are skipped — only common
            // troops get wounded in a skirmish.
            var elements = new List<TroopRosterElement>();
            for (int i = 0; i < party.MemberRoster.Count; i++)
                elements.Add(party.MemberRoster.GetElementCopyAtIndex(i));

            int remaining = count;
            foreach (var elem in elements)
            {
                if (remaining <= 0) break;
                if (elem.Character == null || elem.Character.IsHero) continue;
                int available = elem.Number - elem.WoundedNumber;
                if (available <= 0) continue;
                int toWound = System.Math.Min(available, remaining);
                // count=0 keeps the total; woundedCount shifts healthy→wounded.
                party.MemberRoster.AddToCounts(elem.Character, 0, false, toWound);
                remaining -= toWound;
            }
        }

        private void TriggerAmbushAgainstPlayer(MobileParty ambusher)
        {
            // Teleport AI onto the player — matches the Phase F fiction
            // (lying in wait, springs on the player) and the debug trigger's
            // placement.
            ambusher.Position = MobileParty.MainParty.Position;

            // Leave stealth before the encounter opens so the stealth dict
            // doesn't hold a reference to a party now mid-combat.
            // LeaveAiStealth also writes a stealth-exit record (ambusher,
            // victim=null); the Phase F outcome handler on
            // AmbushCampaignBehavior will append the pair-incident record
            // with the correct AmbusherRepelled flag once the mission
            // closes. Keeping the two records distinct makes the history
            // readable: "X exited stealth" + "X ambushed MainParty with
            // outcome Y" as separate events.
            CurrentAmbusherOnPlayer = ambusher;

            PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, ambusher.Party, false);
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            // §6 bluff — hide troop counts across the whole ambusher
            // side (lead ambusher + any parties vanilla pulled onto the
            // same MapEvent side as reinforcements). Called AFTER
            // RestartPlayerEncounter so MapEvent.Parties is populated.
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.BeginBluffOnAmbusherSide(ambusher);
            GameMenu.ActivateGameMenu(AmbushCampaignBehavior.AmbushedIntroMenuId);
            TORCommon.Say("Ambush! " + (ambusher.LeaderHero?.Name?.ToString()
                                        ?? ambusher.Name?.ToString()
                                        ?? "A hostile force")
                          + " was lying in wait.");
            TORCommon.Log(LogLevel.Info, "[AI Ambush] player ambush @ "
                + MobileParty.MainParty.Position.ToVec2()
                + " ambusher=" + PartyLabel(ambusher));
        }

        /// <summary>§19 — start the preparation window for an AI party.
        /// Party is held in place and ignored by other AI, but NOT yet
        /// in <c>_stealthedParties</c>: strike-triggering and the
        /// IsVisibilityPersistent-based reveal path skip it until prep
        /// completes. <see cref="TickPrepping"/> finalizes once elapsed.
        /// </summary>
        private void StartAiStealthPrep(MobileParty party, float totalMinutes)
        {
            _preppingPartiesStartedAt[party] = CampaignTime.Now;
            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();
            // Cap the ignore window at the max stealth hold (48h). If
            // the prep/finalize cycle misses, the party unfreezes on its
            // own instead of being permanently invisible to other AI.
            party.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(MaxStealthHoldHours));
            // §6 bluff — prepping parties are visible on the map (per
            // §19 fiction) but their troop counts hide. All ambushing
            // parties bluff; perk-driven "see through bluff" is a
            // future design extension.
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.BeginBluffOn(party);
            // Prep does NOT vanish the party from the map — they're
            // still visibly moving into position. Only after finalize
            // do they become invisible (§19 fiction + chase-geometry
            // consistency: a pursuing player should see the enemy "stop
            // and set up" rather than magically disappear).
            TORCommon.Log(LogLevel.Info, "[AI Ambush] " + PartyLabel(party)
                + " (" + CultureLabel(party) + ") begin prep at "
                + party.Position.ToVec2() + " minutes=" + totalMinutes.ToString("F1"));
        }

        /// <summary>Promote a prep-complete party into full stealth
        /// stance. Mirrors the previous <c>EnterAiStealth</c> body:
        /// register in <c>_stealthedParties</c> and vanish from the
        /// map.</summary>
        private void FinalizeAiStealth(MobileParty party)
        {
            _preppingPartiesStartedAt.Remove(party);
            _stealthedParties[party] = CampaignTime.Now;
            party.IsVisible = false;
            TORCommon.Log(LogLevel.Info, "[AI Ambush] " + PartyLabel(party)
                + " (" + CultureLabel(party) + ") entered stealth stance at "
                + party.Position.ToVec2() + " (prep complete)");
        }

        /// <summary>Quarter-hourly check — any party whose prep window
        /// has elapsed graduates into full stealth stance. Called from
        /// <see cref="OnQuarterHourlyTick"/>.</summary>
        private void TickPrepping()
        {
            if (_preppingPartiesStartedAt.Count == 0) return;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;
            var now = CampaignTime.Now;
            var toFinalize = new List<MobileParty>();
            foreach (var kv in _preppingPartiesStartedAt)
            {
                if (kv.Key == null || !kv.Key.IsActive) continue;
                float elapsed = (float)(now - kv.Value).ToMinutes;
                float totalMinutes = model.CalculatePreparationMinutes(kv.Key);
                if (elapsed >= totalMinutes) toFinalize.Add(kv.Key);
            }
            foreach (var party in toFinalize) FinalizeAiStealth(party);
        }

        private void LeaveAiStealth(MobileParty party)
        {
            if (party == null) return;
            _stealthedParties.Remove(party);
            // Add a stealth-exit record (victim null, Resolution.None)
            // — covers burned strikes, vanilla-engagement drag-in, and
            // the max-hold-expired "gave up" case. All three share the
            // "position revealed / recently stealthed" semantics that
            // justify an ambush cooldown. Not a resolved exchange so
            // no AmbushResolved event; written directly.
            RecordAmbush(new AmbushRecord(CampaignTime.Now, party, null,
                                          AmbushResolution.None));
            // §6 bluff clear — leaving stealth exposes the party,
            // numbers back to normal. Safe against vanilla-set hiding
            // (hideouts etc.) because BeginBluffOn only tracks parties
            // we flipped from false → true.
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.EndBluffOn(party);
            if (party.IsActive)
            {
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.IgnoreByOtherPartiesTill(CampaignTime.Zero);
                party.IsVisible = true;
                TORCommon.Log(LogLevel.Info, "[AI Ambush] " + PartyLabel(party)
                    + " left stealth at " + party.Position.ToVec2()
                    + " (cooldown " + AmbushCampaignBehavior.CooldownHours + "h)");
            }
        }

        private void PruneStealthedParties()
        {
            if (_stealthedParties.Count == 0) return;
            var now = CampaignTime.Now;
            // Can't call LeaveAiStealth during the foreach — it removes from
            // _stealthedParties and would throw InvalidOperationException
            // (collection modified mid-iteration). Collect first, apply
            // after.
            var expired = new List<MobileParty>();
            var toLeave = new List<MobileParty>();
            foreach (var kv in _stealthedParties)
            {
                var party = kv.Key;
                if (party == null || !party.IsActive)
                {
                    expired.Add(party);
                    continue;
                }
                if (party.CurrentSettlement != null || party.MapEvent != null)
                {
                    toLeave.Add(party);
                    continue;
                }
                if ((now - kv.Value).ToHours >= MaxStealthHoldHours)
                {
                    toLeave.Add(party);
                }
            }
            foreach (var party in expired) _stealthedParties.Remove(party);
            foreach (var party in toLeave) LeaveAiStealth(party);
            if (expired.Count > 0 || toLeave.Count > 0)
            {
                TORCommon.Log(LogLevel.Info, "[AI Ambush] prune: destroyed/inactive="
                    + expired.Count + " left-stealth=" + toLeave.Count);
            }
        }

        private void OnPartyRemoved(PartyBase party)
        {
            if (party?.MobileParty == null) return;
            _stealthedParties.Remove(party.MobileParty);
            _preppingPartiesStartedAt.Remove(party.MobileParty);
            _playerDetectedParties.Remove(party.MobileParty);
            // §6 bluff cleanup — safety net, usually the AmbushCampaignBehavior
            // paths clear it first, but a destroyed party here means we
            // can't restore IsInfoHidden to false anyway (party is gone).
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.EndBluffOn(party.MobileParty);
            // Null out any record referencing the removed party so
            // queries don't match dangling refs. Keep the record (for
            // history completeness); nulled refs just stop matching.
            foreach (var rec in _ambushRecords)
            {
                if (rec.Ambusher == party.MobileParty) rec.Ambusher = null;
                if (rec.Victim == party.MobileParty) rec.Victim = null;
            }
            if (CurrentAmbusherOnPlayer == party.MobileParty)
                CurrentAmbusherOnPlayer = null;
        }

        /// <summary>
        /// Any party that CAN enter stealth. Lord parties count (strikers)
        /// and bandit parties count (hiders — see
        /// <see cref="DontStrikeAIParties"/>). Non-combatant parties, main hero,
        /// in-settlement, in-encounter, and too-small parties are out.
        /// </summary>
        private static bool IsEligibleAmbusher(MobileParty party)
        {
            if (party == null || !party.IsActive) return false;
            if (party == MobileParty.MainParty) return false;
            if (party.LeaderHero == Hero.MainHero) return false;
            if (party.CurrentSettlement != null) return false;
            if (party.MapEvent != null) return false;
            if (!party.IsLordParty && !party.IsBandit) return false;
            if (party.MemberRoster == null) return false;
            if (party.MemberRoster.TotalHealthyCount < MinHealthyForAmbush) return false;

            // Army followers are committed to the army's movement/plan.
            // Only lone lords and army leaders decide for themselves.
            if (party.Army != null && party.Army.LeaderParty != party) return false;

            // Stealth suspends ALL vanilla AI decisions
            // (SetDoNotMakeNewDecisions + Hold mode), so pulling a lord
            // mid-task starves the kingdom pipeline: no recruitment,
            // no army-forming, no fief visits, no kingdom-council
            // participation. Only lords without a committed goal are
            // candidates. Fleeing counts as a committed situation but
            // the fiction fits — a cornered lord "melts into the trees"
            // as guerrilla tactics. Bandits bypass this gate; they
            // don't participate in kingdom-level AI at all.
            if (party.IsLordParty)
            {
                switch (party.DefaultBehavior)
                {
                    case AiBehavior.Hold:
                    case AiBehavior.None:
                    case AiBehavior.PatrolAroundPoint:
                    case AiBehavior.FleeToPoint:
                    case AiBehavior.FleeToGate:
                    case AiBehavior.FleeToParty:
                        break;
                    default:
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// True for bandit parties — they enter stealth to HIDE from
        /// hostile AI, not to strike it. They can still ambush the
        /// player when the player walks past; the restriction is only
        /// on AI-vs-AI skirmishes. Lord parties (beastmen, chaos
        /// warbands, etc.) are the real strikers.
        /// </summary>
        private static bool DontStrikeAIParties(MobileParty party) => party != null && party.IsBandit;

        private static List<MobileParty> CollectNearbyHostiles(MobileParty self, float scanRadius)
        {
            var result = new List<MobileParty>();
            if (self == null) return result;
            var selfPos = self.Position.ToVec2();
            var selfFaction = self.MapFaction;
            if (selfFaction == null) return result;
            float radiusSq = scanRadius * scanRadius;
            // LocatorGrid returns a cell-superset — parties in cells
            // overlapping the radius, which includes some outside the
            // exact circle. Redundant distance check tightens it.
            var query = MobileParty.StartFindingLocatablesAroundPosition(selfPos, scanRadius);
            for (var p = MobileParty.FindNextLocatable(ref query); p != null;
                 p = MobileParty.FindNextLocatable(ref query))
            {
                if (p == null || !p.IsActive || p == self) continue;
                if (p.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(selfFaction, p.MapFaction)) continue;
                if (p.Position.ToVec2().DistanceSquared(selfPos) > radiusSq) continue;
                result.Add(p);
            }
            return result;
        }
    }
}
