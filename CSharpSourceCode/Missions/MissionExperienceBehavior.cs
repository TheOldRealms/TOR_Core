using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.DualWield;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Utilities;

namespace TOR_Core.Missions
{
    /// <summary>
    /// Used to provide experience in missions where siege weapons, formation captains, and side commanders aren't accounted for.
    /// </summary>
    public class MissionExperienceBehavior : MissionLogic
    {
        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
	    {
		    if (affectorAgent == null) return;
		    
		    if (affectorAgent.IsMount && affectorAgent.RiderAgent != null)
		    {
			    affectorAgent = affectorAgent.RiderAgent;
		    }

		    if (affectorAgent.Character == null || affectedAgent.Character == null) return;
		    

		    float inflictedDamage = (float)blow.InflictedDamage;
		    if (inflictedDamage > affectedAgent.HealthLimit)
		    {
			    inflictedDamage = affectedAgent.HealthLimit;
		    }
		    float fractionOfAgentHealthRemoved = inflictedDamage / affectedAgent.HealthLimit;

            
            // Set flag for spell hits so TORCombatXpModel can skip weapon XP while still tracking kills
            bool isSpellHit = collisionData.AffectorWeaponSlotOrMissileIndex == TORSpellBlowHelper.SpellBlowSentinel;
            if (isSpellHit) TORSpellBlowHelper.IsProcessingSpellHit = true;

            try
            {
                EnemyHitReward(affectedAgent, affectorAgent, blow.MovementSpeedDamageModifier, shotDifficulty, attackerWeapon, blow.AttackType, 0.5f * fractionOfAgentHealthRemoved, inflictedDamage, collisionData.IsSneakAttack);
            }
            finally
            {
                if (isSpellHit)
                    TORSpellBlowHelper.IsProcessingSpellHit = false;
            }

            BerserkHitReward(affectorAgent, affectedAgent, inflictedDamage, isSiegeEngineHit, isSpellHit);
	    }

        /// <summary>
        /// Grant Berserk XP to the main hero when a hit lands while dual-wielding. Uses
        /// <c>DualWieldStateManager.IsDualWielding</c> (set at spawn when two matching
        /// weapons are detected), not <c>IsPaired</c> (which is Yotthani's transient
        /// synchronous-swing combat substate). Additive on top of the vanilla
        /// weapon-skill XP routed through <c>EnemyHitReward</c>. Siege and spell hits
        /// are excluded for parity with <c>TORCombatXpModel</c>.
        /// </summary>
        private static int _berserkDiagHits;

        private static void BerserkHitReward(Agent affector, Agent affected, float inflictedDamage, bool isSiegeEngineHit, bool isSpellHit)
        {
            if (isSiegeEngineHit || isSpellHit) return;
            if (affector == null || !affector.IsMainAgent || Hero.MainHero == null) return;

            bool dualWielding = DualWieldStateManager.IsDualWielding(affector);
            if (_berserkDiagHits++ < 20)
            {
                // Temp diagnostic -- confirms the hook fires and whether IsDualWielding detects the player.
                Utilities.TORCommon.Say($"[Berserk] hit#{_berserkDiagHits} dw={dualWielding} dmg={inflictedDamage:F0} currentXp={Hero.MainHero.HeroDeveloper?.GetSkillXpProgress(TORSkills.Berserk)}");
            }
            if (!dualWielding) return;
            if (TORSkills.Berserk == null) return;

            // Scale per hit by inflicted damage (matches CareerPerkMissionBehavior's Roguery grant),
            // lethal blows get a flat kill bonus.
            int xp = Math.Max(1, (int)(inflictedDamage * 0.5f));
            if (affected != null && affected.Health < 1f) xp += 20;

            Hero.MainHero.AddSkillXp(TORSkills.Berserk, xp);
        }

        public void EnemyHitReward(Agent affectedAgent, Agent affectorAgent, float lastSpeedBonus, float lastShotDifficulty, WeaponComponentData lastAttackerWeapon, AgentAttackType attackType, float hitpointRatio, float damageAmount, bool isSneakAttack)
        {
        	CharacterObject affectorCharacter = (CharacterObject)affectorAgent.Character;
        	CharacterObject affectedCharacter = (CharacterObject)affectedAgent.Character;

        	if (affectedAgent.Origin != null && affectorAgent != null && affectorAgent.Origin != null)
        	{
        		bool isHorseCharge = affectorAgent.MountAgent != null && attackType == AgentAttackType.Collision;
        		SkillLevelingManager.OnCombatHit(affectorCharacter, affectedCharacter, null, null, lastSpeedBonus, lastShotDifficulty, lastAttackerWeapon, hitpointRatio, CombatXpModel.MissionTypeEnum.Battle, affectorAgent.MountAgent != null, affectorAgent.Team == affectedAgent.Team, false, damageAmount, affectedAgent.Health < 1f, false, isHorseCharge, isSneakAttack);
        	}
        }
    }
}
