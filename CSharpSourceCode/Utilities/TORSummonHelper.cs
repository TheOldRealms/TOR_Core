using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;

namespace TOR_Core.Utilities
{
    public static class TORSummonHelper
    {
        private static readonly ActionIndexCache act_raise_from_ground = ActionIndexCache.Create("act_raisefromground");

        public static AgentBuildData GetAgentBuildData(Agent caster, string summonedUnitID)
        {
            BasicCharacterObject troopCharacter = MBObjectManager.Instance.GetObject<BasicCharacterObject>(summonedUnitID);

            if (troopCharacter == null) return null;
            
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
        
        
        
        public static Agent SpawnAgent(AgentBuildData buildData, Vec3 position, bool withAnimation = false)
        {
            Agent troop = Mission.Current.SpawnAgent(buildData, false);
            Vec3 spawnPos = position;
            if(!Mission.Current.Scene.GetNavigationMeshForPosition(ref position))
            {
                spawnPos = Mission.Current.GetRandomPositionAroundPoint(position, 0.05f, 5f, true);
            }
            troop.TeleportToPosition(spawnPos);
            troop.FadeIn();
            troop.WieldInitialWeapons();
            troop.SetWatchState(Agent.WatchState.Alarmed);
            if (withAnimation)
            {
                troop.SetActionChannel(0, act_raise_from_ground);
                troop.SetCurrentActionProgress(0, 0f);
                troop.SetCurrentActionSpeed(0, 1f);
            }
            return troop;
        }
    }
}