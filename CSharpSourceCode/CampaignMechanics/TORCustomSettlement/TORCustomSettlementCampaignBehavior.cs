using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CampaignMechanics.TORCustomSettlement.Component;
using TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;
using TOR_Core.Extensions;
using TOR_Core.Items;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement;

public class TORCustomSettlementCampaignBehavior : CampaignBehaviorBase
{
    private List<TORBaseSettlementMenuLogic> _customSettlementMenus;

    [SaveableField(0)] private Dictionary<string, bool> _customSettlementActiveStates = [];
    [SaveableField(1)] private Dictionary<string, int> _cursedSiteWardDurationLeft = [];
    [SaveableField(2)] private Dictionary<string, int> _lastGhostRecruitmentTime = [];
    [SaveableField(3)] private Dictionary<string, int> _lastDefileTime = [];
    [SaveableField(4)] private List<string> _unlockedOakUpgrades = [];
    [SaveableField(5)] private Dictionary<string, int> _lastPrayerTime = [];
    [SaveableField(6)] private Dictionary<string, int> _lastPrayerTroopRewardTime = [];


    //TODO: Randy - Expose to appropriate xml.
    public static int DEVOTION_FOLLOWER_TROOPS_MIN = 3;
    public static int DEVOTION_FOLLOWER_TROOPS_MAX = 7;
    public static int DEVOTION_DEVOTED_TROOPS_MIN = 7;
    public static int DEVOTION_DEVOTED_TROOPS_MAX = 11;
    public static int DEVOTION_FANATIC_TROOPS_MIN = 11;
    public static int DEVOTION_FANATIC_TROOPS_MAX = 15;

    public static int CURSED_SITE_MIN_TROOP_COUNT = 0;
    public static int CURSED_SITE_MAX_TROOP_COUNT = 4;

    private TORFaithModel _model;

    public static MBReadOnlyList<Settlement> AllCustomSettlements { get; private set; } = [];

    /// <summary>
    /// Returns the value if it exists, otherwise returns 0.
    /// </summary>
    /// <remarks>With a start date of Summer 13, 2502, the first day of the game is 210 201. 0 should be far enough in the past that regardless of changes to the defile cooldown, the player won't be blocked from defiling on campaign start.
    /// <para>Because this is called when the player enters a shrine, the previous implementation that would add CampaignTime.Now to the dictionary would be performed on the first time that a shrine was entered which would push the first defile in a campaign to (days until entered a shrine + defile cooldown).</para>
    /// <para>A specific number was chosen rather than using member accesses to get the campaign start date and the defile cooldown.</para>
    /// </remarks>
    public int LastDefileTime(Hero hero)
    {
        if (_lastDefileTime.TryGetValue(hero.StringId, out int value))
        {
            return value;
        }

        _lastDefileTime.Add(hero.StringId, 0);
        return _lastDefileTime[hero.StringId];
    }

    /// <remarks>
    /// If the hero has never bound wraiths, then the first entry for them is non-existent (or "far" in the past), 
    /// rather than the present day to avoid the player unable to bind wraiths the first time they enter a cursed site in a campaign.
    /// </remarks>
    public int LastGhostRecruitmentTime(Hero hero)
    {
        if (_lastGhostRecruitmentTime.TryGetValue(hero.StringId, out int value))
        {
            return value;
        }
        else
        {
            _lastGhostRecruitmentTime.Add(hero.StringId, 0);
            return _lastGhostRecruitmentTime[hero.StringId];
        }
    }

    public void SetLastDefileTime(Hero hero, int value)
    {
        _lastDefileTime.AddOrReplace(hero.StringId, value);
    }

    public void SetLastGhostRecruitmentTime(Hero hero, int value)
    {
        _lastGhostRecruitmentTime.AddOrReplace(hero.StringId, value);
    }

    public int LastPrayerTime(Hero hero)
    {
        if (_lastPrayerTime.TryGetValue(hero.StringId, out int value))
        {
            return value;
        }

        _lastPrayerTime.Add(hero.StringId, 0);
        return _lastPrayerTime[hero.StringId];
    }

    public void SetLastPrayerTime(Hero hero, int value)
    {
        _lastPrayerTime.AddOrReplace(hero.StringId, value);
    }
    public int LastPrayerTroopRewardTime(Hero hero)
    {
        if (_lastPrayerTroopRewardTime.TryGetValue(hero.StringId, out var lastPrayerTroopRewardTime))
        {
            return lastPrayerTroopRewardTime;
        }

        return -1;
    }

    public void SetLastPrayerTroopRewardTime(Hero hero, int time)
    {
        _lastPrayerTroopRewardTime.AddOrReplace(hero.StringId, time);
    }

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, AfterSessionLaunched);
        CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this, OnSettlementHourlyTick);
        CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
        CampaignEvents.TickPartialHourlyAiEvent.AddNonSerializedListener(this, OnAiTick);
        CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
        CampaignEvents.OnNewGameCreatedPartialFollowUpEndEvent.AddNonSerializedListener(this, OnNewGameStart);
        CampaignEvents.OnBeforeSaveEvent.AddNonSerializedListener(this, CollectSettlementData);
    }


    public void UnlockOakUpgrade(string unlockedUpgrade)
    {
        if (!_unlockedOakUpgrades.Contains(unlockedUpgrade))
        {
            _unlockedOakUpgrades.Add(unlockedUpgrade);
        }
    }

    public bool HasUnlockedOakUpgrade(string unlockedUpgrade)
    {
        return _unlockedOakUpgrades.Contains(unlockedUpgrade);
    }

    public List<string> GetUnlockedOakUpgradeCategory(string unlockedUpgradeCategory)
    {
        return _unlockedOakUpgrades.Where(x => x.StartsWith(unlockedUpgradeCategory)).ToList();
    }


    private void OnMissionEnded(IMission obj)
    {
        //Sly : can this be found from the settlement of the PlayerEncounter?
        var battleSettlement = Settlement.FindFirst(delegate (Settlement settlement)
        {
            {
                var comp = settlement.SettlementComponent as BaseRaiderSpawnerComponent;
                if (comp != null)
                {
                    return comp.IsBattleUnderway;
                }
            }

            return false;
        });

        if (battleSettlement != null)
        {
            var comp = battleSettlement.SettlementComponent as BaseRaiderSpawnerComponent;
            comp.IsBattleUnderway = false;
            var mission = obj as Mission;

            if (mission?.MissionResult != null && mission.MissionResult.BattleResolved && mission.MissionResult.PlayerVictory)
            {
                comp.IsActive = false;
                var list = new List<InquiryElement>();

                // no artifacts
                var artifactIds = ReligionObject.All?
                    .SelectMany(r => r.ReligiousArtifacts)
                    .Select(i => i.StringId)
                    .ToHashSet() ?? new HashSet<string>();

                var heroCulture = Hero.MainHero.Culture;

                var rewardItems = comp.RewardItemIds
                    .Select(id => MBObjectManager.Instance.GetObject<ItemObject>(id))
                    .Where(i => i != null)
                    .Where(i => !artifactIds.Contains(i.StringId))
                    .ToList();

                List<ItemObject> blueprintRewardItems = new();
                if (rewardItems.Count > 0)
                {
                    var rewardSelection = rewardItems;
                    if (rewardItems.Count > 2)
                    {
                        rewardSelection = rewardItems.TakeRandom(2).ToList();
                    }

                    blueprintRewardItems = rewardSelection
                        .Where(i => i.StringId.StartsWith("tor_learn_"))
                        .ToList();
                }

                const int totalRewardSlots = 4;
                const int cultureRewardSlots = 1;
                const int poolSlots = totalRewardSlots - cultureRewardSlots; // 3

                var equipmentSlots = poolSlots - blueprintRewardItems.Count;
                if (equipmentSlots < 0)
                {
                    equipmentSlots = 0;
                }

                var specificItems = comp.RewardItemIds
                    .Select(id => MBObjectManager.Instance.GetObject<ItemObject>(id))
                    .Where(i => i != null)
                    .Where(i => i.IsWeapon() || i.IsArmor() || i.ItemType == ItemObject.ItemTypeEnum.Shield)
                    .Where(i => !i.IsCraftedByPlayer)
                    .Where(i => !artifactIds.Contains(i.StringId))
                    .ToList();

                var cultureItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(i => i.IsTorItem()) // no vanilla
                    .Where(i => i.Culture == heroCulture)
                    .Where(i => (i.IsWeapon() || i.IsArmor() || i.ItemType == ItemObject.ItemTypeEnum.Shield))
                    .Where(i => !i.IsCraftedByPlayer)
                    .Where(i => !artifactIds.Contains(i.StringId))
                    .GroupBy(i => i.StringId)
                    .Select(g => g.First())
                    .ToList();

                var highTierCultureItems = cultureItems
                    .Where(i => i.Tier >= ItemObject.ItemTiers.Tier3)
                    .ToList();

                var cultureWeaponOrArmorItems = cultureItems
                    .Where(i => i.IsWeapon() || i.IsArmor())
                    .ToList();

                var equipmentItems = specificItems
                    .GroupBy(i => i.StringId)
                    .Select(g => g.First())
                    .OrderBy(_ => MBRandom.RandomFloat)
                    .Take(equipmentSlots)
                    .ToList();

                if (equipmentItems.Count < equipmentSlots)
                {
                    var remainingSlots = equipmentSlots - equipmentItems.Count;

                    var fillerItems = highTierCultureItems
                        .Where(i => equipmentItems.All(selected => selected.StringId != i.StringId))
                        .OrderBy(_ => MBRandom.RandomFloat)
                        .Take(remainingSlots)
                        .ToList();

                    if (fillerItems.Count < remainingSlots)
                    {
                        var remainingAfterHighTier = remainingSlots - fillerItems.Count;

                        var fallbackItems = cultureItems
                            .Where(i => equipmentItems.All(selected => selected.StringId != i.StringId))
                            .Where(i => fillerItems.All(selected => selected.StringId != i.StringId))
                            .OrderBy(_ => MBRandom.RandomFloat)
                            .Take(remainingAfterHighTier)
                            .ToList();

                        fillerItems.AddRange(fallbackItems);
                    }

                    equipmentItems.AddRange(fillerItems);

                }

                ItemObject cultureRewardItem = null;

                {
                    var excludedItemIds = new HashSet<string>(StringComparer.Ordinal);

                    foreach (var equipmentItem in equipmentItems)
                    {
                        excludedItemIds.Add(equipmentItem.StringId);
                    }

                    foreach (var blueprint in blueprintRewardItems)
                    {
                        excludedItemIds.Add(blueprint.StringId);
                    }

                    var cultureCandidates = cultureWeaponOrArmorItems
                        .Where(i => !i.NotMerchandise)
                        .Where(i => !excludedItemIds.Contains(i.StringId))
                        .ToList();

                    if (cultureCandidates.Count > 0)
                    {
                        var tier3 = cultureCandidates.Where(i => i.Tier == ItemObject.ItemTiers.Tier3).ToList();
                        var tier4 = cultureCandidates.Where(i => i.Tier == ItemObject.ItemTiers.Tier4).ToList();
                        var tier5 = cultureCandidates.Where(i => i.Tier == ItemObject.ItemTiers.Tier5).ToList();
                        var tier6 = cultureCandidates.Where(i => i.Tier == ItemObject.ItemTiers.Tier6).ToList();

                        var weightedTiers = new List<(float Weight, List<ItemObject> Items)>{(40f, tier3),(30f, tier4),(20f, tier5),(10f, tier6),}
                        .Where(t => t.Items.Count > 0)
                        .ToList();

                        if (weightedTiers.Count > 0)
                        {
                            var totalWeight = weightedTiers.Sum(t => t.Weight);
                            var roll = MBRandom.RandomFloat * totalWeight;

                            foreach (var entry in weightedTiers)
                            {
                                roll -= entry.Weight;
                                if (roll <= 0f)
                                {
                                    cultureRewardItem = entry.Items
                                        .OrderBy(_ => MBRandom.RandomFloat)
                                        .First();
                                    break;
                                }
                            }

                            cultureRewardItem ??= weightedTiers.Last().Items
                                .OrderBy(_ => MBRandom.RandomFloat)
                                .First();
                        }
                        else
                        {
                            cultureRewardItem = cultureCandidates
                                .OrderBy(_ => MBRandom.RandomFloat)
                                .First();
                        }
                    }
                }


                var items = new List<ItemObject>();
                items.AddRange(equipmentItems);
                if (cultureRewardItem != null)
                {
                    items.Add(cultureRewardItem);
                }

                foreach (var blueprint in blueprintRewardItems)
                {
                    if (!items.Contains(blueprint))
                    {
                        items.Add(blueprint);
                    }
                }

                items = items
                    .OrderBy(_ => MBRandom.RandomFloat)
                    .ToList();

                var model = (TORBattleRewardModel)Campaign.Current.Models.BattleRewardModel;

                var unmodifiedItems = new List<ItemObject>(items.Count);
                var enchantedItems = new List<ItemObject>();

                foreach (var item in items)
                {
                    var isEquipmentItem =
                        item.IsWeapon() ||
                        item.IsArmor() ||
                        item.ItemType == ItemObject.ItemTypeEnum.Shield;

                    var canReceiveTraits = isEquipmentItem && !item.HasAnyTrait();
                    if (!canReceiveTraits)
                    {
                        unmodifiedItems.Add(item);
                        continue;
                    }

                    var possibleTraits = ItemTrait.All
                        .Where(x => x.ItemTraitStringId.Contains("lesser_loot"))
                        .ToList();

                    if (item.IsArmor())
                    {
                        possibleTraits = possibleTraits
                            .Where(x => x.ValidItemType == ItemTraitItemType.Armor)
                            .ToList();
                    }
                    else
                    {
                        var validTypes = new HashSet<ItemTraitItemType>();

                        if (item.ItemType == ItemObject.ItemTypeEnum.Shield)
                        {
                            validTypes.Add(ItemTraitItemType.Shield);
                        }
                        else
                        {
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

                                    if (primaryWeapon.IsRangedWeapon && primaryWeapon.IsConsumable) // throwables
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
                        }

                        possibleTraits = possibleTraits
                            .Where(x => validTypes.Contains(x.ValidItemType))
                            .ToList();
                    }

                    var chosenTrait = possibleTraits.TakeRandom(1).FirstOrDefault();
                    if (chosenTrait == null)
                    {
                        unmodifiedItems.Add(item);
                        continue;
                    }

                    var ids = new List<string> { chosenTrait.ItemTraitStringId };
                    var name = model.GetNameModifierForTraits(1);
                    var newItem = EnchantmentHelper.CreateEnchantedItem(item, ids, name + " " + item.Name, false);

                    enchantedItems.Add(newItem);
                }

                unmodifiedItems.AddRange(enchantedItems);
                items = unmodifiedItems;


                var ingredientsBehavior = Campaign.Current.GetCampaignBehavior<EnchantmentIngredientLootCampaignBehavior>();
                if (ingredientsBehavior != null)
                {
                    items.Add(ingredientsBehavior.GetDungeonLootIngredient);
                }

                foreach (var item in items)
                {
                    var traits = item.GetTraits();
                    var text = new MBStringBuilder();
                    text.Initialize();

                    var hintInfo = item.GetTorSpecificDataReadOnly();
                    if (hintInfo != null && !hintInfo.Description.IsEmpty())
                    {
                        text.AppendLine(hintInfo.Description);
                    }

                    foreach (var trait in traits)
                    {
                        text.AppendLine(trait.ItemTraitDescription);
                    }

                    list.Add(new InquiryElement(item, item.Name.ToString(), new ItemImageIdentifier(item), true, text.ToStringAndRelease()));
                }

                var inq = new MultiSelectionInquiryData(
                    TORTextHelper.GetText("tor_custom_settlement_chaos_portal_victory_title", "Victory!"),
                    TORTextHelper.GetText("tor_custom_settlement_chaos_portal_victory_str", "You are Victorious! Claim your reward! Select one!"),
                    list,
                    false,
                    1,
                    1,
                    TORTextHelper.GetText("tor_inquiry_ok_text", "OK"),
                    null,
                    OnRewardClaimed,
                    null);

                MBInformationManager.ShowMultiSelectionInquiry(inq);
            }
            else
            {
                var inq = new InquiryData(TORTextHelper.GetText("tor_custom_settlement_chaos_portal_defeat_title", "Defeated!"), TORTextHelper.GetText("tor_custom_settlement_chaos_portal_lose", "The enemy proved more than a match for you. Better luck next time!"), true, false, TORTextHelper.GetText("tor_inquiry_ok_text", "OK"), null, null, null);
                InformationManager.ShowInquiry(inq);
            }
        }
    }


    private void OnRewardClaimed(List<InquiryElement> obj)
    {
        var item = obj[0].Identifier as ItemObject;
        var count = 1;
        var ingredientsBehavior = Campaign.Current.GetCampaignBehavior<EnchantmentIngredientLootCampaignBehavior>();
        if (ingredientsBehavior != null)
        {
            if (ingredientsBehavior.GetDungeonLootIngredient == item)
            {
                count = 5;
            }
        }

        Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, count);
    }

    private void CollectSettlementData()
    {
        var customSettlements = Settlement.FindAll(x => x.SettlementComponent is TORBaseSettlementComponent);
        AllCustomSettlements = new MBReadOnlyList<Settlement>(customSettlements);
        foreach (var settlement in customSettlements)
        {
            var comp = settlement.SettlementComponent as TORBaseSettlementComponent;
            _customSettlementActiveStates[settlement.StringId] = comp.IsActive;

            if (comp is CursedSiteComponent cursedSite)
            {
                _cursedSiteWardDurationLeft[settlement.StringId] = cursedSite.WardHours;
            }
        }
    }

    private void OnNewGameStart(CampaignGameStarter starter)
    {
        var customSettlements = Settlement.FindAll(x => x.SettlementComponent is TORBaseSettlementComponent);
        AllCustomSettlements = new MBReadOnlyList<Settlement>(customSettlements);
        foreach (var settlement in customSettlements)
        {
            var comp = settlement.SettlementComponent as TORBaseSettlementComponent;
            comp.IsActive = true;
        }
    }
   
    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero leaderHero)
    {
        var settleComp = settlement.SettlementComponent;
        if (party == null || leaderHero == null || party == MobileParty.MainParty) return;

        switch (settleComp) { 
            case ShrineComponent shrine:
                party.AddBlessingToParty(shrine.Religion.StringId);
                ShrineAIRecruitment(party, settlement, shrine);
                break;
            case CursedSiteComponent:
                CursedSiteAIRecruitment(party, settlement);
                break;
            case null:
                return;
        };


        LeaveSettlementAction.ApplyForParty(party);

        if (party.Army == null || party.Army.LeaderParty == party)
            //unsure what happens if all of the attached parties in an army are set to start thinking;
            //player-facing issue only as AI armies won't try to visit shrines
        {
            party.SetMoveModeHold();
            party.Ai.SetDoNotMakeNewDecisions(false);
            party.Ai.RethinkAtNextHourlyTick = true;
        }
        
    }

    private void CursedSiteAIRecruitment(MobileParty party, Settlement settlement)
    {
        var leaderHero = party.LeaderHero;

        if (!leaderHero.IsNecromancer() && !leaderHero.IsVampire()) return;

        var freeSlots = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
        if (freeSlots <= 0) return;

        var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_spirit_host");
        int raisePower = Math.Max(1, (int)leaderHero.GetExtendedInfo().SpellCastingLevel);
        var count = MBRandom.RandomInt(CURSED_SITE_MIN_TROOP_COUNT, CURSED_SITE_MAX_TROOP_COUNT);
        count *= raisePower;
        if (freeSlots < count) count = freeSlots;
        party.MemberRoster.AddToCounts(troop, count);
        CampaignEventDispatcher.Instance.OnTroopRecruited(leaderHero, settlement, null, troop, count);
        Campaign.Current.GetCampaignBehavior<TORCustomSettlementCampaignBehavior>().SetLastGhostRecruitmentTime(leaderHero, (int)CampaignTime.Now.ToDays);
    }

    private void ShrineAIRecruitment(MobileParty party, Settlement settlement, ShrineComponent shrine)
    {
        var leaderHero = party.LeaderHero;

        if (shrine.Religion.ReligiousTroops == null || shrine.Religion.ReligiousTroops.Count <= 0)
        {
            return;
        }

        var heroReligion = leaderHero.GetDominantReligion();

        if (heroReligion != shrine.Religion)
        {
            return;
        }

        var freeSlots = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
        if (freeSlots <= 0)
        {
            return;
        }

        var troop = shrine.Religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
        if (troop == null)
        {
            return;
        }

        var devotion = leaderHero.GetDevotionLevelForReligion(heroReligion);
        int troopCount = GetTroopCountByDevotion(devotion);
        if (troopCount > 0)
        {
            if (freeSlots < troopCount) troopCount = freeSlots;
            party.MemberRoster.AddToCounts(troop, troopCount);
            CampaignEventDispatcher.Instance.OnTroopRecruited(leaderHero, settlement, null, troop, troopCount);
        }
    }

    private static int GetTroopCountByDevotion(DevotionLevel devotionLevel)
    {
        return devotionLevel switch
        {
            DevotionLevel.Follower => MBRandom.RandomInt(DEVOTION_FOLLOWER_TROOPS_MIN, DEVOTION_FOLLOWER_TROOPS_MAX),
            DevotionLevel.Devoted => MBRandom.RandomInt(DEVOTION_DEVOTED_TROOPS_MIN, DEVOTION_DEVOTED_TROOPS_MAX),
            DevotionLevel.Fanatic => MBRandom.RandomInt(DEVOTION_FANATIC_TROOPS_MIN, DEVOTION_FANATIC_TROOPS_MAX),
            _ => 0, // No troops for Skeptic/Believer
        };
    }


    private void OnAiTick(MobileParty party)
    {
        if (!party.IsLordParty || party.LeaderHero == null || party == MobileParty.MainParty) return;
        if (ShrineMenuLogic.CanPartyGoToShrine(party))
        {
            var leaderReligion = party.LeaderHero.GetDominantReligion();
            if (leaderReligion != null)
            {
                var shrineSettlement = TORCommon.FindSettlementsAroundPosition(
                    party.Position.ToVec2(),
                    30,
                    x => x.SettlementComponent is ShrineComponent shrineComponent && shrineComponent.Religion == leaderReligion)
                    .OrderBy(x => x.Position.DistanceSquared(party.Position))
                    .FirstOrDefault();
                if (shrineSettlement != null)
                {
                    party.SetMoveGoToSettlement(shrineSettlement, MobileParty.NavigationType.Default, false);
                    party.Ai.SetDoNotMakeNewDecisions(true);
                }
            }
        }

        if (CursedSiteMenuLogic.CanPartyRecruitGhosts(party))
        {
            if (!_lastGhostRecruitmentTime.ContainsKey(party.LeaderHero.StringId) || _lastGhostRecruitmentTime[party.LeaderHero.StringId] + CursedSiteMenuLogic.MinimumDaysBetweenRaisingGhosts < (int)CampaignTime.Now.ToDays)
            {
                var settlements = TORCommon.FindSettlementsAroundPosition(party.Position.ToVec2(), 20, x => x.SettlementComponent is CursedSiteComponent);
                if (settlements.Count > 0)
                {
                    party.SetMoveGoToSettlement(settlements.First(), MobileParty.NavigationType.Default, false);
                    party.Ai.SetDoNotMakeNewDecisions(true);
                }
            }
        }
    }

    private void AfterSessionLaunched(CampaignGameStarter obj)
    {
        var castleMenu = Campaign.Current.GameMenuManager.GetGameMenu("castle");
        TORSettlementMenuHelpers.RearrangeTownMenus(castleMenu, "town_bloodkeep_hire", "castle_prison");
    }

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        _model = Campaign.Current.Models.GetFaithModel();

        _customSettlementMenus = new List<TORBaseSettlementMenuLogic>()
        {
            new ShrineMenuLogic(starter),
            new CursedSiteMenuLogic(starter),
            new RaidingSiteMenuLogic(starter),
            new OakOfAgesMenuLogic(starter),
            new TrollCaveMenuLogic(starter)
        };

        AddBloodKeepHiringButton(starter);

        foreach (var entry in _customSettlementActiveStates)
        {
            var settlement = Settlement.Find(entry.Key);
            if (settlement != null && settlement.SettlementComponent is TORBaseSettlementComponent)
            {
                var comp = settlement.SettlementComponent as TORBaseSettlementComponent;
                comp.IsActive = entry.Value;
                if (comp is CursedSiteComponent cursedSite && _cursedSiteWardDurationLeft.ContainsKey(settlement.StringId))
                {
                    cursedSite.WardHours = _cursedSiteWardDurationLeft[entry.Key];
                }
            }
        }

        CollectSettlementData();
    }

    private void OnSettlementHourlyTick(Settlement settlement)
    {
        if (settlement.SettlementComponent is CursedSiteComponent)
        {
            var site = settlement.SettlementComponent as CursedSiteComponent;
            if (site.IsActive)
            {

                var affectedParties = TORCommon.FindPartiesAroundPosition(settlement.Position.ToVec2(), TORConstants.DEFAULT_CURSE_RADIUS, x => (x.IsLordParty && x.LeaderHero != null && x.LeaderHero.GetDominantReligion() != site.Religion) && (x.IsLordParty && x.LeaderHero != null && x.LeaderHero.Culture.StringId != "mousillon"));

                if (affectedParties.Contains(MobileParty.MainParty))
                {
                    if (MobileParty.MainParty.LeaderHero.IsEnlisted())
                        affectedParties.Remove(MobileParty.MainParty);
                }

                foreach (var party in affectedParties)
                {
                    if (party.IsActive && !party.IsDisbanding && party.MapEvent == null && party.BesiegedSettlement == null && party.CurrentSettlement == null)
                    {
                        if (party.MemberRoster.TotalHealthyCount > party.MemberRoster.TotalManCount * 0.25f)
                        {
                            party.MemberRoster.WoundNumberOfNonHeroTroopsRandomly((int)Math.Ceiling(party.MemberRoster.TotalHealthyCount * (_model.CalculateCursedRegionDamagePerHour(party) / 100f)));
                        }

                        foreach (var hero in party.GetMemberHeroes())
                        {
                            if (hero.HitPoints > 25 && hero.HitPoints <= hero.MaxHitPoints)
                            {
                                hero.HitPoints -= _model.CalculateCursedRegionDamagePerHour(party);
                            }
                        }
                    }
                }
            }
            else site.HourlyTick();
        }
    }

    private void AddBloodKeepHiringButton(CampaignGameStarter starter)
    {
        starter.AddGameMenuOption("castle", "town_bloodkeep_hire",
            GameTexts.FindText("tor_bloodkeep_hire_start").ToString(),
            args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
                bool canHire = CanHireBloodKnights();
                bool shouldBeDisabled = ShouldBeDisabledBloodKeep(canHire, out TextObject disableReason);

                bool isHireling = Hero.MainHero.IsEnlisted();
                if (canHire && isHireling)
                {
                    var hirelingDisableReason = GameTexts.FindText("tor_bloodkeep_hire_disabled_hireling");
                    return MenuHelper.SetOptionProperties(args, false, true, hirelingDisableReason);
                }

                if (shouldBeDisabled)
                {
                    canHire = false;
                }
                return MenuHelper.SetOptionProperties(args, canHire, shouldBeDisabled, disableReason ?? TextObject.GetEmpty());
            },
            args =>
            {
                HireBloodKnights();
            },
            false, 7, false, null);
    }

    private bool ShouldBeDisabledBloodKeep(bool canHire, out TextObject disableReason)
    {
        disableReason = null;

        if (!Hero.MainHero.IsVampire())
        {
            return false; // Button should not appear at all
        }

        if (!canHire)
        {
            return false; // Already handled by CanHireBloodKnights
        }

        const int darkEnergyCost = 100;
        const int minBloodKnights = 3;

        // Check if player has enough dark energy
        var darkEnergy = Hero.MainHero.GetCustomResourceValue("DarkEnergy");
        if (darkEnergy < darkEnergyCost)
        {
            var disableText = GameTexts.FindText("tor_bloodkeep_hire_disabled_energy");
            disableText.SetTextVariable("ENERGY_COST", darkEnergyCost);
            disableText.SetTextVariable("CURRENT_ENERGY", (int)darkEnergy);
            disableReason = disableText;
            return true;
        }

        // Check if player has enough party space
        var availableSpace = Hero.MainHero.PartyBelongedTo.Party.PartySizeLimit -
                            Hero.MainHero.PartyBelongedTo.MemberRoster.TotalManCount;
        if (availableSpace < minBloodKnights)
        {
            var disableText = GameTexts.FindText("tor_bloodkeep_hire_disabled_space");
            disableText.SetTextVariable("MIN_KNIGHTS", minBloodKnights);
            disableReason = disableText;
            return true;
        }

        return false;
    }

    private bool CanHireBloodKnights()
    {
        // Player must be a vampire
        if (!Hero.MainHero.IsVampire())
        {
            return false;
        }

        // Must be at the Blood Keep
        if (Settlement.CurrentSettlement == null || !Settlement.CurrentSettlement.IsBloodKeep())
        {
            return false;
        }

        // Blood Keep must be owned by vampires
        var owner = Settlement.CurrentSettlement.Owner;
        if (owner == null || owner.Culture == null)
        {
            return false;
        }

        var ownerCulture = owner.Culture.StringId;
        if (ownerCulture != TORConstants.Cultures.SYLVANIA && ownerCulture != TORConstants.Cultures.MOUSILLON)
        {
            return false;
        }

        return true;
    }

    private void HireBloodKnights()
    {
        const int darkEnergyCost = 100;
        const int maxBloodKnights = 5;
        const string bloodKnightTroopId = "tor_bd_blooddragon_knight";
        const string knightIntiateTroopId = "tor_bd_blooddragon_initiate";
        // Deduct dark energy
        Hero.MainHero.AddCustomResource("DarkEnergy", -darkEnergyCost);

        // Calculate number of Blood Knights to recruit (3-5)
        var knightCount = 0;

        for (int i = 0; i < maxBloodKnights; i++)
        {
            if (MBRandom.RandomFloat < 0.1f)
            {
                knightCount++;
            }
        }

        var knightInitiates = maxBloodKnights-knightCount; 

        // Check available party space
        var availableSpace = Hero.MainHero.PartyBelongedTo.Party.PartySizeLimit -
                            Hero.MainHero.PartyBelongedTo.MemberRoster.TotalManCount;
        knightCount = Math.Min(knightCount, availableSpace);
        knightInitiates = Math.Min(knightInitiates, availableSpace);

        // Add Blood Knights to party
        var bloodKnight = MBObjectManager.Instance.GetObject<CharacterObject>(bloodKnightTroopId);
        var knightInitates = MBObjectManager.Instance.GetObject<CharacterObject>(knightIntiateTroopId);

        if (bloodKnight != null && knightCount > 0)
        {
            Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(bloodKnight, knightInitiates);
        }
        
        if (knightInitates != null && knightInitiates > 0)
        {
            Hero.MainHero.PartyBelongedTo.MemberRoster.AddToCounts(knightInitates, knightInitiates);
        }
        // Show success message
        var successText = GameTexts.FindText("tor_bloodkeep_hire_success");
        successText.SetTextVariable("KNIGHT_COUNT", maxBloodKnights);
        successText.SetTextVariable("ENERGY_COST", darkEnergyCost);
        successText.SetTextVariable("CR_ICON", Hero.MainHero.GetCultureSpecificCustomResource().GetCustomResourceIconAsText());

        InformationManager.DisplayMessage(new InformationMessage(successText.ToString()));
        // Return to town menu
        GameMenu.SwitchToMenu("castle");
    }

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_customSettlementActiveStates", ref _customSettlementActiveStates);
        dataStore.SyncData("_cursedSiteWardDurationLeft", ref _cursedSiteWardDurationLeft);
        dataStore.SyncData("_lastGhostRecruitmentTime", ref _lastGhostRecruitmentTime);
        dataStore.SyncData("_unlockedOakUpgrades", ref _unlockedOakUpgrades);
        dataStore.SyncData("_lastPrayerTime", ref _lastPrayerTime);
    }


}