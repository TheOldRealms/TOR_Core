using SandBox.Tournaments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions
{
    public class BrawlTournamentMissionController(CultureObject culture) : MissionLogic, ITournamentGameBehavior
    {
        //Match methods derive from the ITournamentGameBehavior interface

        public void StartMatch(TournamentMatch match, bool isLastRound)
        {
            throw new NotImplementedException();
        }

        public void SkipMatch(TournamentMatch match)
        {
            throw new NotImplementedException();
        }
        
        public bool IsMatchEnded()
        {
            throw new NotImplementedException();
        }
        
        public void OnMatchEnded()
        {
            throw new NotImplementedException();
        }
        

        //These derive from MissionLogic
        
        public override InquiryData OnEndMissionRequest(out bool canLeave)
        {
            return base.OnEndMissionRequest(out canLeave);
        }

        public override bool MissionEnded(ref MissionResult missionResult)
        {
            return base.MissionEnded(ref missionResult);
        }

        public override void OnBattleEnded()
        {
            base.OnBattleEnded();
        }

        public override void ShowBattleResults()
        {
            base.ShowBattleResults();
        }

        public override void OnRetreatMission()
        {
            base.OnRetreatMission();
        }

        public override void OnSurrenderMission()
        {
            base.OnSurrenderMission();
        }

        public override List<EquipmentElement> GetExtraEquipmentElementsForCharacter(BasicCharacterObject character, bool getAllEquipments = false)
        {
            return base.GetExtraEquipmentElementsForCharacter(character, getAllEquipments);
        }

        public override void OnMissionResultReady(MissionResult missionResult)
        {
            base.OnMissionResultReady(missionResult);
        }


        //These derive from MissionBehavior

        public override void AfterStart()
        {
            base.AfterStart();
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
        }

        public override List<CompassItemUpdateParams> GetCompassTargets()
        {
            return base.GetCompassTargets();
        }

        public override bool IsThereAgentAction(Agent userAgent, Agent otherAgent)
        {
            return base.IsThereAgentAction(userAgent, otherAgent);
        }

        public override void OnAddTeam(Team team)
        {
            base.OnAddTeam(team);
        }

        public override void OnAfterMissionCreated()
        {
            base.OnAfterMissionCreated();
        }

        public override void OnAgentBuild(Agent agent, Banner banner)
        {
            base.OnAgentBuild(agent, banner);
        }

        public override void OnAgentCreated(Agent agent)
        {
            base.OnAgentCreated(agent);
        }

        public override void OnAgentDeleted(Agent affectedAgent)
        {
            base.OnAgentDeleted(affectedAgent);
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            base.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData);
        }

        public override void OnAgentInteraction(Agent userAgent, Agent agent, sbyte agentBoneIndex)
        {
            base.OnAgentInteraction(userAgent, agent, agentBoneIndex);
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
        }

        public override void OnClearScene()
        {
            base.OnClearScene();
        }

        public override void OnCreated()
        {
            base.OnCreated();
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            base.OnEarlyAgentRemoved(affectedAgent, affectorAgent, agentState, blow);
        }

        public override void OnEndMissionInternal()
        {
            base.OnEndMissionInternal();
        }

        public override void OnFixedMissionTick(float fixedDt)
        {
            base.OnFixedMissionTick(fixedDt);
        }

        public override void OnFocusGained(Agent agent, IFocusable focusableObject, bool isInteractable)
        {
            base.OnFocusGained(agent, focusableObject, isInteractable);
        }

        public override void OnFocusLost(Agent agent, IFocusable focusableObject)
        {
            base.OnFocusLost(agent, focusableObject);
        }


        public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            base.OnMeleeHit(attacker, victim, isCanceled, collisionData);
        }

        public override void OnMissionScreenPreLoad()
        {
            base.OnMissionScreenPreLoad();
        }

        public override void OnMissionStateActivated()
        {
            base.OnMissionStateActivated();
        }

        public override void OnMissionStateDeactivated()
        {
            base.OnMissionStateDeactivated();
        }

        public override void OnMissionStateFinalized()
        {
            base.OnMissionStateFinalized();
        }

        public override void OnMissionTick(float dt)
        {
            base.OnMissionTick(dt);
        }

        public override void OnPreDisplayMissionTick(float dt)
        {
            base.OnPreDisplayMissionTick(dt);
        }

        public override void OnPreMissionTick(float dt)
        {
            base.OnPreMissionTick(dt);
        }

        public override void OnRegisterBlow(Agent attacker, Agent victim, WeakGameEntity realHitEntity, Blow b, ref AttackCollisionData collisionData, in MissionWeapon attackerWeapon)
        {
            base.OnRegisterBlow(attacker, victim, realHitEntity, b, ref collisionData, attackerWeapon);
        }

        public override void OnRemoveBehavior()
        {
            base.OnRemoveBehavior();
        }

        public override void OnRenderingStarted()
        {
            base.OnRenderingStarted();
        }

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
        {
            base.OnScoreHit(affectedAgent, affectorAgent, attackerWeapon, isBlocked, isSiegeEngineHit, blow, collisionData, damagedHp, hitDistance, shotDifficulty);
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
        }

        protected override void OnGetAgentState(Agent agent, bool usedSurgery)
        {
            base.OnGetAgentState(agent, usedSurgery);
        }
    }
}
