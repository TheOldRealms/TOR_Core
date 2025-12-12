using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Diamond;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Missions;

public class BrawlMissionController : MissionLogic, IMissionAgentSpawnLogic
{
    private TORMissionAgentHandler _missionAgentSpawnLogic;
    private TroopRoster _enemyRoster;
    private int _enemyPartySize;
    private readonly Action<BrawlMissionResult> _onMissionEnd;

    public bool PlayerCanLeave { get; set; } = false;

    private Dictionary<Hero, int> _scores;
    private List<Hero> _losses;

    public BrawlMissionController(TroopRoster playerSideTroops, TroopRoster enemyRoster, int enemyPartySize, Action<BrawlMissionResult> onMissionEnd = null)
    {
        base.OnBehaviorInitialize();
        _enemyPartySize = enemyPartySize;
        _enemyRoster = enemyRoster;
        _onMissionEnd = onMissionEnd;
    }

    public override void AfterStart()
    {
        Mission.SetMissionMode(MissionMode.StartUp, true);
        Mission.IsInventoryAccessible = false;
        Mission.IsQuestScreenAccessible = true;
        Mission.DoesMissionRequireCivilianEquipment = false;
        _missionAgentSpawnLogic = Mission.GetMissionBehavior<TORMissionAgentHandler>();
        _missionAgentSpawnLogic.SpawnPlayer(false, true, false, true, false);
        _missionAgentSpawnLogic.SpawnEnemies(_enemyRoster, _enemyPartySize);
        foreach (var agent in Mission.Agents)
        {
            if (agent != Agent.Main && agent.IsHuman)
            {
                agent.SetWatchState(Agent.WatchState.Alarmed);
            }
        }

        _scores = new Dictionary<Hero, int>();
        _losses = new List<Hero>();
    }

    public override InquiryData OnEndMissionRequest(out bool canLeave)
    {
        canLeave = Mission.MissionResult is { BattleResolved: true, PlayerVictory: true } || PlayerCanLeave;
        if (!canLeave) MBInformationManager.AddQuickInformation(new TextObject("{=str_tor_brawl_cannot_leave}You may not leave until finishing the brawl."));
        else
        {
            Mission.Current.EndMission();
        }
        return null;

    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
    {
        if (affectedAgent.Team.Side == BattleSideEnum.Attacker && affectedAgent.IsHero)
        {
            var hero = affectedAgent.GetHero();
            if (hero == null)
            {
                return;
            }
            _losses.Add(hero);
        }
        if (affectedAgent.Team.Side == BattleSideEnum.Defender && affectorAgent.IsHero)
        {
            var hero = affectorAgent.GetHero();
            if (hero == null)
            {
                return;
            }

            if (!_scores.ContainsKey(hero))
            {
                _scores.Add(hero, 1);
            }
            else
            {
                _scores[hero]++;
            }
        }
    }

    public override void OnMissionResultReady(MissionResult missionResult)
    {
        if (missionResult.PlayerDefeated)
        {
            PlayerCanLeave = true;
        }
    }

    public override bool MissionEnded(ref MissionResult missionResult)
    {
        if (Agent.Main == null || !Agent.Main.IsActive())
        {
            missionResult = MissionResult.CreateDefeated(Mission);
            BrawlMissionResult brawlMissionResult = new BrawlMissionResult(missionResult, _scores, _losses);
            _onMissionEnd?.Invoke(brawlMissionResult);
            return true;
        }
        if (Mission.GetMemberCountOfSide(BattleSideEnum.Attacker) == 0)
        {
            missionResult = (Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateDefeated(Mission) : MissionResult.CreateSuccessful(Mission, false);
            BrawlMissionResult brawlMissionResult = new BrawlMissionResult(missionResult, _scores, _losses);
            _onMissionEnd?.Invoke(brawlMissionResult);
            return true;
        }
        if (Mission.GetMemberCountOfSide(BattleSideEnum.Defender) == 0)
        {
            missionResult = (Mission.PlayerTeam.Side == BattleSideEnum.Attacker) ? MissionResult.CreateSuccessful(Mission, false) : MissionResult.CreateDefeated(Mission);
            BrawlMissionResult brawlMissionResult = new BrawlMissionResult(missionResult, _scores, _losses);
            _onMissionEnd?.Invoke(brawlMissionResult);
            return true;
        }
        return false;
    }

    public void StartSpawner(BattleSideEnum side) { }

    public void StopSpawner(BattleSideEnum side) { }

    public bool IsSideSpawnEnabled(BattleSideEnum side)
    {
        return false;
    }

    public bool IsSideDepleted(BattleSideEnum side)
    {
        return true;
    }

    public float GetReinforcementInterval()
    {
        return 1;
    }

    public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side)
    {
        throw new NotImplementedException();
    }

    public bool GetSpawnHorses(BattleSideEnum side)
    {
        throw new NotImplementedException();
    }

    public int GetNumberOfPlayerControllableTroops()
    {
        throw new NotImplementedException();
    }
}