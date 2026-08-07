using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.Missions
{
    /// <summary>
    /// Used to provide experience in missions where siege weapons, formation captains, and side commanders aren't accounted for.
    /// </summary>
    public class MissionExperienceBehavior : MissionLogic
    {
        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	    {
		    if (affectorAgent == null)
		    {
			    return;
		    }
		    if (affectorAgent.IsMount && affectorAgent.RiderAgent != null)
		    {
			    affectorAgent = affectorAgent.RiderAgent;
		    }
		    if (affectorAgent.Character == null || affectedAgent.Character == null)
		    {
			    return;
		    }
		    float inflictedDamage = (float)blow.InflictedDamage;
		    if (inflictedDamage > affectedAgent.HealthLimit)
		    {
			    inflictedDamage = affectedAgent.HealthLimit;
		    }
		    float fractionOfAgentHealthRemoved = inflictedDamage / affectedAgent.HealthLimit;
		    this.EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, blow.AttackType, 0.5f * fractionOfAgentHealthRemoved, inflictedDamage, collisionData.IsSneakAttack);
	    }

        public void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData lastAttackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount, bool isSneakAttack)
        {
        	CharacterObject affectorCharacter = (CharacterObject)affectedAgent.Character;
        	CharacterObject affectedCharacter = (CharacterObject)affectorAgent.Character;
        	if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
        	{
        		bool isHorseCharge = affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision;
        		SkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, null, null, lastSpeedBonus, lastShotDifficulty, lastAttackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, false, damageAmount, affectedAgent.Health < 1f, false, isHorseCharge, isSneakAttack);
        	}
        }
    }
}
