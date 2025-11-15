using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Companions
{
    public class TORCompanionsCampaignBehavior : CampaignBehaviorBase
    {
        //store by culture id, then manipulate the returned list
        public Dictionary<string, List<CharacterObject>> _potentialCompanionTemplates = [];

        //cache of companions to avoid going through AliveHeroes; there's something like 100 towns with dawi+greenskin update
        private HashSet<Hero> _aliveCompanions = [];

        public override void RegisterEvents()
        {
            //cache wanderer templates, can this be done on the CampaignInitialization event so that it precedes NewGameCreated and can be more generic?
            CampaignEvents.OnGameLoadFinishedEvent.AddNonSerializedListener(this, OnGameLoadFinished);
			CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);

            //replenish/refresh wanderers on the weekly tick; handle deletion of outdated character objects (be aware of how you need to detect dead wanderers)
			CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyTick);
            //backup to refresh a town if the player hired a wanderer
            CampaignEvents.BeforeSettlementEnteredEvent.AddNonSerializedListener(this, VerifyWanderersOnEnter);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, SwapWandererWhenOwnerCultureChange);

            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.HeroOccupationChangedEvent.AddNonSerializedListener(this, OnHeroOccupationChanged);

            //to be determined if I keep here or move elsewhere
            CampaignEvents.CanHeroDieEvent.AddNonSerializedListener(this, CanHeroDie);
            CampaignEvents.DailyTickClanEvent.AddNonSerializedListener(this, AddDailySkillXpToCompanions); //checking all parties via the daily party event includes caravans, patrols, bandits, etc... which are a waste; because heroes must be part of clans (generally, and particularly in this case which ignores things like temporary heroes created for the engineer quest), and we patch clans to spawn as many parties as possible, we can interate through the clan members and check for their party to restrict the amount checked
        }

        //Sly : tempted to put this into OnGameInitializationFinished in SubModule as it is called for both campaign creation and load, but nyeh
        private void OnGameLoadFinished()
        {
            CacheCompanionTemplates();
			foreach (Hero hero in Hero.AllAliveHeroes)
			{
				if (hero.IsWanderer)
				{
					AddToAliveCompanions(hero);
				}
			}
			foreach (Hero hero2 in Hero.DeadOrDisabledHeroes)
			{
				if (hero2.IsAlive && hero2.IsWanderer)
				{
					AddToAliveCompanions(hero2);//add disabled wanderers so they get unregistered on the next weekly tick
				}
			}
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            CacheCompanionTemplates();
			foreach (var town in Town.AllTowns)
            {
                SpawnWanderer(town.Settlement);
            }
        }

        private void WeeklyTick()
        {
            foreach (var town in Town.AllTowns)
            {
                //this assumes that all deletion of incorrect wanderers is correctly handled by other methods - tbd if that holds true
                if (town.IsUnderSiege || town.Settlement.HeroesWithoutParty.WhereQ(x => x.IsWanderer && x.CompanionOf == null).AnyQ())
                {
                    //Skip towns under siege : EnterSettlementAction unsafe
                    //Skip towns that already have a wanderer not in a party AND who is not hired by a clan. The 2nd condition is necessary to not detect governors, or companions who have been separated from their party after a defeat.
                    continue;
                }
                
                SpawnWanderer(town.Settlement);
            }

            foreach (var wanderer in _aliveCompanions)
            {
                if (wanderer.HeroState == Hero.CharacterStates.Disabled)
                {
                    UnregisterWandererObject(wanderer);
                }
            }
        }

        //Sly : this is here for centralizing the companion mechanics, but I'd prefer to put this into the PartyEntered for town components
        private void VerifyWanderersOnEnter(MobileParty party, Settlement settlement, Hero hero)
        {//I hope this event isn't thrown after a siege completes but before the settlement stops being considered under siege!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            if (hero != Hero.MainHero || hero.IsPrisoner || party != MobileParty.MainParty) return;

            
            var clanlessWanderers = settlement.HeroesWithoutParty.WhereQ(x => x.IsWanderer && x.CompanionOf == null).ToList();
            if (settlement.IsUnderSiege) return;//Skip towns under siege : EnterSettlementAction unsafe

            
            if (clanlessWanderers.AnyQ())
            {
                int clanless = clanlessWanderers.Count();
                foreach(var wrongCultureWanderer in clanlessWanderers.WhereQ(x => x.Culture != settlement.Culture))
                {
                    DisableWanderer(wrongCultureWanderer);
                    clanless--;
                }
                if (clanless > 0) return;//a wanderer of the correct culture is still left in the settlement and there's no need to spawn another 
            }
                
            SpawnWanderer(settlement);
        }

        /// <remarks>
        /// capturerHero is null for "peaceful" transitions (votes/gifts).
        /// 
        /// Sly : I think the only way to have an incorrectly cultured companion in a settlement is if the settlement is transferred via cheat command which bypasses the event that this action is performed on.
        /// </remarks>
        private void SwapWandererWhenOwnerCultureChange (Settlement settlement, bool openToClaim, Hero newOwner, Hero oldOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (settlement.Culture == newOwner.Culture) return;//nothing to do for transitions within culture

            var existingWanderers = settlement.HeroesWithoutParty.WhereQ(x => x.IsWanderer && x.CompanionOf == null).ToList();
            if (settlement.IsUnderSiege)//please don't be under siege somehow at this point....!!!!!!!!!!
            {
                //Skip towns under siege : EnterSettlementAction unsafe
                return;
            }
            if (existingWanderers.Any())
            {
                foreach (var wrongWanderer in existingWanderers)
                {
                    DisableWanderer(wrongWanderer);
                }
            }

            SpawnWanderer(settlement);
        }


        private void OnHeroCreated(Hero hero, bool showNotification)
        {
            if (hero.IsAlive && hero.IsWanderer)
			{
				AddToAliveCompanions(hero);
			}
        }

        private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
			if (victim.IsWanderer)
			{
                RemoveFromAliveCompanions(victim);

                if (!victim.HasMet)//or do we wait for weekly tick?
                {
				    UnregisterWandererObject(victim);
                }
			}
        }

        /// <remarks>
        /// Handles wanderers promoted to nobles via vassalization so they're no longer cached here.
        /// </remarks>
        private void OnHeroOccupationChanged(Hero hero, Occupation oldOccupation)
        {
            if (oldOccupation == Occupation.Wanderer)
			{
				RemoveFromAliveCompanions(hero);
				return;
			}
			if (hero.Occupation == Occupation.Wanderer)
			{
				AddToAliveCompanions(hero);
			}
        }

        private void CanHeroDie(Hero dyingHero, KillCharacterAction.KillCharacterActionDetail causeOfDeath, ref bool result)
        {
            //Sly : prevents all hero deaths; I still need to look up what npc-npc execution details are.
            if (causeOfDeath != KillCharacterAction.KillCharacterActionDetail.Executed) {result = false;}
        }

        private void AddDailySkillXpToCompanions(Clan clan)
        {
            foreach (Hero clanmember in clan.AliveLords)
            {
                if (clanmember.IsPartyLeader && clanmember.GetPerkValue(TORPerks.SpellCraft.StoryTeller))//If prisoner, their mobile party would be null. CompanionsInParty iterates through the whole clan's companion cache and compares the parties for the source hero and the companion. For a clan with no companions it's possibly a faster check than looking at the perk value, but as the companion count grows there's more to check for each noble in the clan.
                {
                    //Sly : CompanionsInParty in party is a yield return so we don't actually need to check for more than 0 companions; we can just execute and there will be no iterations if nothing is found.
                    foreach(var companion in clanmember.CompanionsInParty)
                    {
                        var skills = MBObjectManager.Instance.GetObjectTypeList<SkillObject>();
                        var randomskill = skills.TakeRandom(1).FirstOrDefault();
                        var amount = TORPerks.SpellCraft.StoryTeller.PrimaryBonus;
                        companion.AddSkillXp(randomskill, amount);
                    }
                }
            }
        }


        private void CacheCompanionTemplates()
        {
            foreach (CharacterObject characterObject in MBObjectManager.Instance.GetObjectTypeList<CharacterObject>())
			{
				if (characterObject.IsTemplate && characterObject.Occupation == Occupation.Wanderer)
				{
                    var cultureId = characterObject.Culture.StringId;
                    if (!_potentialCompanionTemplates.ContainsKey(cultureId))
                    {
                        _potentialCompanionTemplates.Add(cultureId, []);
                    }
					_potentialCompanionTemplates[cultureId].Add(characterObject);
				}
			}
        }

        private void SpawnWanderer(Settlement settlement)
        {
            var culture = settlement.Culture;
            if (culture == null)
            {
                TORCommon.Log("TORCompanionCampaignBehavior : null culture on " + settlement.StringId, NLog.LogLevel.Warn);
                return;
            }

            var companionTemplate = _potentialCompanionTemplates[culture.StringId].GetRandomElementInefficiently();

            if (companionTemplate == null)
            {
                TORCommon.Log("TORCompanionCampaignBehavior : no template found for " + culture.StringId, NLog.LogLevel.Warn);
                return;
            }

            Hero hero = HeroCreator.CreateSpecialHero(companionTemplate, settlement, null, null, Campaign.Current.Models.AgeModel.HeroComesOfAge + 5 + MBRandom.RandomInt(12)); //age is arbitrary, it will be adjusted up based on the trait levels of the hero.
            //Sly : Given that traits no longer tie into skills, will wanderers generally be younger because their traits will be lower? Do tor's traits get caught in TraitObject.All and lead to an approximately uniform age across wanderers from the same template?

            //adding the wanderer to the _aliveCompanions cache is handled by the HeroCreated event dispatched by the HeroCreator
			AdjustEquipment(hero);
			hero.ChangeState(Hero.CharacterStates.Active);
			EnterSettlementAction.ApplyForCharacterOnly(hero, settlement);
        }

        private void DisableWanderer(Hero wanderer)//should this kill them instead? still unsure on the nuance of dead/disable, except that death shows up in the encyclopedia and the obituary is default text of little relevance
        {
            _aliveCompanions.Remove(wanderer);
            //wanderer is removed and disabled; clean up occurs on the weekly tick to clear out outdated wanderers from the object manager
            DisableHeroAction.Apply(wanderer);//takes care of party membership, settlement, prisonership, CharacterState
        }

        private void UnregisterWandererObject(Hero hero)
        {
            //does this handle encyclopedia entries?
            var objectManager = Campaign.Current.CampaignObjectManager;
            var methodInvoke = AccessTools.Method(typeof(CampaignObjectManager), "UnregisterDeadHero").Invoke(objectManager, new object[] { hero });
        }

        private void AdjustEquipment(Hero hero)
        {
            AddCompanionModifiers(hero.BattleEquipment);
            AddCompanionModifiers(hero.CivilianEquipment);
        }

        private void AddCompanionModifiers(Equipment equipment)
		{
            //Downgrade the quality of the wanderer's equipment
			ItemModifier armorModifier = MBObjectManager.Instance.GetObject<ItemModifier>("companion_armor");
			ItemModifier weaponModifier = MBObjectManager.Instance.GetObject<ItemModifier>("companion_weapon");
			ItemModifier horseModifier = MBObjectManager.Instance.GetObject<ItemModifier>("companion_horse");
			for (EquipmentIndex equipmentIndex = EquipmentIndex.WeaponItemBeginSlot; equipmentIndex < EquipmentIndex.NumEquipmentSetSlots; equipmentIndex++)
			{
				EquipmentElement equipmentElement = equipment[equipmentIndex];
				if (equipmentElement.Item != null)
				{
					if (equipmentElement.Item.ArmorComponent != null)
					{
						equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, armorModifier, null, false);
					}
					else if (equipmentElement.Item.WeaponComponent != null)
					{
						equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, weaponModifier, null, false);
					}
					else if (equipmentElement.Item.HorseComponent != null)
					{
						equipment[equipmentIndex] = new EquipmentElement(equipmentElement.Item, horseModifier, null, false);
					}
				}
			}
		}

        private void AddToAliveCompanions(Hero hero)
        {
            _aliveCompanions.Add(hero);
        }

        private void RemoveFromAliveCompanions(Hero hero)
        {
            if(!_aliveCompanions.Remove(hero))
            {
                TORCommon.Log("TORCompanionCampaignBehavior : " + hero.Name + " was not present in the living companion cache and removal was attempted", NLog.LogLevel.Info);
            }
        }



        /* Things needed :
         * - storage for all of the companion templates
         * - cache for spawned companions (when they're to be replaced, we want to search just this type of hero)
         * - awareness of companions promoted to lord via vassalship
         * - ability to delete, clear out, and unregister outdated companions; nothing should be left behind related to them if they have nothing to contribute to the save file
         * - spawning for new companions to fill empty settlements, correct culture mismatches on settlement owndership change
         * - equipment verification and adjustments (do we want to reimplement the native companion modifier penalty?)
         * 
         * Goals :
         * - limited overhead, no wasting processing as this will become a larger and larger cost as the map grows
         * - towns only support wanderers of their culture
         * - the encyclopedia should contain no traces of wanderers that the player will never be able to find
         * - a variety of wanderers offered to avoid being pigeon-holed into a handful of choices
         * - governors not getting removed
         * - a wanderer recruited and placed as governor doesn't prevent the spawning of a new one
         */

        //nothing to save, everything is cached on campaign creation/load
        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}
