using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectMissionLogic : MissionLogic
    {
        private Queue<Agent> _unprocessedCharacters = new Queue<Agent>();

        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsHuman)
            {
                StatusEffectComponent effectComponent = new StatusEffectComponent(agent);
                agent.AddComponent(effectComponent);
            }
            
            _unprocessedCharacters.Enqueue(agent);
        }

        public override void OnAgentMount(Agent agent)
        {
            StatusEffectComponent effectComponent = agent.GetComponent<StatusEffectComponent>();
            effectComponent.SynchronizeBaseValues(true);
        }

        public override void OnMissionTick(float dt)
        {
            foreach (var agent in Mission.Current.AllAgents)
            {
                if (agent.GetComponent<StatusEffectComponent>() != null)
                {
                    if (agent.IsActive() && agent.Health > 1f)
                    {
                        var comp = agent.GetComponent<StatusEffectComponent>();
                        comp.OnTick(dt);
                    }
                }
            }
            
            while (_unprocessedCharacters.AnyQ())
            {
                var queueAgent = _unprocessedCharacters.Dequeue();

                if (!CheckPermanentEffectsForAddingPermanentEffects(queueAgent))
                {
                    _unprocessedCharacters.Enqueue(queueAgent);
                    break;
                }
            }
        }


        private bool CheckPermanentEffectsForAddingPermanentEffects(Agent agent)
        {
            if (agent?.Character == null)
            {
                if (agent.IsMount)
                {
                    return true;
                }
                return false;
            }
            
            if (agent.WieldedWeapon.IsEmpty) return false;
            
            if (agent.BelongsToMainParty()&& Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                CareerHelper.PowerstoneEffectAssignment(agent);
                return true;
            }
            
            if(!agent.BelongsToMainParty())
            {
                return true;
            }
            
            return false;
        }
    }
}