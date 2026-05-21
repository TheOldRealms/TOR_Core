using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Missions;

public class BrawlMissionController : MissionLogic, IMissionAgentSpawnLogic
{
    private TroopRoster _enemyRoster;
    private int _enemyPartySize;
    private readonly Action<BrawlMissionResult> _onMissionEnd;

    public bool PlayerCanLeave { get; set; } = false;

    private Dictionary<Hero, int> _scores;
    private List<Hero> _losses;

    private const int NumberOfTeams = 4;
    private List<Team> _brawlTeams = new List<Team>();
    private List<MatrixFrame> _spawnFrames = new List<MatrixFrame>();
    private int _spawnFrameIndex = 0;

    public BrawlMissionController(TroopRoster playerSideTroops, TroopRoster enemyRoster, int enemyPartySize, Action<BrawlMissionResult> onMissionEnd = null)
    {
        base.OnBehaviorInitialize();
        _enemyPartySize = enemyPartySize;
        _enemyRoster = enemyRoster;
        _onMissionEnd = onMissionEnd;
    }

    public override void AfterStart()
    {
        var gates = Mission.GetActiveEntitiesWithScriptComponentOfType<CastleGate>();

        if (gates != null)
        {
            foreach (var gate in gates)
            {
                var components = gate.GetScriptComponents<CastleGate>();
                foreach (var component in components)
                {
                    //component.AutoOpen = true;
                   // component.SetAutoOpenState(true);
                    component.OpenDoorAndDisableGateForCivilianMission();
                }
            }
        }
        
        Mission.SetMissionMode(MissionMode.StartUp, true);
        Mission.IsInventoryAccessible = false;
        Mission.IsQuestScreenAccessible = true;
        Mission.DoesMissionRequireCivilianEquipment = false;

        CollectSpawnFrames();
        SetupBrawlTeams();

        SpawnPlayer();
        SpawnPlayerHeroes();
        SpawnEnemies();

        _scores = new Dictionary<Hero, int>();
        _losses = new List<Hero>();
    }

    private void SetupBrawlTeams()
    {
        _brawlTeams.Clear();

        uint[] teamColors = new uint[]
        {
            0xFF0000FF, // Red (Player)
            0xFF00FF00, // Green
            0xFFFF0000, // Blue
            0xFFFFFF00  // Yellow
        };

        // Player team on Defender side (like vanilla arena)
        var playerTeam = Mission.Teams.Add(BattleSideEnum.Defender, teamColors[0], teamColors[0], null, true, false, true);
        _brawlTeams.Add(playerTeam);

        // All AI teams on Attacker side (like vanilla arena)
        for (int i = 1; i < NumberOfTeams; i++)
        {
            var team = Mission.Teams.Add(BattleSideEnum.Attacker, teamColors[i], teamColors[i], null, true, false, true);
            _brawlTeams.Add(team);
        }

        // Set all teams hostile to each other
        for (int i = 0; i < _brawlTeams.Count; i++)
        {
            for (int j = i + 1; j < _brawlTeams.Count; j++)
            {
                _brawlTeams[i].SetIsEnemyOf(_brawlTeams[j], true);
            }
        }

        Mission.PlayerTeam = _brawlTeams[0];
    }

    private List<Team> GetEnemyTeams()
    {
        return _brawlTeams.Skip(1).ToList();
    }

    private void CollectSpawnFrames()
    {
        _spawnFrames.Clear();

        var spawnEntities = Mission.Scene.FindEntitiesWithTag("sp_arena").ToList();
        if (spawnEntities.Count == 0)
        {
            spawnEntities = Mission.Scene.FindEntitiesWithTag("npc_common").ToList();
        }
        if (spawnEntities.Count == 0)
        {
            spawnEntities = Mission.Scene.FindEntitiesWithTag("spawnpoint_player").ToList();
        }

        foreach (var entity in spawnEntities)
        {
            var frame = entity.GetGlobalFrame();
            frame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            _spawnFrames.Add(frame);
        }
    }

    private MatrixFrame GetNextSpawnFrame()
    {
        if (_spawnFrames.Count == 0)
        {
            return MatrixFrame.Identity;
        }

        var frame = _spawnFrames[_spawnFrameIndex % _spawnFrames.Count];
        _spawnFrameIndex++;
        return frame;
    }

    private void SpawnPlayer()
    {
        MatrixFrame spawnFrame = MatrixFrame.Identity;

        GameEntity playerSpawn = Mission.Scene.FindEntityWithTag("spawnpoint_player");
        if (playerSpawn != null)
        {
            spawnFrame = playerSpawn.GetGlobalFrame();
            spawnFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
        }
        else
        {
            spawnFrame = GetNextSpawnFrame();
        }

        CharacterObject playerCharacter = CharacterObject.PlayerCharacter;

        AgentBuildData agentBuildData = new AgentBuildData(playerCharacter)
            .Team(Mission.PlayerTeam)
            .InitialPosition(spawnFrame.origin)
            .InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized())
            .CivilianEquipment(false)
            .NoHorses(true)
            .NoWeapons(false)
            .ClothingColor1(Mission.PlayerTeam.Color)
            .ClothingColor2(Mission.PlayerTeam.Color2)
            .TroopOrigin(new PartyAgentOrigin(PartyBase.MainParty, playerCharacter, -1, default, false))
            .MountKey(MountCreationKey.GetRandomMountKeyString(playerCharacter.Equipment[EquipmentIndex.ArmorItemEndSlot].Item, playerCharacter.GetMountKeySeed()))
            .Controller(AgentControllerType.Player);

        if (playerCharacter.HeroObject?.ClanBanner != null)
        {
            agentBuildData.Banner(playerCharacter.HeroObject.ClanBanner);
        }

        Agent agent = Mission.SpawnAgent(agentBuildData, false);
        agent.WieldInitialWeapons(Agent.WeaponWieldActionType.InstantAfterPickUp);

        for (int i = 0; i < 3; i++)
        {
            Agent.Main.AgentVisuals.GetSkeleton().TickAnimations(0.1f, Agent.Main.AgentVisuals.GetGlobalFrame(), true);
        }
    }

    private void SpawnPlayerHeroes()
    {
        if (MobileParty.MainParty == null) return;

        var heroCompanions = MobileParty.MainParty.MemberRoster.GetTroopRoster()
            .Where(t => t.Character.IsHero && t.Character.HeroObject != Hero.MainHero)
            .Select(t => t.Character)
            .ToList();

        foreach (var companion in heroCompanions)
        {
            SpawnBrawlAgent(companion, Mission.PlayerTeam);
        }
    }

    private void SpawnEnemies()
    {
        if (_enemyRoster == null) return;

        List<CharacterObject> enemies = _enemyRoster.ToFlattenedRoster()
            .Select(e => e.Troop)
            .ToList();

        if (enemies.Count == 0) return;

        var enemyTeams = GetEnemyTeams();
        int teamIndex = 0;

        for (int i = 0; i < _enemyPartySize; i++)
        {
            Team targetTeam = enemyTeams[teamIndex % enemyTeams.Count];
            CharacterObject character = enemies[i % enemies.Count];
            SpawnBrawlAgent(character, targetTeam);
            teamIndex++;
        }
    }

    private Agent SpawnBrawlAgent(CharacterObject character, Team team)
    {
        MatrixFrame spawnFrame = GetNextSpawnFrame();

        if (spawnFrame == MatrixFrame.Identity)
        {
            return null;
        }

        using (new TWSharedMutexReadLock(Scene.PhysicsAndRayCastLock))
        {
            spawnFrame.origin.z = Mission.Scene.GetGroundHeightAtPosition(spawnFrame.origin, BodyFlags.CommonCollisionExcludeFlags);
        }

        IAgentOriginBase origin = new SimpleAgentOrigin(character);

        AgentBuildData agentData = new AgentBuildData(character)
            .Team(team)
            .Equipment(character.FirstBattleEquipment)
            .TroopOrigin(origin)
            .InitialPosition(spawnFrame.origin)
            .InitialDirection(spawnFrame.rotation.f.AsVec2.Normalized())
            .NoHorses(true)
            .CivilianEquipment(false)
            .ClothingColor1(team.Color)
            .ClothingColor2(team.Color2);

        Agent agent = Mission.SpawnAgent(agentData);
        agent.FadeIn();

        if (agent.IsAIControlled)
        {
            agent.SetWatchState(Agent.WatchState.Alarmed);
        }

        return agent;
    }

    public override InquiryData OnEndMissionRequest(out bool canLeave)
    {
        canLeave = Mission.MissionResult is { BattleResolved: true, PlayerVictory: true } || PlayerCanLeave;
        if (!canLeave)
        {
            MBInformationManager.AddQuickInformation(TORTextHelper.GetTextObject("tor_brawl_cannot_leave", "You may not leave until finishing the brawl."));
        }
        else
        {
            Mission.Current.EndMission();
        }
        return null;
    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
    {
        if (affectorAgent != null && affectorAgent == Agent.Main && affectedAgent.IsHuman)
        {
            var playerHero = Agent.Main.GetHero();
            if (playerHero != null)
            {
                if (!_scores.ContainsKey(playerHero))
                {
                    _scores.Add(playerHero, 1);
                }
                else
                {
                    _scores[playerHero]++;
                }
            }
        }
    }

    public override void OnMissionResultReady(MissionResult missionResult)
    {
        if (missionResult.PlayerDefeated)//Sly : if the result is ready and the mission is cleared, shouldn't the player also be able to leave immediately when winning?
        {
            PlayerCanLeave = true;
        }
    }

    private int GetTotalEnemyCount()
    {
        int count = 0;
        foreach (var agent in Mission.Agents)
        {
            if (agent.IsHuman && agent.IsActive() && agent.Team != Mission.PlayerTeam)
            {
                count++;
            }
        }
        return count;
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

        if (GetTotalEnemyCount() == 0)
        {
            missionResult = MissionResult.CreateSuccessful(Mission, false);
            BrawlMissionResult brawlMissionResult = new BrawlMissionResult(missionResult, _scores, _losses);
            _onMissionEnd?.Invoke(brawlMissionResult);
            return true;
        }

        return false;
    }

    public void StartSpawner(BattleSideEnum side) { }
    public void StopSpawner(BattleSideEnum side) { }
    public bool IsSideSpawnEnabled(BattleSideEnum side) => false;
    public bool IsSideDepleted(BattleSideEnum side) => true;
    public float GetReinforcementInterval(BattleSideEnum side = BattleSideEnum.None)
    {
        return 0;
    }

    public float GetReinforcementInterval() => 1;
    public IEnumerable<IAgentOriginBase> GetAllTroopsForSide(BattleSideEnum side) => Enumerable.Empty<IAgentOriginBase>();
    public bool GetSpawnHorses(BattleSideEnum side) => false;
    public int GetNumberOfPlayerControllableTroops() => 0;
    public BattleSideEnum PlayerSide { get; } //TODO  think that needs to be set properly
}
