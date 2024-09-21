using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ink.Parsed;
using NLog;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.StatusEffect
{
    public class StatusEffectMissionLogic : MissionLogic
    {
        private Queue<Agent> _unprocessedAgents = new Queue<Agent>();

        public override void OnAgentCreated(Agent agent)
        {
            if (agent.IsHuman)
            {
                StatusEffectComponent effectComponent = new StatusEffectComponent(agent);
                agent.AddComponent(effectComponent);
            }
            
            _unprocessedAgents.Enqueue(agent);
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
            
            while (_unprocessedAgents.Count>0)
            {
                var queueAgent = _unprocessedAgents.Dequeue();
                CheckPermanentEffectsForAddingPermanentEffects(queueAgent);
            }
        }


        private void CheckPermanentEffectsForAddingPermanentEffects(Agent agent)
        {
            if (agent?.Character == null)
            {
                return;
            }
            
            if(!agent.BelongsToMainParty())
            {
                return;
            }
            
            if (agent.WieldedWeapon.IsEmpty) return;

            if (agent.BelongsToMainParty()&& Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                CareerHelper.PowerstoneEffectAssignment(agent);
                return;
            }

            if (agent.GetOriginMobileParty().HasBlessing("cult_of_loec"))
            {
                CareerHelper.AddDefaultPermanentMissionEffect(agent,"loec_blessing_mvs");
                CareerHelper.AddDefaultPermanentMissionEffect(agent,"loec_blessing_ats");
            }
            
            if (agent.BelongsToMainParty()&& Hero.MainHero.HasCareer(TORCareers.Waywatcher))
            {
                var partyExtendedInfo = Hero.MainHero.PartyBelongedTo.GetPartyInfo();

                var infos = partyExtendedInfo.TroopAttributes.FirstOrDefault(x => x.Key == agent.Character.StringId);
                if (infos.Key!=null)
                {
                    foreach (var id in infos.Value)
                    {
                        CareerHelper.AddDefaultPermanentMissionEffect(agent,id);
                    }
                }
                
            }
        }
    }
}