using System;
using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class AccusationScript : CareerAbilityScript
    {

        protected override void OnBeforeTick(float dt)
        {
            if (ExplicitTargetAgents.Count == 0) return;

            var chance = Ability.Template.ScaleVariable1;

            var additionalTargetNumber = CalculateAdditonalTargetAmount(chance);

            var additionalTargets = GetAdditionalAccusationMarkTargets(ExplicitTargetAgents[0].Position.AsVec2, additionalTargetNumber);

            MBList<Agent> list = new MBList<Agent>
            {
                ExplicitTargetAgents[0]
            };
            list.AddRange(additionalTargets);
            
            SetExplicitTargetAgents(list);
        }

        public static int CalculateAdditonalTargetAmount(float chance)
        {
            var additionalTargetNumber = 0;
            if (chance > 1)
            {
                additionalTargetNumber = 9;
            }
            else
            {
                for (int i = 0; i < 9; i++)
                {
                    var trial = MBRandom.RandomFloatRanged(0f,1f);
                    if (trial <= chance)
                    {
                        additionalTargetNumber++;
                        continue;
                    }
                    break;
                }
            }

            return additionalTargetNumber;
        }


        public static MBList<Agent> GetAdditionalAccusationMarkTargets(Vec2 pos, int limit = 0)
        {
            var targets = Mission.Current.GetNearbyAgents(pos, 5, new MBList<Agent>()).TakeRandom(limit).ToMBList();
            if (limit > 0&&targets.Count < limit)
            {
                List<Agent> list = targets.ToList();
                targets = list.TakeRandom(limit).ToMBList();
            }
            
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                if (target.Team.MBTeam.IsValid&&target.Team.IsPlayerTeam)
                {
                    targets.Remove(target);
                    continue;
                }
                
                var tempAttributes = target.GetComponent<StatusEffectComponent>().GetTemporaryAttributes();

                if (tempAttributes.Contains("AccusationMark"))
                {
                    targets.Remove(target);
                }
            }
            
            return targets;
        }
    }
}