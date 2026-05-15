using HarmonyLib;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
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
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.GiveInitialGrainToAILords));
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, new Action<CampaignGameStarter, int>(this.GiveInitialInfluenceToClans));
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        /// <summary>
        /// All heroes are spawned with parties and some food on game creation.
        /// </summary>
        /// <remarks>
        /// The game already calls this through the OnNewGameCreatedPartialFollowUp event in HeroSpawnCampaignBehavior with the OnNonBanditClanDailyTick method call, but that's occurring too soon as tor's nobles aren't spawning with parties.
        ///SpawnLordParty checks for newGame == true and adds additional troops to the party to scale up the member count to be closer to the maximum party size.
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
                if (clan.StringId == "troll_clan_1") continue;//prevents troll clan parties with leader heroes from spawning with hundreds of trolls
                considerSpawningLordPartiesMethod.Invoke(heroSpawnCampaignBehaviorInstance, new object[] { clan, true });
            }

        }

        /// <summary>
        /// Gives all AI lords an initial contingent of 100 grain at campaign start.
        /// </summary>
        private void GiveInitialGrainToAILords(CampaignGameStarter starter, int i)
        {
            var grain = MBObjectManager.Instance.GetObject<ItemObject>("grain");
            if (grain == null) return;

            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                if (hero == Hero.MainHero) continue;
                if (!hero.IsLord) continue;
                if (hero.PartyBelongedTo == null) continue;

                hero.PartyBelongedTo.Party.ItemRoster.AddToCounts(grain, 100);
            }
        }

        /// <summary>
        /// Gives the faction leader and one other clan in each kingdom 200 starting influence.
        /// </summary>
        private void GiveInitialInfluenceToClans(CampaignGameStarter starter, int i)
        {
            if (i != 90) return;

            foreach (Kingdom kingdom in Kingdom.All)
            {
                if (kingdom.IsEliminated) continue;
                if (kingdom.Clans.Count == 0) continue;

                // Give ruling clan 200 influence
                var rulingClan = kingdom.RulingClan;
                if (rulingClan != null)
                {
                    rulingClan.Influence+=200f;
                }

                // Give one other random clan 200 influence
                var randomClan = kingdom.Clans.Where(c => c != rulingClan && !c.IsEliminated).TakeRandom(1).FirstOrDefault();

                if (randomClan != null)
                {
                    randomClan.Influence+=200f;
                }
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

            PartyBase.MainParty.MemberRoster.UpdateVersion();//Forces the main party's size to be recalculated due to version mismatch on next value refresh for UI so it performs a full calculation after the CharacterAttributes have been loaded so detection of specific attibutes like Undead

            TORPartyWageModel.ClearCharacterWageCache();//Initial wage calculations are performed during campaign loading before the extended info manager is initialized which is needed to detect troops with wage exceptions, eg. undead and tree spirits.
        }
    }
}