using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
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

            // Housekeeping first: parties that got into a map event /
            // settlement / were destroyed shouldn't remain in the stealth
            // dict. Also expire parties that have held stealth too long.
            PruneStealthedParties();

            // Decision tick: every eligible party rolls for entry.
            TickDecisions();

            // Target check: any stealthed party close enough to the player
            // triggers the mission.
            TickTargetDetection();
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
            var main = MobileParty.MainParty;
            if (main == null || !main.IsActive) return;
            if (main.CurrentSettlement != null) return;
            if (MapEvent.PlayerMapEvent != null) return;
            if (TaleWorlds.MountAndBlade.Mission.Current != null) return;

            float strikeRadius = AmbushCampaignBehavior.StealthRadius;
            float strikeRadiusSq = strikeRadius * strikeRadius;
            var mainPos = main.Position.ToVec2();

            // Grab a snapshot — triggering an ambush mutates the dict.
            var snapshot = new List<MBGUID>(_stealthedParties.Keys);
            foreach (var id in snapshot)
            {
                var party = FindPartyById(id);
                if (party == null || !party.IsActive) continue;
                if (party.MapFaction == null || main.MapFaction == null) continue;
                if (!FactionManager.IsAtWarAgainstFaction(party.MapFaction, main.MapFaction)) continue;
                if (party.Position.ToVec2().DistanceSquared(mainPos) > strikeRadiusSq) continue;

                TriggerAmbushAgainstPlayer(party);
                // Only one trigger per tick — the player can't be in two
                // ambushes at once, and PlayerEncounter is now occupied.
                return;
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
            var expired = new List<MBGUID>();
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
                    LeaveAiStealth(party);
                    continue;
                }
                if ((now - kv.Value).ToHours >= MaxStealthHoldHours)
                {
                    LeaveAiStealth(party);
                }
            }
            foreach (var id in expired) _stealthedParties.Remove(id);
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
            if (!party.IsLordParty && !party.IsBandit) return false;
            if (party.MemberRoster == null) return false;
            if (party.MemberRoster.TotalHealthyCount < 5) return false;
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
