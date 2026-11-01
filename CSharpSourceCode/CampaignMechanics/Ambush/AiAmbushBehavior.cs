using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
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
        // Serialized so stealth survives save/load.
        private Dictionary<MBGUID, CampaignTime> _stealthedParties
            = new Dictionary<MBGUID, CampaignTime>();

        // Parties on ambush cooldown → when the cooldown expires. Mirror
        // of the player-side cooldown; stamped whenever a party leaves
        // stealth (burned the surprise, visibility compromised) and read
        // by IsEligibleAmbusher to block immediate re-stealth.
        private Dictionary<MBGUID, CampaignTime> _partyCooldownUntil
            = new Dictionary<MBGUID, CampaignTime>();

        // How long a stealthed AI party stays hidden before giving up and
        // leaving stealth on its own (no viable target walked past). Keeps
        // the stealth pool from accumulating indefinitely.
        private const float MaxStealthHoldHours = 48f;

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

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, OnQuarterHourlyTick);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemoved);
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

            if (_stealthedParties.Count == 0) return;
            var snapshot = new List<MBGUID>(_stealthedParties.Keys);
            foreach (var id in snapshot)
            {
                var party = FindPartyById(id);
                if (party != null) LeaveAiStealth(party);
                else _stealthedParties.Remove(id);
            }
            TORCommon.Log(LogLevel.Info, "[AI Ambush] OnSessionLaunched: cleared "
                + snapshot.Count + " stealthed parties from loaded save");
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_stealthedParties", ref _stealthedParties);
            dataStore.SyncData("_partyCooldownUntil", ref _partyCooldownUntil);
        }

        /// <summary>True while <paramref name="party"/> is in AI stealth
        /// stance. Readers (future Slice 3 reveal, Slice 2b auto-resolve
        /// bonus) query via this; nothing else should poke the dict.</summary>
        public bool IsPartyStealthed(MobileParty party)
            => party != null && _stealthedParties.ContainsKey(party.Id);

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
            foreach (var id in _stealthedParties.Keys)
            {
                var other = FindPartyById(id);
                if (other == null || other == party) continue;
                if (other.MapFaction != faction) continue;
                if (other.Position.ToVec2().DistanceSquared(selfPos) > radiusSq) continue;
                n++;
            }
            return n;
        }

        private bool IsOnCooldown(MobileParty party)
        {
            if (party == null) return false;
            if (!_partyCooldownUntil.TryGetValue(party.Id, out var until)) return false;
            if (until.IsFuture) return true;
            // Expired — tidy up so the dict doesn't accumulate forever.
            _partyCooldownUntil.Remove(party.Id);
            return false;
        }

        private void OnQuarterHourlyTick()
        {
            if (Campaign.Current == null) return;

            TORCommon.Log(LogLevel.Info, "[AI Ambush] tick: start, stealthed=" + _stealthedParties.Count);
            PruneStealthedParties();
            TORCommon.Log(LogLevel.Info, "[AI Ambush] tick: pruned, stealthed=" + _stealthedParties.Count);
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
                if (_stealthedParties.ContainsKey(party.Id)) continue;
                if (IsOnCooldown(party)) continue;

                float propensity = model.GetAmbushPropensity(party);
                if (propensity <= 0f) continue;

                // Reactive-stealth gate: only enter stealth if there's a
                // viable target nearby RIGHT NOW. Scans a wider "opportunity"
                // radius than the strike radius so the party stealths just
                // before the target walks into range. If no hostile is in
                // sight OR the best candidate's predicted ambush success is
                // too low, don't bother hiding — the party would just sit
                // forever with no one to ambush.
                var nearbyHostiles = CollectNearbyHostiles(party, model.AiOpportunityScanRadius);
                if (nearbyHostiles.Count == 0) continue;
                var bestTarget = FindBestEntryCandidate(party, nearbyHostiles, model, out float bestChance);
                if (bestTarget == null || bestChance < model.AiMinEntryViabilityChance) continue;

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

                EnterAiStealth(party);
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

            float strikeRadius = AmbushCampaignBehavior.StealthRadius;
            float strikeRadiusSq = strikeRadius * strikeRadius;
            bool playerAvailable = IsPlayerAvailableAsTarget();
            var main = MobileParty.MainParty;
            Vec2 mainPos = main != null ? main.Position.ToVec2() : Vec2.Zero;

            // Grab a snapshot — triggering an ambush mutates the dict.
            var snapshot = new List<MBGUID>(_stealthedParties.Keys);
            int skirmishesFired = 0;
            foreach (var id in snapshot)
            {
                var ambusher = FindPartyById(id);
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
                // Post-skirmish-wounding may have dropped the ambusher
                // below the fighting threshold since the last tick.
                if (ambusher.MemberRoster == null
                    || ambusher.MemberRoster.TotalHealthyCount < MinHealthyForAmbush)
                {
                    LeaveAiStealth(ambusher);
                    continue;
                }

                // Priority 1: player in strike range. Fiction: the player
                // always counts as a notable target, so a stealthed party
                // springs on them when they walk past.
                if (playerAvailable
                    && FactionManager.IsAtWarAgainstFaction(ambusher.MapFaction, main.MapFaction)
                    && ambusher.Position.ToVec2().DistanceSquared(mainPos) <= strikeRadiusSq)
                {
                    TriggerAmbushAgainstPlayer(ambusher);
                    // One player-ambush per tick: PlayerEncounter is now
                    // occupied, no other stealthed party can trigger.
                    playerAvailable = false;
                    continue;
                }

                // Priority 2: closest AI victim. Multiple skirmishes CAN
                // fire in a tick but we cap them — each one touches
                // vanilla's engagement pipeline (CreateFieldBattleEvent +
                // roster mutation), and stacking too many at once can
                // hang the campaign-map tick.
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
            return true;
        }

        private static MobileParty FindClosestHostileAiParty(MobileParty ambusher, float strikeRadius)
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

        private static MobileParty FindPartyById(MBGUID id)
        {
            // MBObjectManager keys by (type, GUID) with O(1) lookup inside
            // each type bucket. Beats the previous MobileParty.All linear
            // scan by a wide margin when the stealth dict grows.
            return MBObjectManager.Instance.GetObject(id) as MobileParty;
        }

        private void TriggerAmbushAgainstPlayer(MobileParty ambusher)
        {
            // Teleport AI onto the player — matches the Phase F fiction
            // (lying in wait, springs on the player) and the debug trigger's
            // placement.
            ambusher.Position = MobileParty.MainParty.Position;

            // Leave stealth before the encounter opens so the stealth dict
            // doesn't hold a reference to a party now mid-combat.
            LeaveAiStealth(ambusher);

            PlayerEncounter.RestartPlayerEncounter(PartyBase.MainParty, ambusher.Party, false);
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            GameMenu.ActivateGameMenu(AmbushCampaignBehavior.AmbushedIntroMenuId);
            TORCommon.Say("Ambush! " + (ambusher.LeaderHero?.Name?.ToString()
                                        ?? ambusher.Name?.ToString()
                                        ?? "A hostile force")
                          + " was lying in wait.");
            TORCommon.Log(LogLevel.Info, "[AI Ambush] player ambush @ "
                + MobileParty.MainParty.Position.ToVec2()
                + " ambusher=" + PartyLabel(ambusher));
        }

        private void EnterAiStealth(MobileParty party)
        {
            _stealthedParties[party.Id] = CampaignTime.Now;
            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();
            // Cap the ignore window at the max stealth hold (48h). If
            // LeaveAiStealth ever misses, the party clears on its own
            // instead of being permanently invisible to other AI
            // (previous +100 years was a time bomb for stuck parties).
            party.IgnoreByOtherPartiesTill(CampaignTime.HoursFromNow(MaxStealthHoldHours));
            // Actually vanish from the campaign map. This is the other
            // side of the ambush model — the stealth value isn't just
            // "AI leaves them alone", it's "nobody sees them". Lets
            // weaker parties lurk until a bigger threat passes.
            party.IsVisible = false;
            TORCommon.Log(LogLevel.Info, "[AI Ambush] " + PartyLabel(party)
                + " (" + CultureLabel(party) + ") entered stealth stance at "
                + party.Position.ToVec2());
        }

        private void LeaveAiStealth(MobileParty party)
        {
            if (party == null) return;
            _stealthedParties.Remove(party.Id);
            // Stamp cooldown unconditionally — covers burned strikes
            // (player ambush / skirmish), vanilla-engagement drag-in, and
            // the max-hold-expired "gave up" case. All three share the
            // "position revealed / recently stealthed" semantics that
            // justify a cooldown before the party tries again.
            _partyCooldownUntil[party.Id] =
                CampaignTime.HoursFromNow(AmbushCampaignBehavior.CooldownHours);
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
            var expired = new List<MBGUID>();
            var toLeave = new List<MobileParty>();
            foreach (var kv in _stealthedParties)
            {
                var party = FindPartyById(kv.Key);
                if (party == null || !party.IsActive)
                {
                    expired.Add(kv.Key);
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
            foreach (var id in expired) _stealthedParties.Remove(id);
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
            _stealthedParties.Remove(party.MobileParty.Id);
        }

        private static bool IsEligibleAmbusher(MobileParty party)
        {
            if (party == null || !party.IsActive) return false;
            if (party == MobileParty.MainParty) return false;
            if (party.LeaderHero == Hero.MainHero) return false;
            if (party.CurrentSettlement != null) return false;
            if (party.MapEvent != null) return false;
            // Only lord parties ambush. Bandit parties get excluded for
            // now — base Bannerlord spawns dozens of them continuously and
            // enrolling the whole bandit population into the stealth pool
            // was swamping the quarter-hourly tick (freezes). Bandits
            // remain valid *victims* in the AI-vs-AI skirmish check (that
            // filter is in FindClosestHostileAiParty).
            if (!party.IsLordParty) return false;
            if (party.MemberRoster == null) return false;
            if (party.MemberRoster.TotalHealthyCount < MinHealthyForAmbush) return false;
            return true;
        }

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
