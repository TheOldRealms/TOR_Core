using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORTroopSupplierModel : DefaultTroopSupplierProbabilityModel
    {
        private readonly float DesiredCavalryRatio = 0.15f;
        private readonly float DesiredInfantryRatio = 0.5f;
        private readonly float DesiredRangedRatio = 0.25f;
        private readonly float DesiredRangedCavalryRatio = 0.1f;

        public override void EnqueueTroopSpawnProbabilitiesAccordingToUnitSpawnPrioritization(MapEventParty battleParty, 
            FlattenedTroopRoster priorityTroops, bool includePlayer, int sizeOfSide, bool forcePriorityTroops, 
            List<(FlattenedTroopRosterElement, MapEventParty, float)> priorityList)
        {
            if(forcePriorityTroops || (priorityTroops != null && priorityTroops.Count() > 0))
            {
                base.EnqueueTroopSpawnProbabilitiesAccordingToUnitSpawnPrioritization(battleParty, priorityTroops, includePlayer, sizeOfSide, forcePriorityTroops, priorityList);
                return;
            }

            UnitSpawnPrioritizations unitSpawnPrioritizations = UnitSpawnPrioritizations.HighLevel;
            MapEvent battle = PlayerEncounter.Battle;
            if (battle != null && battle.IsSiegeAmbush && battleParty.Party == PartyBase.MainParty)
            {
                unitSpawnPrioritizations = Game.Current.UnitSpawnPrioritization;
            }

            var list = battleParty.Troops.ToList();
            list.Shuffle();

            foreach(var element in list)
            {
                float num = 0f;
                if (CanTroopJoinBattle(element, includePlayer))
                {
                    if (element.Troop.IsHero)
                    {
                        num = 1000;
                        if (element.Troop.HeroObject.IsHumanPlayerCharacter) num *= 5;
                        priorityList.Add(new ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>(element, battleParty, num));
                    }
                    else
                    {
                        num = MBRandom.RandomFloatRanged(1f, element.Troop.Tier);
                        if (element.Troop.HasAttribute("ArtilleryCrew") && priorityList.Where(x => x.Item1.Troop.HasAttribute("ArtilleryCrew")).Count() < 6)
                        {
                            num *= 15;
                        }
                        else if(element.Troop.GetFormationClass() == FormationClass.Infantry && CalculateRatioOfFormationClass(FormationClass.Infantry, priorityList) < DesiredInfantryRatio)
                        {
                            num *= 10;
                        }
                        else if (element.Troop.GetFormationClass() == FormationClass.Ranged && CalculateRatioOfFormationClass(FormationClass.Ranged, priorityList) < DesiredRangedRatio)
                        {
                            num *= 10;
                        }
                        else if (element.Troop.GetFormationClass() == FormationClass.Cavalry && CalculateRatioOfFormationClass(FormationClass.Cavalry, priorityList) < DesiredCavalryRatio)
                        {
                            num *= 10;
                        }
                        else if (element.Troop.GetFormationClass() == FormationClass.HorseArcher && CalculateRatioOfFormationClass(FormationClass.HorseArcher, priorityList) < DesiredRangedCavalryRatio)
                        {
                            num *= 10;
                        }
                        num *= MBRandom.RandomFloatRanged(0.9f, 1.1f);
                        priorityList.Add(new ValueTuple<FlattenedTroopRosterElement, MapEventParty, float>(element, battleParty, num));
                    }
                }
            }

        }

        private bool CanTroopJoinBattle(FlattenedTroopRosterElement troopRoster, bool includePlayer)
        {
            return !troopRoster.IsWounded && !troopRoster.IsRouted && !troopRoster.IsKilled && (includePlayer || !troopRoster.Troop.IsPlayerCharacter);
        }

        private float CalculateRatioOfFormationClass(FormationClass formationClass, List<(FlattenedTroopRosterElement, MapEventParty, float)> priorityList)
        {
            if (priorityList.Count == 0) return 0f;
            return priorityList.Where(x => x.Item1.Troop.GetFormationClass() == formationClass).Count() / priorityList.Count;
        }
    }
}
