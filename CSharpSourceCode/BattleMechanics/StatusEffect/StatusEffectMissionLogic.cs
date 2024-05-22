using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
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
        private List<Agent> _unprocessedCharacters;

        public override void OnCreated()
        {
            base.OnCreated();
            
            _unprocessedCharacters = new List<Agent>();
        }

        public override void OnAfterMissionCreated()
        {
            base.OnAfterMissionCreated();
            _unprocessedCharacters = new List<Agent>();
        }

        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsHuman)
            {
                StatusEffectComponent effectComponent = new StatusEffectComponent(agent);
                agent.AddComponent(effectComponent);
            }
            
            _unprocessedCharacters.Add(agent);
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
                if (!_unprocessedCharacters.Any()) continue;

                if (_unprocessedCharacters.Contains(agent))
                {
                    CheckPermanentEffectsForAddingPermanentEffects(agent);
                }
            }
        }


        private void CheckPermanentEffectsForAddingPermanentEffects(Agent agent)
        {
            if(agent.Character==null) return;
            if (agent.WieldedWeapon.IsEmpty || !_unprocessedCharacters.Contains(agent)) return;
            
            if (agent.BelongsToMainParty()&& Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                CareerHelper.PowerstoneEffectAssignment(agent);
            
            }

            
            _unprocessedCharacters.Remove(agent);

        }
    }
}