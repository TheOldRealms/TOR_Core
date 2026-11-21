using System.Collections.Generic;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;
using TOR_Core.CampaignMechanics.Ambush;
using TOR_Core.Utilities;

namespace TOR_Core.Missions.AmbushMission
{
    /// <summary>
    /// Mission logic for the victim-side ambush (player is ambushed by an AI
    /// party). Owns the 4-outcome priority table, the soft-border countdown
    /// that drives the Fled outcome, and the roster snapshot used by the
    /// post-mission wounded/remove flows. Role-specific SetOutcome flags come
    /// from <see cref="AmbushMissionLogicBase.WriteRoleFlags"/>.
    /// </summary>
    /// <remarks>
    /// Outcome priority: hero inactive, defender wiped, attacker wiped, player across soft border for countdown seconds.
    /// </remarks>
    public class PlayerAmbushedMissionLogic : AmbushMissionLogicBase
    {
        private float _outsideBoundarySeconds;

        // Non-hero roster snapshot captured at mission start; drives the wound/remove flows on flee.
        private Dictionary<CharacterObject, int> _ambushedRosterSnapshot;

        protected override string LogTag => "[PlayerAmbushedMission]";

        protected override void OnAfterStarted()
        {
            StartObjective();

            _ambushedRosterSnapshot = new Dictionary<CharacterObject, int>();
            var roster = MobileParty.MainParty?.MemberRoster;
            if (roster != null)
            {
                for (int i = 0; i < roster.Count; i++)
                {
                    var c = roster.GetCharacterAtIndex(i);
                    if (c == null || c.IsHero) continue;
                    int healthy = roster.GetElementNumber(i) - roster.GetElementWoundedNumber(i);
                    if (healthy <= 0) continue;
                    _ambushedRosterSnapshot[c] = healthy;
                }
            }
            TORCommon.Log(LogLevel.Info, LogTag + " AfterStart: committed roster snapshot, "
                + _ambushedRosterSnapshot.Count + " non-hero troop types captured");
        }

        protected override void OnMissionTickReady(float dt)
        {
            var mainAgent = Mission.MainAgent;

            // Priority 1: hero down.
            if (mainAgent != null && !mainAgent.IsActive())
            {
                SetOutcome(AmbushResolution.VictimDefeated, successful: false,
                    leaderDown: true,
                    reason: "hero inactive");
                return;
            }

            // Priority 2: defender (player) wiped; also wait for the reinforcement queue to drain.
            if (CountActiveHumans(Mission.DefenderTeam) == 0
                && !AreReinforcementsStillPending())
            {
                SetOutcome(AmbushResolution.VictimDefeated, successful: false,
                    reason: "defender (player) wiped + reinforcement queue drained");
                return;
            }

            // Priority 3: AI ambusher wiped. Active-humans count is authoritative; no IsSideDepleted gate.
            if (CountActiveHumans(Mission.AttackerTeam) == 0)
            {
                SetOutcome(AmbushResolution.AmbusherDefeated, successful: true,
                    reason: "attacker (AI) wiped");
                return;
            }

            // Priority 4: soft border crossed. Accumulate when hero is outside, reset on re-entry.
            if (mainAgent != null)
            {
                if (!Mission.IsPositionInsideBoundaries(mainAgent.Position.AsVec2))
                    _outsideBoundarySeconds += dt;
                else
                    _outsideBoundarySeconds = 0f;

                if (_outsideBoundarySeconds >= _model.AmbushedFledCountdownSeconds)
                {
                    SetOutcome(AmbushResolution.VictimFled, successful: true,
                        reason: "soft border countdown elapsed ("
                                + _model.AmbushedFledCountdownSeconds + "s)");
                    return;
                }
            }
        }

        protected override void WriteRoleFlags(AmbushCampaignBehavior behavior, bool leaderDown)
        {
            behavior.PlayerWasAmbusher = false;
            behavior.LastAmbusherLeaderDown = false;
            behavior.LastVictimLeaderDown = leaderDown;
        }

        /// <summary>
        /// Starts the single "survive the ambush" objective. No live enemy count, preserves the bluff.
        /// </summary>
        private void StartObjective()
        {
            if (_objectiveLogic == null) return;
            _objective = new AmbushObjective(
                Mission, "tor_ambushed",
                new TextObject("{=tor_ambushed_obj_name}Survive the Ambush"),
                new TextObject("{=tor_ambushed_obj_desc_init}Defeat the ambushers or flee past the mission boundary."));
            _objective.SetProgressCallback(() => default);
            _objectiveLogic.StartObjective(_objective);
        }

        /// <summary>
        /// Only AmbusherDefeated nudges the vanilla victory flow; other resolutions leave PlayerEncounter alive.
        /// </summary>
        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
            TORCommon.Log(LogLevel.Info, LogTag + " OnMissionResultReady fired. missionResult="
                + (missionResult != null ? "ok" : "null"));
            if (missionResult == null) return;
            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null) return;
            var resolution = behavior.LastResolution;
            TORCommon.Log(LogLevel.Info, LogTag + " OnMissionResultReady resolution=" + resolution);

            // Victim won: nudge vanilla victory flow; OnPlayerBattleEnd dispatches the record.
            if (resolution == AmbushResolution.AmbusherDefeated)
            {
                if (PlayerEncounter.Current != null)
                    PlayerEncounter.SetPlayerVictorious();
                behavior.PendingPostAmbushCleanup = true;
                return;
            }
            // VictimFled / hero-down cleanup runs from OnEndMission; OnMissionResultReady doesn't fire on retreat paths.
        }

        /// <summary>
        /// Teardown callback that always fires. Runs the Fled/HeroWounded cleanup and dispatches the record.
        /// </summary>
        protected override void OnEndMission()
        {
            base.OnEndMission();
            if (!_outcomeDecided) return;

            var behavior = Campaign.Current?.GetCampaignBehavior<AmbushCampaignBehavior>();
            if (behavior == null) return;
            var resolution = behavior.LastResolution;
            bool heroDownVictimDefeated = resolution == AmbushResolution.VictimDefeated
                                           && behavior.LastVictimLeaderDown;
            if (resolution != AmbushResolution.VictimFled
                && !heroDownVictimDefeated) return;

            // Capture ambusher ref before Finish; party destruction clears CurrentAmbusherOnPlayer.
            var aiBehavior = Campaign.Current?.GetCampaignBehavior<AiAmbushBehavior>();
            var ambusher = aiBehavior?.CurrentAmbusherOnPlayer;

            TORCommon.Log(LogLevel.Info, LogTag + " OnEndMission stash+finish: resolution="
                + resolution
                + " committed-types=" + (_ambushedRosterSnapshot?.Count ?? -1)
                + " ambusher=" + (ambusher?.LeaderHero?.Name?.ToString()
                                   ?? ambusher?.Name?.ToString() ?? "?")
                + " PlayerEncounter.Current="
                + (PlayerEncounter.Current != null ? "ALIVE" : "null"));

            behavior.StashAmbushedRosterSnapshot(_ambushedRosterSnapshot);

            // Dispatch the resolution with MainParty as victim; fires before Finish so the pair incident is captured.
            TORCampaignEvents.Instance.OnAmbushResolved(
                new AmbushRecord(CampaignTime.Now,
                                  (ambusher != null && ambusher.IsActive) ? ambusher : null,
                                  MobileParty.MainParty,
                                  resolution,
                                  wasLoud: false,
                                  ambusherLeaderDown: false,
                                  victimLeaderDown: behavior.LastVictimLeaderDown));

            if (PlayerEncounter.Current != null)
                PlayerEncounter.Finish(forcePlayerOutFromSettlement: false);
        }
    }
}
