using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.BattleMechanics.TriggeredEffect.Scripts
{
    public class SummonScript : ITriggeredScript
    {
        public void OnInit(string troopId, int number)
        {
            SummonedUnitID = troopId;
            NumberToSummon = number;
        }

        public void OnTrigger(Vec3 position, Agent triggeredByAgent, IEnumerable<Agent> triggeredAgents, float duration)
        {
            var data = GetAgentBuildData(triggeredByAgent);
            bool leftSide = false;
            Vec3 lastPosition = position;
            var bonus = 0;

            if (triggeredByAgent.GetHero() == Hero.MainHero)
            {
                if (Hero.MainHero.HasCareer(TORCareers.Necromancer))
                {
                    if(Hero.MainHero.HasCareerChoice("GrimoireNecrisPassive4"))
                    {
                        var equipmentItems = triggeredByAgent.Character.GetCharacterEquipment();

                        foreach (var item in equipmentItems)
                        {
                            if (item.IsMagicalItem())
                            {
                                bonus++;
                            }
                        }
                    }
                }
            }
            
            for(int i = 1; i < NumberToSummon + 1+bonus; i++)
            {
                lastPosition = leftSide ? new Vec3(lastPosition.X - i * 1, lastPosition.Y) : new Vec3(lastPosition.X + i * 1, lastPosition.Y);
                leftSide = !leftSide;
                _ = SpawnAgent(data, lastPosition);
            }
        }

        private AgentBuildData GetAgentBuildData(Agent caster)
        {
            BasicCharacterObject troopCharacter = MBObjectManager.Instance.GetObject<BasicCharacterObject>(SummonedUnitID);
            
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


        public string SummonedUnitID { get; private set; }
        public int NumberToSummon { get; private set; }
    }
}
