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
using TOR_Core.Utilities;

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
            var data = TORSummonHelper.GetAgentBuildData(triggeredByAgent, SummonedUnitID);
            var bonus = 0;

            if(Game.Current.GameType is Campaign)
            {
                if (triggeredByAgent.GetHero() == Hero.MainHero && Hero.MainHero.HasCareer(TORCareers.Necromancer))
                {
                    if (Hero.MainHero.HasCareerChoice("GrimoireNecrisPassive4"))
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

            var spawnPosition = position;
            for (int i = 1; i < NumberToSummon + 1 + bonus; i++)
            {
                spawnPosition = Mission.Current.GetRandomPositionAroundPoint(spawnPosition, 0.1f, 0.6f);
                TORSummonHelper.SpawnAgent(data, spawnPosition, true);
            }
        }
        
        public string SummonedUnitID { get; private set; }
        public int NumberToSummon { get; private set; }
    }
}
