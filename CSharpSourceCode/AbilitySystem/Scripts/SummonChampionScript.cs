using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Scripts
{
    public class SummonChampionScript : CareerAbilityScript
    {
        private string SummonedChampionId;

        private bool summoned;
        
        private Vec3 targetPosition;
        
        protected override void OnInit()
        {
            var effects = this.GetEffectsToTrigger();
            
            foreach (var effect in effects)
            {
                if (effect.SummonedTroopId != "none")
                {
                    SummonedChampionId = effect.SummonedTroopId;
                    break;
                }
               
            }

            targetPosition = GameEntity.GlobalPosition;
        }
        
        protected override void OnTick(float dt)
        {
            if(summoned) return;
            
            var data = GetAgentBuildData(_casterAgent);
            bool leftSide = false;
            
            var agent = SpawnAgent(data, targetPosition);

            _casterAgent.UnsetSpellCasterMode();

            agent.Controller = Agent.ControllerType.Player;
            
            summoned = true;

        }

        private AgentBuildData GetAgentBuildData(Agent caster)
        {
            BasicCharacterObject troopCharacter = MBObjectManager.Instance.GetObject<BasicCharacterObject>(SummonedChampionId);
            
            IAgentOriginBase troopOrigin = new SummonedAgentOrigin(caster, troopCharacter);
            var formation = caster.Team.GetFormation(FormationClass.Infantry);
            if (formation == null)
            {
                formation = caster.Formation;
            }
            AgentBuildData buildData = new AgentBuildData(troopCharacter).
                Team(caster.Team).
                Formation(formation).
                ClothingColor1(caster.Team.Color).
                ClothingColor2(caster.Team.Color2).
                Equipment(troopCharacter.GetFirstEquipment(false)).
                TroopOrigin(troopOrigin).
                IsReinforcement(true).
                InitialDirection(Vec2.Forward);
            return buildData;
        }

        private Agent SpawnAgent(AgentBuildData buildData, Vec3 position)
        {
            Agent troop = Mission.Current.SpawnAgent(buildData, false);
            troop.TeleportToPosition(position);
            troop.FadeIn();
            troop.SetWatchState(Agent.WatchState.Alarmed);
            return troop;
        }


       
    }
}