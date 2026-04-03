
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Crafting;
/// <summary>
/// Adds findable Items with item traits to the lootable pool. Also removes items that are currently not directly or indirectly (via companions) used by the player.
/// </summary>
public class LootCampaignBehavior : CampaignBehaviorBase
{
    protected readonly Dictionary<CharacterObject, int> _initialEnemyArmy = new();

    public override void RegisterEvents()
    {
        CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, AddMagicalItemsFromBattle);
        CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, RemovedUnusedLootItems); //slightly less often than on map event end, but skips having to check lots of map events
        CampaignEvents.OnMissionStartedEvent.AddNonSerializedListener(this, StoreInitialArmy);
    }


    private void StoreInitialArmy(IMission obj)
    {
        var playerEvent = Campaign.Current.MainParty.MapEvent;

        var enemySide = BattleSideEnum.Attacker;
        if (playerEvent == null)
            return;
        if (playerEvent.PlayerSide == BattleSideEnum.Attacker) enemySide = BattleSideEnum.Defender;


        var side = playerEvent.GetMapEventSide(enemySide);

        foreach (var characterObject in from enemyParties in side.Parties from troop in enemyParties.Troops select troop.Troop) //flattened rosters unstack all of the troops to create a list with length equal to the number of members - is this better to go MapEventParty.PartyBase.MemberRoster and iterate through the stacked list which stores CO + amount?
        {
            if (_initialEnemyArmy.TryGetValue(characterObject, out var count))
                _initialEnemyArmy[characterObject] = count + 1;
            else
                _initialEnemyArmy.Add(characterObject, 1);
        }
    }

    private void RemovedUnusedLootItems()
    {
        var objects = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x => x.HasAnyLootTraits()).ToMBList();

        var toRemove = new List<ItemObject>();


        var settlements = new List<Settlement>();
        //creating a new MBReadOnlyList and assigning it the value from the hero's clan was creating a reference copy which allowed settlements.Add to add the settlement with any of the player's workshops to the list which then transfered back to the property which was used in generating the list of settlements belonging to the player in the ClanManagementVM. When loading a save, the list would then read the empty cache and display the correct value until this started adding the settlement again on each pass through.
        if (Hero.MainHero.IsKingdomLeader)
        {
            settlements = Hero.MainHero.Clan.Kingdom.Settlements.ToList();
        }
        else
        {
            settlements = Hero.MainHero.Clan.Settlements.ToList();
        }

        foreach (var workshop in Hero.MainHero.OwnedWorkshops)
        {
            settlements.Add(workshop.Settlement);
        }

        settlements = settlements.Distinct().ToMBList();

        foreach (var item in objects)
        {
            var found = settlements.AnyQ(settlement => settlement.ItemRoster.AnyQ(x => x.EquipmentElement.Item == item));

            var heroes = Hero.MainHero.IsKingdomLeader ? Hero.MainHero.Clan.Kingdom.Heroes : Hero.MainHero.Clan.Heroes; //Sly : is the goal of the kingdom usage to allow player companions that were promoted to vassals to keep their equipment that may contain loot traits? Code isn't set up to give them those benefits on campaign map/in battle so those would be useless enchantments. If this was being left in long term, part of this block should be in an event triggered by promoting a companion to vassal which swaps their enchanted equipment for the base non-magical equivalent and allows the ObjectManager to delete the now unused ItemObject.

            //if the goal is to remove lesser enchanted items from mobileParty item rosters later, why avoid doing that due to the player's clan/kingdom parties having the item? They could instead all be removed; those parties can't do anything with the item, it can be removed and unregistered.
            if ((MobileParty.MainParty?.ItemRoster.AnyQ(x => x.EquipmentElement.Item == item)) == true)
            {
                found = true;
            }


            //why only check their armour?
            if (heroes.Where(hero => hero.IsActive).Any(hero => hero.CharacterObject.GetCharacterEquipment(EquipmentIndex.ArmorItemBeginSlot, EquipmentIndex.HorseHarness)
                    .AnyQ(x => x == item)))
            {
                found = true;
            }

            if (!found)
            {
                toRemove.Add(item);
            }
        }

        foreach (var item in toRemove)
        {
            foreach (var settlement in Settlement.All)
            {
                if (!settlement.ItemRoster.AnyQ(x => x.EquipmentElement.Item == item)) continue;
                {
                    var count = settlement.ItemRoster.Count(x => x.EquipmentElement.Item == item);
                    settlement.ItemRoster.Remove(new ItemRosterElement(item));
                }
            }

            foreach (var party in MobileParty.AllLordParties.WhereQ(x => x.ActualClan != null && x.ActualClan != Clan.PlayerClan))
            {
                if (!party.ItemRoster.AnyQ(x => x.EquipmentElement.Item == item)) continue; //Sly : this shouldn't be possible without mods as any loot "acquired" by ai parties is ignored and the equivalent gold value is added to the leader's gold immediately
                {
                    party.ItemRoster.Remove(new ItemRosterElement(item));
                }
            }

            MBObjectManager.Instance.UnregisterObject(item);
        }
    }

    /// <summary>
    /// Calculates and adds a Magical item from a given map event. A fitting trait from a loot pool of the party is considered and a suitable enchantment (prefix : lesser loot is added
    /// </summary>
    /// <param name="mapEvent"></param>
    private void AddMagicalItemsFromBattle(MapEvent mapEvent)
    {
        if (Hero.MainHero.IsEnlisted())
        {
            _initialEnemyArmy.Clear();
            return;
        }

        if (mapEvent.PlayerSide != mapEvent.WinningSide) return; //player dying and their troops retreating triggers a PlayerBattleEndEvent with no winner; no point in calculating this for losses


        float renownChange, influenceChange, moraleChange, goldChange, playerEarnedLootPercentage;
        mapEvent.GetBattleRewards(PartyBase.MainParty, out renownChange, out influenceChange, out moraleChange, out goldChange,
            out playerEarnedLootPercentage);

        var itemRosterToReceive = PlayerEncounter.Current.RosterToReceiveLootItems;
        var enemySide = mapEvent.PlayerSide == BattleSideEnum.Attacker ? BattleSideEnum.Defender : BattleSideEnum.Attacker;
        var model = (TORBattleRewardModel)Campaign.Current.Models.BattleRewardModel;

        foreach (var element in _initialEnemyArmy)
        {
            var character = element.Key;
            var chance = model.DropChanceForMagicalItemsLoot(character, element.Value, playerEarnedLootPercentage);
            if (MBRandom.RandomFloatRanged(0, 1) > chance)
            {
                continue;
            }

            if (character.HeroObject != null &&
                character.HeroObject.HasAttribute("LegendaryLord"))
                continue; //TODO check in Unit Catalog that all Legendary Lord have this attribute.


            var traitCount = model.GetTraitCountForTroops(character, element.Value, playerEarnedLootPercentage);

            if (traitCount <= 0)
                continue;

            var traitList = new List<string>();
            var item = character.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Cape).Where(x => !x.IsBannerItem()).TakeRandom(1).FirstOrDefault();
            for (var j = 0; j < traitCount; j++)
                if (item != null)
                {
                    var traits = ItemTrait.All.Where(x => x.ItemTraitStringId.Contains("lesser_loot"));


                    if (item.IsArmor())
                    {
                        traits = traits.Where(x => x.ValidItemType == ItemTraitItemType.Armor).ToList();
                    }
                    else
                    {
                        //ammo/shields having weapon traits
                        var validTypes = new HashSet<ItemTraitItemType>();

                        if (item.HasWeaponComponent && item.PrimaryWeapon != null)
                        {
                            var primaryWeapon = item.PrimaryWeapon;

                            if (primaryWeapon.IsAmmo)
                            {
                                validTypes.Add(ItemTraitItemType.Ammo);
                            }
                            else if (primaryWeapon.IsShield || item.ItemType == ItemObject.ItemTypeEnum.Shield)
                            {
                                validTypes.Add(ItemTraitItemType.Shield);
                            }
                            else
                            {
                                validTypes.Add(ItemTraitItemType.Weapon);

                                if (primaryWeapon.IsMeleeWeapon)
                                    validTypes.Add(ItemTraitItemType.Melee);

                                if (primaryWeapon.IsRangedWeapon)
                                    validTypes.Add(ItemTraitItemType.Ranged);

                                if (primaryWeapon.IsRangedWeapon && primaryWeapon.IsConsumable) //throwables
                                    validTypes.Add(ItemTraitItemType.Thrown);
                            }
                        }
                        else if (item.ItemType == ItemObject.ItemTypeEnum.Shield)
                        {
                            validTypes.Add(ItemTraitItemType.Shield);
                        }
                        else
                        {
                            validTypes.Add(ItemTraitItemType.Weapon);
                        }

                        traits = traits.Where(x => validTypes.Contains(x.ValidItemType)).ToList();
                    }
                    traits = traits.Where(x => !traitList.Contains(x.ItemTraitStringId)).ToList();
                    var trait = traits.TakeRandom(1).FirstOrDefault(); //Sly : could do TakeRandom(traitCount) and skip iteration

                    if (trait == null)
                        continue;

                    traitList.Add(trait.ItemTraitStringId);
                }
            if (traitList.Count == 0)
            {
                continue;
            }
            var nameModifier = model.GetNameModifierForTraits(traitList.Count);
            GameTexts.SetVariable("NAMEMODIFIER", nameModifier);
            GameTexts.SetVariable("NAMEOFITEM", item.Name);

            var defaultName = item.Name + ", " + nameModifier;
            var name = TORTextHelper.GetTextObject("tor_magical_items_trait_nameComposition", defaultName);

            var magicItem = EnchantmentHelper.CreateEnchantedItem(item, traitList, name.ToString(), false);

            itemRosterToReceive.Add(new ItemRosterElement(magicItem, 1));
        }

        _initialEnemyArmy.Clear();
    }

    public override void SyncData(IDataStore dataStore)
    {

    }
}