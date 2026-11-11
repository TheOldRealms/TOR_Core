using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Source.Missions;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.BattleMechanics.TriggeredEffect
{
    public class AnimationTriggerMissionLogic : MissionLogic
    {
        public override void OnMeleeHit(Agent attacker, Agent victim, bool isCanceled, AttackCollisionData collisionData)
        {
            if (victim != null) return;
            if (collisionData.CollisionResult != CombatCollisionResult.HitWorld) return;
            if (collisionData.StrikeType != (int)StrikeType.Swing) return;
            if (attacker == null) return;
            if (!AnimationTriggerManager.Instance.RegisteredRaceInts.Contains(attacker.Character.Race)) return;
            
            var effectId = AnimationTriggerManager.GetEffectForRace(attacker.Monster.StringId);
            if (string.IsNullOrEmpty(effectId)) return;

            //Sly : agent.Position is the location of the feet.

            TriggerEffect(effectId, collisionData.CollisionGlobalPosition, attacker);
        }

        public override void OnScoreHit(Agent affectedAgent, Agent affectorAgent, WeaponComponentData attackerWeapon, bool isBlocked, bool isSiegeEngineHit, in Blow blow, in AttackCollisionData collisionData, float damagedHp, float hitDistance, float shotDifficulty)
        {
            //if (affectorAgent == null) return;
            //if (!affectorAgent.IsMonstrous()) return;

            //var effectId = AnimationTriggerManager.GetEffectForCharacter(affectorAgent.Character.StringId);
            //if (string.IsNullOrEmpty(effectId)) return;

            ////Sly : agent.Position is the location of the feet.

            //TriggerEffect(effectId, affectedAgent.Position, affectorAgent);
        }

        private void TriggerEffect(string effectName, Vec3 position, Agent agent)
        {
            var effect = TriggeredEffectManager.CreateNew(effectName);
            effect.Trigger(position, Vec3.Up, agent);
        }
    }
}