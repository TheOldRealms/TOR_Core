using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
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
            bool isLordParty = battleParty.Party != null && battleParty.Party.MobileParty != null && battleParty.Party.MobileParty.IsLordParty;
            bool isPlayerEvent = battleParty.Party != null && battleParty.Party.MapEvent != null && battleParty.Party.MapEvent.IsPlayerMapEvent;
            if (forcePriorityTroops || (priorityTroops != null && priorityTroops.Count() > 0) || !isLordParty || !isPlayerEvent)
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
            bool partyHasArtilleryCrew = list.AnyQ(x => x.Troop.HasAttribute("ArtilleryCrew"));
            var crewMembers = 0;
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
                        num = 10;
                        if (partyHasArtilleryCrew && crewMembers <= 6)
                        {
                            if (element.Troop.HasAttribute("ArtilleryCrew"))
                            {
                                num *= 15;
                                crewMembers++;
                                
                                goto skip;
                            }
                        }
                        if(element.Troop.DefaultFormationClass == FormationClass.Infantry && CalculateRatioOfFormationClass(FormationClass.Infantry, priorityList) < DesiredInfantryRatio)
                        {
                            num *= 10;
                        }
                        else if (element.Troop.DefaultFormationClass == FormationClass.Ranged && CalculateRatioOfFormationClass(FormationClass.Ranged, priorityList) < DesiredRangedRatio)
                        {
                            num *= 10;
                        }
                        else if (element.Troop.DefaultFormationClass == FormationClass.Cavalry && CalculateRatioOfFormationClass(FormationClass.Cavalry, priorityList) < DesiredCavalryRatio)
                        {
                            num *= 10;
                        }
                        else if (element.Troop.DefaultFormationClass == FormationClass.HorseArcher && CalculateRatioOfFormationClass(FormationClass.HorseArcher, priorityList) < DesiredRangedCavalryRatio)
                        {
                            num *= 10;
                        }
                        skip:
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
            return priorityList.WhereQ(x => x.Item1.Troop.DefaultFormationClass == formationClass).Count() / priorityList.Count;
        }
    }
}
