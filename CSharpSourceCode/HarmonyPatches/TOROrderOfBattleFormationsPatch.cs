using HarmonyLib;
using SandBox.ViewModelCollection;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.OrderOfBattle;
using TOR_Core.CampaignMechanics.Formations;

namespace TOR_Core.HarmonyPatches
{
    public static class TOROrderOfBattleFormationsPatch
    {
        private static readonly System.Reflection.FieldInfo AllFormationsField = AccessTools.Field(typeof(OrderOfBattleVM), "_allFormations");
        private static readonly System.Reflection.FieldInfo AllHeroesField = AccessTools.Field(typeof(OrderOfBattleVM), "_allHeroes");

        private static List<OrderOfBattleFormationItemVM> GetFormations(OrderOfBattleVM viewModel)
        {
            return (List<OrderOfBattleFormationItemVM>)AllFormationsField.GetValue(viewModel);
        }

        private static List<OrderOfBattleHeroItemVM> GetHeroes(OrderOfBattleVM viewModel)
        {
            return (List<OrderOfBattleHeroItemVM>)AllHeroesField.GetValue(viewModel);
        }

        private static bool IsPlayerPartyHero(OrderOfBattleHeroItemVM heroItem)
        {
            return heroItem.Agent.Character is CharacterObject character && character.IsHero &&
                   (character.HeroObject == Hero.MainHero || character.HeroObject.PartyBelongedTo == MobileParty.MainParty);
        }

        private static void ClearHeroAssignment(OrderOfBattleHeroItemVM heroItem)
        {
            var assignedFormation = heroItem.CurrentAssignedFormationItem;
            if (assignedFormation == null)
            {
                return;
            }

            if (assignedFormation.Captain == heroItem)
            {
                assignedFormation.UnassignCaptain();
            }
            else
            {
                assignedFormation.RemoveHeroTroop(heroItem);
            }
        }

        private static void AssignCaptain(OrderOfBattleFormationItemVM formation, OrderOfBattleHeroItemVM captain)
        {
            if (formation.Captain == captain)
            {
                return;
            }

            var previousCaptain = formation.Captain;
            if (formation.HasCaptain)
            {
                formation.UnassignCaptain();
                if (previousCaptain.Agent != null && previousCaptain.Agent != captain.Agent && !previousCaptain.Agent.IsMainAgent)
                {
                    formation.AddHeroTroop(previousCaptain);
                }
            }

            ClearHeroAssignment(captain);
            formation.Captain = captain;
        }

        private static void ApplyHeroAssignments(OrderOfBattleVM viewModel, List<OrderOfBattleFormationItemVM> formations)
        {
            var heroes = GetHeroes(viewModel);
            var captainPreferences = Campaign.Current.GetCampaignBehavior<TORFormationCampaignBehavior>();
            var savedMainHeroFormation = -1;
            for (var i = 0; i < formations.Count; i++)
            {
                if (captainPreferences.GetCaptain(i) == Hero.MainHero)
                {
                    savedMainHeroFormation = i;
                }
            }

            foreach (var heroItem in heroes)
            {
                if (!IsPlayerPartyHero(heroItem) || heroItem.Agent.IsMainAgent)
                {
                    continue;
                }

                var formation = formations[(int)TORFormationAssignments.GetFormation((CharacterObject)heroItem.Agent.Character)];
                if (heroItem.CurrentAssignedFormationItem != formation)
                {
                    ClearHeroAssignment(heroItem);
                    formation.AddHeroTroop(heroItem);
                }
            }

            for (var i = 0; i < formations.Count; i++)
            {
                var formation = formations[i];
                if (formation.HasCaptain && formation.Captain.Agent.IsMainAgent &&
                    savedMainHeroFormation >= 0 && savedMainHeroFormation != i)
                {
                    formation.UnassignCaptain();
                }
            }

            for (var i = 0; i < formations.Count; i++)
            {
                var formation = formations[i];
                var previousCaptain = captainPreferences.GetCaptain(i);
                var captain = heroes.FirstOrDefault(heroItem =>
                    IsPlayerPartyHero(heroItem) && heroItem.Agent.Character == previousCaptain?.CharacterObject &&
                    (heroItem.Agent.IsMainAgent || heroItem.CurrentAssignedFormationItem == formation));

                if (captain != null)
                {
                    AssignCaptain(formation, captain);
                    continue;
                }

                if (formation.HasCaptain && (formation.Captain.Agent.IsMainAgent ||
                    formation.Captain.Agent.Formation == formation.Formation))
                {
                    continue;
                }

                if (formation.HasCaptain)
                {
                    formation.UnassignCaptain();
                }

                var candidates = heroes.Where(heroItem => IsPlayerPartyHero(heroItem) &&
                    !heroItem.Agent.IsMainAgent && heroItem.CurrentAssignedFormationItem == formation).ToList();
                if (candidates.Count > 0)
                {
                    AssignCaptain(formation, candidates[MBRandom.RandomInt(candidates.Count)]);
                }
            }
        }

        // prevent native from overwriting TOR's player hero and captain assignments when distributing troops
        [HarmonyPatch(typeof(OrderOfBattleVM), "DistributeAllTroops")]
        public static class KeepTORSpawnFormations
        {
            [HarmonyPrefix]
            public static bool Prefix(OrderOfBattleVM __instance)
            {
                if (__instance is not SPOrderOfBattleVM || !TORFormationAssignments.HasPlayerCultureFormations ||
                    !__instance.IsPlayerGeneral)
                {
                    return true;
                }

                var formations = GetFormations(__instance);
                ApplyHeroAssignments(__instance, formations);
                foreach (var formation in formations)
                {
                    if (formation.Formation.CountOfUnits > 0)
                    {
                        formation.RefreshFormation(formation.Formation);
                    }
                }
                return false;
            }
        }

        // vanilla stores heroes and captains separately for each battle type but i dont think nobody has noticed this so far. storing the last assignments should suffice
        [HarmonyPatch(typeof(SPOrderOfBattleVM), "SaveConfiguration")]
        public static class SaveHeroAssignments
        {
            [HarmonyPrefix]
            public static void Prefix(SPOrderOfBattleVM __instance)
            {
                if (!TORFormationAssignments.HasPlayerCultureFormations || !__instance.IsPlayerGeneral)
                {
                    return;
                }

                foreach (var formation in GetFormations(__instance))
                {
                    if (!formation.HasCaptain && formation.HeroTroops.Count == 1 && !formation.HeroTroops[0].Agent.IsMainAgent)
                    {
                        AssignCaptain(formation, formation.HeroTroops[0]);
                    }
                }
            }

            [HarmonyPostfix]
            public static void Postfix(SPOrderOfBattleVM __instance)
            {
                if (!TORFormationAssignments.HasPlayerCultureFormations || !__instance.IsPlayerGeneral)
                {
                    return;
                }

                var captainPreferences = Campaign.Current.GetCampaignBehavior<TORFormationCampaignBehavior>();
                var formations = GetFormations(__instance);
                for (var i = 0; i < formations.Count; i++)
                {
                    var formation = formations[i];
                    if (formation.HasCaptain && IsPlayerPartyHero(formation.Captain))
                    {
                        var captain = ((CharacterObject)formation.Captain.Agent.Character).HeroObject;
                        captainPreferences.SetCaptain(i, captain);
                        if (captain != Hero.MainHero)
                        {
                            Campaign.Current.SetPlayerFormationPreference(captain.CharacterObject, (FormationClass)i);
                        }
                    }
                    else
                    {
                        captainPreferences.ClearCaptain(i);
                    }

                    foreach (var heroItem in formation.HeroTroops)
                    {
                        if (IsPlayerPartyHero(heroItem) && !heroItem.Agent.IsMainAgent)
                        {
                            Campaign.Current.SetPlayerFormationPreference(
                                (CharacterObject)heroItem.Agent.Character, (FormationClass)i);
                        }
                    }
                }
            }
        }
    }
}