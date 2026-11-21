using System.Collections.Generic;
using System.Linq;
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
using TOR_Core.Models.Ambush;

namespace TOR_Core.CampaignMechanics.Ambush
{
    /// <summary>
    /// AI side of the ambush system. Decides which hostile parties enter stealth and springs ambushes on the player or weaker AI parties.
    /// </summary>
    public class AiAmbushBehavior : CampaignBehaviorBase
    {
        private Dictionary<MobileParty, CampaignTime> _stealthedParties
            = new Dictionary<MobileParty, CampaignTime>();
        private Dictionary<MobileParty, CampaignTime> _preppingPartiesStartedAt
            = new Dictionary<MobileParty, CampaignTime>();
        private Dictionary<MobileParty, CampaignTime> _playerDetectedParties
            = new Dictionary<MobileParty, CampaignTime>();
        private List<AmbushRecord> _ambushRecords = new List<AmbushRecord>();

        private const float MaxStealthHoldHours = 12f;
        private const int MinHealthyForAmbush = 20;
        private const int MaxSkirmishesPerTick = 2;
        private const float MaxRecordAgeHours = 72f;

        /// <summary>
        /// The AI party currently ambushing the player. Transient, not serialized.
        /// </summary>
        public MobileParty CurrentAmbusherOnPlayer { get; set; }

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, OnQuarterHourlyTick);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, OnWeeklyTick);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemoved);
        }

        /// <summary>
        /// Append a resolved ambush record to the history.
        /// </summary>
        public void RecordAmbush(AmbushRecord record)
        {
            if (record == null) return;
            if (record.Ambusher == null && record.Victim == null) return;
            _ambushRecords.Add(record);
        }

        /// <summary>
        /// Most recent record involving the party within the given window.
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

        /// <summary>
        /// True if the party is still inside a cooldown window from any record.
        /// </summary>
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

        /// <summary>
        /// True when the party was the ambusher of a non-repelled record created within the window.
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

        /// <summary>
        /// True when the party was the victim of a non-repelled record created within the window.
        /// </summary>
        public bool IsRecentAmbushVictim(MobileParty party, float windowMinutes)
        {
            if (party == null) return false;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return false;
            var now = CampaignTime.Now;
            for (int i = _ambushRecords.Count - 1; i >= 0; i--)
            {
                var rec = _ambushRecords[i];
                if (rec.Victim != party) continue;
                if (!model.IsSuccessfulStrike(rec)) continue;
                if ((now - rec.At).ToMinutes > windowMinutes) continue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Most recent active record where the party was a non-repelled victim.
        /// </summary>
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

        /// <summary>
        /// Game-time at which the party's current cooldown expires, or zero if none active.
        /// </summary>
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

        /// <summary>True if the party was a repelled ambusher within its cooldown window
        /// and the player was involved, either as victim or as a nearby ally of the victim.</summary>
        public bool IsPartyRevealed(MobileParty party)
        {
            if (party == null) return false;
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return false;
            var mainParty = MobileParty.MainParty;
            if (mainParty == null) return false;
            var playerFaction = mainParty.MapFaction;
            float allyRadiusSq = model.AlliedRevealRadius * model.AlliedRevealRadius;
            var now = CampaignTime.Now;
            for (int i = _ambushRecords.Count - 1; i >= 0; i--)
            {
                var rec = _ambushRecords[i];
                if (!(rec.AmbusherRepelled && rec.Ambusher == party)) continue;
                if ((now - rec.At).ToHours >= model.GetAmbushCooldownHours(rec)) continue;

                // Player was the one being ambushed.
                if (rec.Victim == mainParty) return true;

                // Ally of the player was ambushed, and the player is close enough
                // to have plausibly witnessed it.
                if (rec.Victim != null
                    && playerFaction != null
                    && rec.Victim.MapFaction == playerFaction
                    && party.IsActive
                    && party.Position.DistanceSquared(mainParty.Position) <= allyRadiusSq)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Weekly sweep dropping records older than <see cref="MaxRecordAgeHours"/> and orphaned detection entries.
        /// </summary>
        private void OnWeeklyTick()
        {
            var now = CampaignTime.Now;
            _ambushRecords.RemoveAll(r => (now - r.At).ToHours >= MaxRecordAgeHours);
            if (_playerDetectedParties.Count > 0)
            {
                var stale = _playerDetectedParties
                    .Where(kv => kv.Key == null || !kv.Key.IsActive)
                    .Select(kv => kv.Key)
                    .ToList();
                foreach (var p in stale) _playerDetectedParties.Remove(p);
            }
        }

        /// <summary>
        /// Clears stealth/prep state on save load and precomputes troop scores.
        /// </summary>
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            AmbushTroopStealthScore.PrecomputeAll();
            AmbushTroopSpottingScore.PrecomputeAll();

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
            }

            if (_stealthedParties.Count == 0) return;
            var snapshot = new List<MobileParty>(_stealthedParties.Keys);
            foreach (var party in snapshot)
            {
                if (party != null && party.IsActive) LeaveAiStealth(party);
                else _stealthedParties.Remove(party);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_stealthedParties", ref _stealthedParties);
            dataStore.SyncData("_ambushRecords", ref _ambushRecords);
            dataStore.SyncData("_preppingPartiesStartedAt", ref _preppingPartiesStartedAt);
            dataStore.SyncData("_playerDetectedParties", ref _playerDetectedParties);
        }

        /// <summary>
        /// True while the party is in AI stealth stance.
        /// </summary>
        public bool IsPartyStealthed(MobileParty party)
            => party != null && _stealthedParties.ContainsKey(party);

        /// <summary>
        /// Game-time at which the party entered stealth, or zero if not stealthed.
        /// </summary>
        public CampaignTime GetStealthEnteredAt(MobileParty party)
            => (party != null && _stealthedParties.TryGetValue(party, out var at))
                ? at : CampaignTime.Zero;

        /// <summary>
        /// Enumerate stealthed AI parties within radius of the given center party.
        /// </summary>
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

        /// <summary>
        /// Break stealth via a player-detection event.
        /// </summary>
        public void BreakStealthByDetection(MobileParty party)
        {
            if (party == null || !_stealthedParties.ContainsKey(party)) return;
            LeaveAiStealth(party);
        }

        /// <summary>
        /// Enumerate currently-prepping AI parties.
        /// </summary>
        public IEnumerable<MobileParty> PreppingParties => _preppingPartiesStartedAt.Keys;

        /// <summary>
        /// Timestamp a player-detection event on the party. Resets the reveal window.
        /// Also clears the bluff so troop counts become visible, since detection
        /// can fire before the stealth-exit path has a chance to.
        /// </summary>
        public void RecordPlayerDetection(MobileParty party)
        {
            if (party == null) return;
            _playerDetectedParties[party] = CampaignTime.Now;
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.EndBluffOn(party);
        }

        /// <summary>
        /// True if the party was spotted by the player within the reveal window.
        /// </summary>
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

        /// <summary>
        /// Count stealthed same-faction parties within radius.
        /// </summary>
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

        private bool IsOnCooldown(MobileParty party) => IsOnAmbushCooldown(party);

        /// <summary>
        /// True if any hostile could intercept the party during the prep window at its current speed and heading.
        /// </summary>
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
                // Convert map-u/hour to u/minute for prep math.
                float speedPerMinute = p.Speed / 60f;
                float interceptDistance = prepMinutes * speedPerMinute;
                if (dist > interceptDistance) continue;

                // Skip pursuers heading away. Idle parties (no target) fall through.
                var pPos = p.Position.ToVec2();
                var toSelf = (selfPos - pPos).Normalized();
                var headingRaw = p.TargetPosition.ToVec2() - pPos;
                if (headingRaw.LengthSquared > 0.01f)
                {
                    var heading = headingRaw.Normalized();
                    float dot = heading.x * toSelf.x + heading.y * toSelf.y;
                    if (dot < 0f) continue;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Main AI tick. Prunes stale entries, advances prep, decides new stealth entries, then runs target detection.
        /// </summary>
        private void OnQuarterHourlyTick()
        {
            if (Campaign.Current == null) return;
            PruneStealthedParties();
            TickPrepping();
            TickDecisions();
            TickTargetDetection();
        }

        /// <summary>
        /// Scan eligible AI parties and start stealth prep on those that pass the entry roll.
        /// </summary>
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

                // Strikers need a viable target; hiders stealth from any nearby threat.
                var nearbyHostiles = CollectNearbyHostiles(party, model.AiOpportunityScanRadius);
                if (nearbyHostiles.Count == 0) continue;
                if (!DontStrikeAIParties(party))
                {
                    var bestTarget = FindBestEntryCandidate(party, nearbyHostiles, model, out float bestChance);
                    if (bestTarget == null || bestChance < model.AiMinEntryViabilityChance) continue;
                }

                // Propensity gate.
                if (MBRandom.RandomFloat >= propensity * model.AiDecisionTickFraction) continue;

                // Entry chance with observer penalty.
                var observers = CollectNearbyHostiles(party, model.DetectionScanRadius);
                float entryChance = model.CalculateEntryChance(party, observers);
                // Cluster penalty from nearby stealthed friendlies; floor at the model minimum.
                int clusterCount = CountStealthedFriendliesInRadius(party, model.FriendlyStealthClusterRadius);
                entryChance -= model.FriendlyStealthClusterPenalty(clusterCount);
                if (entryChance < model.MinEntryChance) entryChance = model.MinEntryChance;
                if (MBRandom.RandomFloat >= entryChance) continue;

                // Reject entry if a pursuer would overrun the position before setup completes.
                float prepMinutes = model.CalculatePreparationMinutes(party);
                if (WouldPursuerInterceptBeforePrepCompletes(party, prepMinutes, model))
                    continue;

                StartAiStealthPrep(party, prepMinutes);
            }
        }

        /// <summary>
        /// Pick the candidate with the highest predicted success chance, or null if none are plausible.
        /// </summary>
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
                // Pass current time since stealth hasn't been entered yet.
                float chance = model.CalculateAmbushSuccessChance(ambusher, cand, CampaignTime.Now);
                if (chance > bestChance)
                {
                    bestChance = chance;
                    best = cand;
                }
            }
            return best;
        }

        /// <summary>
        /// For each stealthed ambusher, strike the player if in range, else attempt an AI-vs-AI skirmish.
        /// </summary>
        private void TickTargetDetection()
        {
            if (_stealthedParties.Count == 0) return;

            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;
            bool playerAvailable = IsPlayerAvailableAsTarget();
            var main = MobileParty.MainParty;
            Vec2 mainPos = main != null ? main.Position.ToVec2() : Vec2.Zero;

            // Snapshot before triggers mutate the dict.
            var snapshot = new List<MobileParty>(_stealthedParties.Keys);
            int skirmishesFired = 0;
            foreach (var ambusher in snapshot)
            {
                if (ambusher == null || !ambusher.IsActive) continue;
                if (ambusher.MapFaction == null) continue;
                // Drop out of stealth if vanilla dragged them into a map event.
                if (ambusher.MapEvent != null)
                {
                    LeaveAiStealth(ambusher);
                    continue;
                }
                // Same for settlement entry between prune and detection.
                if (ambusher.CurrentSettlement != null)
                {
                    LeaveAiStealth(ambusher);
                    continue;
                }
                // Wounded below the ambush threshold.
                if (ambusher.MemberRoster == null
                    || ambusher.MemberRoster.TotalHealthyCount < MinHealthyForAmbush)
                {
                    LeaveAiStealth(ambusher);
                    continue;
                }

                // Strike range scales with the ambusher's Scouting.
                float strikeRadius = model.CalculateStrikeRange(ambusher);
                float strikeRadiusSq = strikeRadius * strikeRadius;

                // Priority 1: player in strike range, honouring vanilla's DoNotAttackMainPartyUntil.
                if (playerAvailable
                    && FactionManager.IsAtWarAgainstFaction(ambusher.MapFaction, main.MapFaction)
                    && ambusher.Ai.DoNotAttackMainPartyUntil.IsPast
                    && ambusher.Position.ToVec2().DistanceSquared(mainPos) <= strikeRadiusSq)
                {
                    TriggerAmbushAgainstPlayer(ambusher);
                    // PlayerEncounter is now occupied; one player ambush per tick.
                    playerAvailable = false;
                    continue;
                }

                // Priority 2: closest AI victim, strikers only, capped per tick.
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

        /// <summary>
        /// True when the player is in a map state where an AI ambush can plausibly open on them.
        /// </summary>
        private static bool IsPlayerAvailableAsTarget()
        {
            var main = MobileParty.MainParty;
            if (main == null || !main.IsActive) return false;
            if (main.CurrentSettlement != null) return false;
            if (MapEvent.PlayerMapEvent != null) return false;
            if (TaleWorlds.MountAndBlade.Mission.Current != null) return false;
            if (main.MapFaction == null) return false;

            // Only ambush moving parties.
            if (!main.IsMoving) return false;

            // Skip if the player is already in stealth; stacking menus races the menu stack.
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior != null && behavior.IsPlayerInStealth) return false;

            if (behavior != null && behavior.IsOnAmbushCooldown) return false;

            // Only when the player is on the map screen; menu stacking under modal UI can freeze the engine.
            if (!(Game.Current?.GameStateManager?.ActiveState is MapState)) return false;

            return true;
        }

        /// <summary>
        /// Return the nearest eligible hostile AI lord party inside the ambusher's strike radius.
        /// </summary>
        private MobileParty FindClosestHostileAiParty(MobileParty ambusher, float strikeRadius)
        {
            float strikeRadiusSq = strikeRadius * strikeRadius;
            var ambusherPos = ambusher.Position.ToVec2();
            MobileParty closest = null;
            float closestDistSq = strikeRadiusSq;
            // LocatorGrid radius query, cost proportional to local density.
            var query = MobileParty.StartFindingLocatablesAroundPosition(ambusherPos, strikeRadius);
            for (var p = MobileParty.FindNextLocatable(ref query); p != null;
                 p = MobileParty.FindNextLocatable(ref query))
            {
                if (p == null || !p.IsActive || p == ambusher) continue;
                if (p == MobileParty.MainParty) continue;
                if (p.CurrentSettlement != null) continue;
                if (p.MapEvent != null) continue;
                if (p.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(ambusher.MapFaction, p.MapFaction)) continue;
                // Lord parties only; caravans / villagers / militia keep vanilla engagement.
                if (!p.IsLordParty) continue;
                if (p.MemberRoster == null || p.MemberRoster.TotalHealthyCount < MinHealthyForAmbush) continue;
                // Cooldown applies to ambusher and victim roles.
                if (IsOnCooldown(p)) continue;
                float distSq = p.Position.ToVec2().DistanceSquared(ambusherPos);
                if (distSq > closestDistSq) continue;
                closest = p;
                closestDistSq = distSq;
            }
            return closest;
        }

        /// <summary>
        /// Resolves a headless AI-vs-AI ambush: rolls tier, wounds troops, optionally opens a field battle.
        /// </summary>
        private void TriggerSkirmishAgainstAiParty(MobileParty ambusher, MobileParty victim)
        {
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            // Roll outcome tier from the chance curve.
            float chance = model.CalculateAmbushSuccessChance(ambusher, victim, CampaignTime.Now);
            var tier = model.RollOutcomeTier(chance, MBRandom.RandomFloat);

            var (ambusherWounds, victimWounds) = model.CalculateAiSkirmishCasualties(ambusher, victim, tier);
            WoundTroops(ambusher.Party, ambusherWounds);
            WoundTroops(victim.Party, victimWounds);

            bool press = model.ShouldPressAttackAfterSkirmish(ambusher, victim);
            LeaveAiStealth(ambusher);

            // TerribleFail flips the resolution to AmbusherDefeated.
            bool victimWonExchange = tier == AmbushOutcomeTier.TerribleFail;
            var resolution = victimWonExchange
                ? AmbushResolution.AmbusherDefeated
                : AmbushResolution.AmbusherTotalVictory;
            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now, ambusher, victim, resolution));

            // Open a field battle only if the player can plausibly reach it.
            // Distant press-attacks resolve silently through the casualties above.
            if (press && ambusher.MapEvent == null && victim.MapEvent == null
                && IsWithinPlayerProximity(ambusher, model))
            {
                ambusher.Position = victim.Position;
                FieldBattleEventComponent.CreateFieldBattleEvent(ambusher.Party, victim.Party);
            }
        }

        /// <summary>True when the party is within the player-proximity radius for
        /// visible map events. Falls through to false if the main party is unavailable.</summary>
        private static bool IsWithinPlayerProximity(MobileParty party, TORAmbushModel model)
        {
            var main = MobileParty.MainParty;
            if (main == null || !main.IsActive || party == null) return false;
            float r = model.AlliedRevealRadius;
            return party.Position.ToVec2().DistanceSquared(main.Position.ToVec2()) <= r * r;
        }

        /// <summary>
        /// Return the leader hero's name if present, otherwise the party name.
        /// </summary>
        private static string PartyLabel(MobileParty p)
        {
            if (p == null) return "?";
            if (p.LeaderHero != null) return p.LeaderHero.Name.ToString();
            return p.Name?.ToString() ?? "?";
        }

        /// <summary>
        /// Return the party's map-faction culture id for logging.
        /// </summary>
        private static string CultureLabel(MobileParty p)
        {
            return p?.MapFaction?.Culture?.StringId ?? "?";
        }

        /// <summary>
        /// Wound a given number of non-hero troops in the party roster.
        /// </summary>
        private static void WoundTroops(PartyBase party, int count)
        {
            if (party?.MemberRoster == null || count <= 0) return;
            party.MemberRoster.WoundNumberOfNonHeroTroopsRandomly(count);
        }

        /// <summary>
        /// Open the ambush mission against the player.
        /// </summary>
        private void TriggerAmbushAgainstPlayer(MobileParty ambusher)
        {
            // Teleport AI onto the player.
            ambusher.Position = MobileParty.MainParty.Position;

            CurrentAmbusherOnPlayer = ambusher;

            PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, ambusher.Party, false);
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            // Hide troop counts across the whole ambusher side. Needs MapEvent.Parties populated.
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.BeginBluffOnAmbusherSide(ambusher);
            GameMenu.ActivateGameMenu(AmbushCampaignBehavior.AmbushedIntroMenuId);
            TORCommon.Say("Ambush! " + (ambusher.LeaderHero?.Name?.ToString()
                                        ?? ambusher.Name?.ToString()
                                        ?? "A hostile force")
                          + " was lying in wait.");
        }

        /// <summary>
        /// Start the preparation window. Party holds position and ignored by AI, not yet invisible.
        /// </summary>
        private void StartAiStealthPrep(MobileParty party, float totalMinutes)
        {
            _preppingPartiesStartedAt[party] = CampaignTime.Now;
            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();
            // Cap ignore window at max stealth hold so the party unfreezes if prep misses.
            party.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(MaxStealthHoldHours));
            // Bluff troop counts during prep.
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.BeginBluffOn(party);
        }

        /// <summary>
        /// Promote a prep-complete party into full stealth and vanish from the map.
        /// </summary>
        private void FinalizeAiStealth(MobileParty party)
        {
            _preppingPartiesStartedAt.Remove(party);
            _stealthedParties[party] = CampaignTime.Now;
            party.IsVisible = false;
        }

        /// <summary>
        /// Promote prep-complete parties into full stealth.
        /// </summary>
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

        /// <summary>
        /// Exit stealth. Records the event, clears the bluff, re-enables AI, and un-ignores the party.
        /// </summary>
        private void LeaveAiStealth(MobileParty party)
        {
            if (party == null) return;
            _stealthedParties.Remove(party);
            // Write a stealth-exit record so cooldown queries cover solo exits.
            RecordAmbush(new AmbushRecord(CampaignTime.Now, party, null,
                                          AmbushResolution.None));
            // Clear the bluff.
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.EndBluffOn(party);
            if (party.IsActive)
            {
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.IgnoreByOtherPartiesTill(CampaignTime.Zero);
                // Only flip IsVisible to true for parties within vanilla's
                // LocatorGrid reach. Vanilla's distance pass only iterates
                // parties near the main party, so a far stealth-exit that
                // writes true here would stay stuck visible forever on the
                // player's map. For far parties, keep them invisible and let
                // vanilla pick them up when the player approaches.
                var mp = MobileParty.MainParty;
                if (mp != null)
                {
                    float r = mp.SeeingRange * 2f;
                    if (party.Position.DistanceSquared(mp.Position) <= r * r)
                        party.IsVisible = true;
                }
            }
        }

        /// <summary>
        /// Drop expired, in-settlement, or in-event parties from the AI stealth set.
        /// </summary>
        private void PruneStealthedParties()
        {
            if (_stealthedParties.Count == 0) return;
            var now = CampaignTime.Now;
            // Collect first, apply after; LeaveAiStealth mutates the dict.
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
        }

        /// <summary>
        /// Clear stealth, prep, detection, and bluff references for a party being removed from play.
        /// </summary>
        private void OnPartyRemoved(PartyBase party)
        {
            if (party?.MobileParty == null) return;
            _stealthedParties.Remove(party.MobileParty);
            _preppingPartiesStartedAt.Remove(party.MobileParty);
            _playerDetectedParties.Remove(party.MobileParty);
            // Bluff cleanup safety net.
            Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>()
                ?.EndBluffOn(party.MobileParty);
            // Null references in records; keep the record for history.
            foreach (var rec in _ambushRecords)
            {
                if (rec.Ambusher == party.MobileParty) rec.Ambusher = null;
                if (rec.Victim == party.MobileParty) rec.Victim = null;
            }
            if (CurrentAmbusherOnPlayer == party.MobileParty)
                CurrentAmbusherOnPlayer = null;
        }

        /// <summary>
        /// Gates which parties may enter stealth. Excludes main hero, non-combatants, in-settlement, in-encounter, small parties.
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

            // Army followers follow the army; only lone lords and army leaders qualify.
            if (party.Army != null && party.Army.LeaderParty != party) return false;

            // Stealth suspends all vanilla AI; only lords without a committed goal qualify. Bandits bypass this gate.
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
        /// True for bandit parties. Bandits hide from AI but can still ambush the player.
        /// </summary>
        private static bool DontStrikeAIParties(MobileParty party) => party != null && party.IsBandit;

        /// <summary>
        /// Return hostile parties within scanRadius of the given party.
        /// </summary>
        private static List<MobileParty> CollectNearbyHostiles(MobileParty self, float scanRadius)
        {
            var result = new List<MobileParty>();
            if (self == null) return result;
            var selfPos = self.Position.ToVec2();
            var selfFaction = self.MapFaction;
            if (selfFaction == null) return result;
            float radiusSq = scanRadius * scanRadius;
            // LocatorGrid returns a cell-superset; tighten with a distance check.
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
