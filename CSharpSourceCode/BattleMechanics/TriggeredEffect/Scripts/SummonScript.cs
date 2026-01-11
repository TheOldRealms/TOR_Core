using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
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

            if (Game.Current.GameType is Campaign)
            {
                if (triggeredByAgent.GetHero() == Hero.MainHero && Hero.MainHero.HasCareer(TORCareers.Necromancer))
                {
                    if (Hero.MainHero.HasCareerChoice("GrimoireNecrisPassive4"))
                    {
                        var equipmentItems = triggeredByAgent.Character.GetCharacterEquipment();

                        foreach (var item in equipmentItems)
                        {
                            bonus += item.GetTraits().Count;
                        }
                    }
                }
            }

            var spawnPosition = position;
            var totalSummoned = NumberToSummon + bonus;
            for (int i = 1; i < totalSummoned + 1; i++)
            {
                spawnPosition = Mission.Current.GetRandomPositionAroundPoint(spawnPosition, 0.1f, 0.6f);
                TORSummonHelper.SpawnAgent(data, spawnPosition, true);
            }

            // Add career charge for necromancer when summoning undead
            if (Game.Current.GameType is Campaign && triggeredByAgent.GetHero() == Hero.MainHero)
            {
                if (Hero.MainHero.HasCareer(TORCareers.Necromancer))
                {
                    var chargePerUnit = 50;
                    var totalCharge = chargePerUnit * totalSummoned;
                    CareerHelper.ApplyCareerAbilityCharge(totalCharge, ChargeType.Custom, AttackTypeMask.Spell, triggeredByAgent);
                }
            }
        }

        public string SummonedUnitID { get; private set; }
        public int NumberToSummon { get; private set; }
    }
}