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
using TOR_Core.Extensions;
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
            CampaignEvents.QuarterHourlyTickEvent.AddNonSerializedListener(this, OnQuarterHourlyTick);
            CampaignEvents.OnPartyRemovedEvent.AddNonSerializedListener(this, OnPartyRemoved);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_stealthedParties", ref _stealthedParties);
        }

        /// <summary>True while <paramref name="party"/> is in AI stealth
        /// stance. Readers (future Slice 3 reveal, Slice 2b auto-resolve
        /// bonus) query via this; nothing else should poke the dict.</summary>
        public bool IsPartyStealthed(MobileParty party)
            => party != null && _stealthedParties.ContainsKey(party.Id);

        private void OnQuarterHourlyTick()
        {
            if (Campaign.Current == null) return;

            TaleWorlds.Library.Debug.Print("[AI Ambush] tick: start, stealthed=" + _stealthedParties.Count);
            PruneStealthedParties();
            TaleWorlds.Library.Debug.Print("[AI Ambush] tick: pruned, stealthed=" + _stealthedParties.Count);
            TickDecisions();
            TaleWorlds.Library.Debug.Print("[AI Ambush] tick: decisions done, stealthed=" + _stealthedParties.Count);
            TickTargetDetection();
            TaleWorlds.Library.Debug.Print("[AI Ambush] tick: targets done, stealthed=" + _stealthedParties.Count);
        }

        private void TickDecisions()
        {
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            foreach (var party in MobileParty.All)
            {
                if (!IsEligibleAmbusher(party)) continue;
                if (_stealthedParties.ContainsKey(party.Id)) continue;

                float propensity = model.GetAmbushPropensity(party);
                if (propensity <= 0f) continue;

                // propensity gate first (cheap), then the full entry chance
                // (reads terrain, scouting, night, nearby hostiles).
                if (MBRandom.RandomFloat >= propensity * model.AiDecisionTickFraction) continue;

                int hostileCount = CountNearbyHostiles(party, model.DetectionScanRadius);
                float entryChance = model.CalculateEntryChance(party, hostileCount);
                if (MBRandom.RandomFloat >= entryChance) continue;

                EnterAiStealth(party);
            }
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
                var aiVictim = FindClosestHostileAiParty(ambusher, strikeRadiusSq);
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

        private static MobileParty FindClosestHostileAiParty(MobileParty ambusher, float strikeRadiusSq)
        {
            var ambusherPos = ambusher.Position.ToVec2();
            MobileParty closest = null;
            float closestDistSq = strikeRadiusSq;
            foreach (var p in MobileParty.All)
            {
                if (p == null || !p.IsActive || p == ambusher) continue;
                if (p == MobileParty.MainParty) continue;    // player handled above
                if (p.CurrentSettlement != null) continue;
                if (p.MapEvent != null) continue;
                if (p.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(ambusher.MapFaction, p.MapFaction)) continue;
                // Only engage proper fighting parties; caravans / villagers
                // use their vanilla engagement paths.
                if (!p.IsLordParty && !p.IsBandit) continue;
                if (p.MemberRoster == null || p.MemberRoster.TotalHealthyCount < MinHealthyForAmbush) continue;
                float distSq = p.Position.ToVec2().DistanceSquared(ambusherPos);
                if (distSq > closestDistSq) continue;
                closest = p;
                closestDistSq = distSq;
            }
            return closest;
        }

        private void TriggerSkirmishAgainstAiParty(MobileParty ambusher, MobileParty victim)
        {
            var model = Campaign.Current?.Models?.GetAmbushModel();
            if (model == null) return;

            var (ambusherWounds, victimWounds) = model.CalculateSkirmishCasualties(ambusher, victim);
            WoundTroops(ambusher.Party, ambusherWounds);
            WoundTroops(victim.Party, victimWounds);

            bool press = model.ShouldPressAttackAfterSkirmish(ambusher, victim);
            LeaveAiStealth(ambusher);

            TORCommon.Say("[AI Ambush] " + PartyLabel(ambusher) + " struck "
                          + PartyLabel(victim) + ": wounded " + victimWounds
                          + " (took " + ambusherWounds + ") — "
                          + (press ? "pressing the attack." : "fading back into cover."));

            // Only open the field battle if neither party has drifted into
            // a map event during this tick (another skirmish, player
            // engagement, etc.). CreateFieldBattleEvent on a party already
            // in a MapEvent hangs vanilla.
            if (press && ambusher.MapEvent == null && victim.MapEvent == null)
            {
                TaleWorlds.Library.Debug.Print("[AI Ambush] press-attack opening field battle: "
                    + PartyLabel(ambusher) + " -> " + PartyLabel(victim));
                // Teleport the ambusher onto the victim and open a vanilla
                // field battle. The vanilla engagement AI + auto-resolve
                // take over from here — nearby allied-party reinforcements,
                // morale, composition all fold in naturally.
                ambusher.Position = victim.Position;
                FieldBattleEventComponent.CreateFieldBattleEvent(ambusher.Party, victim.Party);
                TaleWorlds.Library.Debug.Print("[AI Ambush] press-attack field battle opened");
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
            foreach (var p in MobileParty.All)
                if (p.Id == id) return p;
            return null;
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
        }

        private void EnterAiStealth(MobileParty party)
        {
            _stealthedParties[party.Id] = CampaignTime.Now;
            party.Ai.SetDoNotMakeNewDecisions(true);
            party.SetMoveModeHold();
            party.IgnoreByOtherPartiesTill(CampaignTime.YearsFromNow(100f));
            // Mono-log only — many parties can enter per tick, and spamming
            // the top-left feed with "X entered stealth" was swamping real
            // user-facing messages. The dramatic beats (skirmish fire,
            // player ambush) still go to the feed via TORCommon.Say below.
            TaleWorlds.Library.Debug.Print("[AI Ambush] " + PartyLabel(party)
                + " (" + CultureLabel(party) + ") entered stealth stance.");
        }

        private void LeaveAiStealth(MobileParty party)
        {
            _stealthedParties.Remove(party.Id);
            if (party != null && party.IsActive)
            {
                party.Ai.SetDoNotMakeNewDecisions(false);
                party.IgnoreByOtherPartiesTill(CampaignTime.Zero);
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

        private static int CountNearbyHostiles(MobileParty self, float scanRadius)
        {
            if (self == null) return 0;
            var selfPos = self.Position.ToVec2();
            float radiusSq = scanRadius * scanRadius;
            var selfFaction = self.MapFaction;
            if (selfFaction == null) return 0;
            int n = 0;
            foreach (var p in MobileParty.All)
            {
                if (p == null || !p.IsActive || p == self) continue;
                if (p.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(selfFaction, p.MapFaction)) continue;
                if (p.Position.ToVec2().DistanceSquared(selfPos) > radiusSq) continue;
                n++;
            }
            return n;
        }
    }
}
