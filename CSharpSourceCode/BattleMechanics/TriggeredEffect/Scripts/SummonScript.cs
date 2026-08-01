using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
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
            // Check if summoning is possible before proceeding
            if (!TORSummonHelper.CanSummon())
            {
                return;
            }

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
                            if (item.GetTraits().Any())
                                bonus += 1;
                        }
                    }
                }
            }

            var spawnPosition = position;
            var desiredCount = NumberToSummon + bonus;
            // Clamp to available slots to prevent exceeding mission agent limit
            var totalSummoned = TORSummonHelper.GetClampedSummonCount(desiredCount);

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