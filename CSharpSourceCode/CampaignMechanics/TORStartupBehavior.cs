using HarmonyLib;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORStartupBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.SpawnAiHeroParties));
        }

        /// <summary>
        /// All heroes are spawned with parties and some food on game creation.
        /// </summary>
        /// <remarks>
        /// The game already calls this through the OnNewGameCreatedPartialFollowUp event in HeroSpawnCampaignBehavior with the OnNonBanditClanDailyTick method call, but evidently that's occuring too soon as tor's nobles aren't spawning with parties.
        ///
        /// LordPartyComponent.InitializeLordPartyProperties will check if GameStarted != true (ie. during the campaign initialization), then it will pass through a larger value to InitializeMobilePartyAroundPosition(PartyTemplate, ...) which in turn passes it into FillPartyStacks
        /// SpawnLordParty after Game start will grant the minimum between 19 troops + leader or 10% of their max party size
        ///
        /// Sly : This could be at risk of causing a crash due to mobParty data caching not being fast enough to complete before the method will attempt to spawn parties; no issues so far, but I ran into a crash with another mod when attempting to run a similar method on the CharacterCreationOverEvent where the parallel methods for data caching were too slow compared to the time it took to exit char creation.
        /// </remarks>
        ///

        //Sly : this is likely resolved by clans/kingdoms having an initial home settlement now
        private void SpawnAiHeroParties(CampaignGameStarter starter, int i)
        {
            var heroSpawnCampaignBehaviorInstance = Activator.CreateInstance(typeof(HeroSpawnCampaignBehavior));
            var considerSpawningLordPartiesMethod = AccessTools.Method(typeof(HeroSpawnCampaignBehavior), "ConsiderSpawningLordParties");

            //Campaign.Current.GameStarted is still false at this point in campaign initialization; it will be set to true on the first game tick
            foreach (Clan clan in Campaign.Current.Clans)
            {
                considerSpawningLordPartiesMethod.Invoke(heroSpawnCampaignBehaviorInstance, new object[] { clan, true });
            }
            //place chaos, beastmen, and bandit nobles on top of settlements of their culture maybe?

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
    }
}