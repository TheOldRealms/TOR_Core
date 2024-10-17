using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Options;
using TaleWorlds.MountAndBlade.Options.ManagedOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions.GameKeys;
using TOR_Core.GameManagers;
using TOR_Core.Utilities;

namespace TOR_Core.HarmonyPatches
{
    /// <summary>
    /// I hope this patch is one day not needed. During the check for categories, taleworlds decided to leave out non vanilla contexts inside the OptionsVM.
    /// the only way to put those in is setting up the GameKeyContext different, then you can create custom Categories, however, you can't change the inputs.
    /// One way or the other, i hope this stays a temporary solution
    /// </summary>
    [HarmonyPatch]
    public static class GameKeyOptionsCategoryPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OptionsProvider), "GetGameKeyCategoriesList")]
        public static IEnumerable<string> Postfix( IEnumerable<string> __result)
        {
            return __result.AddItem(nameof(TORGameKeyContext));
        }
    }
    
    
    [HarmonyPatch]
    public static class GameplayCampaignOptionsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(OptionsProvider), "GetGameplayOptionGroups")]
        public static IEnumerable<OptionGroup>  Postfix( IEnumerable<OptionGroup>  __result)
        {
            TOROptions.SetupTexts();


            return AddOptions(ref __result);
        }
        
        private static IEnumerable<OptionGroup> AddOptions(ref IEnumerable<OptionGroup> optionGroup)
        {
            var list = optionGroup.ToList();
            
            var torBanditDensity = new OptionGroup(new TextObject("The Old Realms: Bandit Density"), TOROptions.GetTORBanditOptions());
            var torDiplomacy = new OptionGroup(new TextObject("The Old Realms: Diplomacy"), TOROptions.GetTORDiplomacyOptions());
            var torGeneral = new OptionGroup(new TextObject("The Old Realms: General"), TOROptions.GetTORGeneralOptions());


            list.AddRange(new[] {torGeneral, torBanditDensity,  torDiplomacy});

            return list;

        }
    }
    




    public static class TOROptions
    {

        public static void SetupTexts()
        {
            Module.CurrentModule.GlobalTextManager.LoadDefaultTexts();
            Module.CurrentModule.GlobalTextManager.LoadGameTexts();
            GameTexts.Initialize(Module.CurrentModule.GlobalTextManager);
        }
        public static IEnumerable<IOptionData> GetTORBanditOptions()
        {
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfMaximumLooterPartiesEarly);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfMaximumLooterParties);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfMaximumLooterPartiesLate);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfMaximumBanditPartiesAroundEachHideout);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfMaximumBanditPartiesInEachHideout);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfInitialHideoutsAtEachBanditFaction);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfMaximumHideoutsAtEachBanditFaction);
        }
        
        public static IEnumerable<IOptionData> GetTORDiplomacyOptions()
        {
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.DeclareWarScoreDistanceMultiplier);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.DeclareWarScoreFactionStrengthMultiplier);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.DeclareWarScoreReligiousEffectMultiplier);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumMinKingdomWars);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumMaxKingdomWars);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.MinPeaceDays);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.MinWarDays);
        }
        
        public static IEnumerable<IOptionData> GetTORGeneralOptions()
        {
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.FakeBannerFrequency);
            yield return new TORGameplayCampaignOptions(TORManagedOptionsType.NumberOfTroopsPerFormationWithStandard);
        }
    }
    
    
    
    
    public enum TORManagedOptionsType{
        
        NumberOfMaximumLooterPartiesEarly=49,
        NumberOfMaximumLooterParties = 50,
        NumberOfMaximumLooterPartiesLate = 51,
        NumberOfMaximumBanditPartiesAroundEachHideout = 52,
        NumberOfMaximumBanditPartiesInEachHideout =53,
        NumberOfInitialHideoutsAtEachBanditFaction = 54,
        NumberOfMaximumHideoutsAtEachBanditFaction = 55,
        
        DeclareWarScoreDistanceMultiplier = 56,
        DeclareWarScoreFactionStrengthMultiplier = 57,
        DeclareWarScoreReligiousEffectMultiplier = 58,
        NumMinKingdomWars = 59, 
        NumMaxKingdomWars = 60,
        MinPeaceDays = 61,
        MinWarDays = 62,
        
        FakeBannerFrequency =63,
        NumberOfTroopsPerFormationWithStandard = 64
    }
}