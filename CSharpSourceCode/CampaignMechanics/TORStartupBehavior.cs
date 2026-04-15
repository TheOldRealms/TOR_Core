using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORStartupBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.SpawnAiHeroParties));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        /// <summary>
        /// All heroes are spawned with parties and some food on game creation.
        /// </summary>
        /// <remarks>
        /// The game already calls this through the OnNewGameCreatedPartialFollowUp event in HeroSpawnCampaignBehavior with the OnNonBanditClanDailyTick method call, but that's occurring too soon as tor's nobles aren't spawning with parties.
        ///
        /// LordPartyComponent.InitializeLordPartyProperties will check if GameStarted != true (ie. during the campaign initialization), then it will pass through a larger value to InitializeMobilePartyAroundPosition(PartyTemplate, ...) which in turn passes it into FillPartyStacks
        /// SpawnLordParty after Game start will grant the minimum between 19 troops + leader or 10% of their max party size
        /// </remarks>

        //Sly : this could likely resolved be by clans/kingdoms having an initial home settlement now. This is what is done to handle the raider clans, empire deserters, etc..
        private void SpawnAiHeroParties(CampaignGameStarter starter, int i)
        {
            if (i != 90) return;//Sly : arbitrary choice

            var heroSpawnCampaignBehaviorInstance = Activator.CreateInstance(typeof(HeroSpawnCampaignBehavior));
            var considerSpawningLordPartiesMethod = AccessTools.Method(typeof(HeroSpawnCampaignBehavior), "ConsiderSpawningLordParties");

            //Campaign.Current.GameStarted is still false at this point in campaign initialization; it will be set to true on the first game tick
            foreach (Clan clan in Campaign.Current.Clans)
            {
                considerSpawningLordPartiesMethod.Invoke(heroSpawnCampaignBehaviorInstance, new object[] { clan, true });
            }

        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        /// <summary>
        /// Sets up initial alliances when a new game is created.
        /// </summary>
        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            // Set up initial alliances between friendly kingdoms
            SetupInitialAlliances();
        }

        /// <summary>
        /// Creates initial alliances between kingdoms that should start the game allied.
        /// </summary>
        private void SetupInitialAlliances()
        {
            // Moot and Stirland alliance (Dwarves and Empire)
            var moot = Kingdom.All.Find(m => m.StringId == TORConstants.Factions.MOOT);
            var stirland = Kingdom.All.Find(s => s.StringId == TORConstants.Factions.STIRLAND);
            if (moot != null && stirland != null)
            {
                moot.SetAlliance(stirland);
                TORKingdomDecisionsCampaignBehavior.UpdateWarPeaceForAlliance(stirland);
                stirland.SetAllyTriggered(false);
            }

            // Additional alliances can be added here as needed
            // Example: Empire provinces that should be allied
            // var reikland = Kingdom.All.Find(k => k.StringId == "reikland");
            // var middenland = Kingdom.All.Find(k => k.StringId == "middenland");
        }

        // late init
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            //override TradePenaltyReduction description once effects are initialized
            TOR_Core.Models.TORTradeItemPriceFactorModel.ApplyTradePenaltyReductionDescriptionOverride();

            TORPartySizeModel.RecalculateMainPartySize();//Forces the main party's size to be recalculated so it performs a full calculation using all of TOR's bonuses

            TORPartyWageModel.ClearCharacterWageCache();//Initial wage calculations are performed during campaign loading before the extended info manager is initialized which is needed to detect troops with wage exceptions, eg. undead and tree spirits.
        }
    }
}