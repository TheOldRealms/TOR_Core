using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
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
using TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
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

    private TORFaithModel _model;
    private static HashSet<string> _xmlItemIds;

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
    /// If the hero has never bound wraiths, then the first entry for them is non-existent (or "far" in the past), rather than the present day to avoid the player unable to bind wraiths the first time they enter a cursed site in a campaign.
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

    public override void RegisterEvents()
    {
        CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
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
                    .Where(i => _xmlItemIds.Contains(i.StringId))
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

                var specificItems = comp.RewardItemIds
                    .Select(id => MBObjectManager.Instance.GetObject<ItemObject>(id))
                    .Where(i => i != null)
                    .Where(i => _xmlItemIds.Contains(i.StringId))
                    .Where(i => i.IsTorItem()) // no vanilla
                    .Where(i => (i.IsWeapon() || i.IsArmor()))
                    .Where(i => i.Culture == heroCulture)
                    .Where(i => !i.IsCraftedByPlayer)
                    .Where(i => !artifactIds.Contains(i.StringId));

                var cultureItems = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                    .Where(i => _xmlItemIds.Contains(i.StringId))
                    .Where(i => i.IsTorItem()) // no vanilla
                    .Where(i => i.Culture == heroCulture && (i.IsWeapon() || i.IsArmor()))
                    .Where(i => !i.IsCraftedByPlayer)
                    .Where(i => !artifactIds.Contains(i.StringId));

                var equipmentPool = specificItems
                    .Concat(cultureItems)
                    .GroupBy(i => i.StringId)
                    .Select(g => g.First())
                    .ToList();

                const int maxEquipmentSlots = 4;
                var equipmentSlots = maxEquipmentSlots - blueprintRewardItems.Count;
                if (equipmentSlots < 0)
                {
                    equipmentSlots = 0;
                }

                var equipmentItems = equipmentPool
                    .OrderBy(_ => MBRandom.RandomFloat)
                    .Take(equipmentSlots)
                    .ToList();

                var items = new List<ItemObject>();
                items.AddRange(equipmentItems);

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

                var newItems = new List<ItemObject>(items);
                foreach (var item in items)
                {
                    if (!item.IsWeapon() && (!item.IsArmor() || item.HasAnyTrait()))
                        continue;

                    var traitCount = MBRandom.RandomInt(0, model.MaximumFindableTraitsOnItems());
                    if (traitCount <= 0)
                        continue;

                    var traits = ItemTrait.All
                        .Where(x => x.ItemTraitStringId.Contains("lesser_loot") && ItemTrait.IsValidFor(x, item.ItemType))
                        .TakeRandom(traitCount);

                    var ids = traits.Select(x => x.ItemTraitStringId).ToList();
                    var name = model.GetNameModifierForTraits(traitCount);
                    var newItem = EnchantmentHelper.CreateEnchantedItem(item, ids, name + " " + item.Name, false);

                    newItems.Add(newItem);
                    newItems.Remove(item);
                }
                items = newItems;

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

                    var hintInfo = item.GetTorSpecificData();
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
                    "Victory!",
                    new TextObject("{=tor_custom_settlement_chaos_portal_victory_str}You are Victorious! Claim your reward! Select one!").ToString(),
                    list,
                    false,
                    1,
                    1,
                    "OK",
                    null,
                    OnRewardClaimed,
                    null);

                MBInformationManager.ShowMultiSelectionInquiry(inq);
            }
            else
            {
                var inq = new InquiryData("Defeated!", new TextObject("{=tor_custom_settlement_chaos_portal_lose_str}The enemy proved more than a match for you. Better luck next time!").ToString(), true, false, "OK", null, null, null);
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
        foreach (var settlement in customSettlements)
        {
            var comp = settlement.SettlementComponent as TORBaseSettlementComponent;
            comp.IsActive = true;
        }
    }

    //Sly : this should be transfered over into the relevant settlement component's OnPartyEntered override method; we can perform this without needing to evaluate every party entering every settlement. See hideouts in native for an example.
    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero leaderHero)
    {
        var settleComp = settlement.SettlementComponent;
        if (party == null || leaderHero == null || party == MobileParty.MainParty) return;
        if (settleComp is not ShrineComponent && settleComp is not CursedSiteComponent) return;
        if (settleComp is ShrineComponent shrine)
        {
            //with no check for the religion of the hero, a dedicated player can take nobles into their army and slowly change their religion by taking them to shrines that will give influence from a different religion than their current dominant one
            party.AddBlessingToParty(shrine.Religion.StringId);
        }
        else if (settleComp is CursedSiteComponent)
        {
            //no check would have parties in a player's army (who's attempting to deactivate the site) attempt to recruit wraiths who would then be culturally converted to an equivalent if one exists, or crash in some cases
            if (leaderHero.IsNecromancer() || leaderHero.IsVampire())
            {
                var freeSlots = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
                if (freeSlots > 0)
                {
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_spirit_host");
                    int raisePower = Math.Max(1, (int)leaderHero.GetExtendedInfo().SpellCastingLevel);
                    var count = MBRandom.RandomInt(0, 4);
                    count *= raisePower;
                    if (freeSlots < count) count = freeSlots;
                    party.MemberRoster.AddToCounts(troop, count);
                    CampaignEventDispatcher.Instance.OnTroopRecruited(party.LeaderHero, settlement, null, troop, count);

                    if (_lastGhostRecruitmentTime.ContainsKey(party.LeaderHero.StringId))
                    {
                        _lastGhostRecruitmentTime[party.LeaderHero.StringId] = (int)CampaignTime.Now.ToDays;
                    }
                    else
                    {
                        _lastGhostRecruitmentTime.Add(party.LeaderHero.StringId, (int)CampaignTime.Now.ToDays);
                    }
                }
            }
        }

        LeaveSettlementAction.ApplyForParty(party);

        if (party.Army == null || party.Army.LeaderParty == party)//unsure what happens if all of the attached parties in an army are set to start thinking; player-facing issue only as AI armies won't try to visit shrines
        {
            party.SetMoveModeHold();
            party.Ai.SetDoNotMakeNewDecisions(false);
            party.Ai.RethinkAtNextHourlyTick = true;
        }
    }

    private void OnAiTick(MobileParty party)
    {
        if (!party.IsLordParty || party.LeaderHero == null || party == MobileParty.MainParty) return;
        if (ShrineMenuLogic.CanPartyGoToShrine(party))
        {
            var settlements = TORCommon.FindSettlementsAroundPosition(party.Position.ToVec2(), 20, x => x.SettlementComponent is ShrineComponent);
            if (settlements.Count > 0)
            {
                var shrine = settlements.First().SettlementComponent as ShrineComponent;
                if (party.LeaderHero.GetDominantReligion() == shrine.Religion)
                {
                    party.SetMoveGoToSettlement(settlements.First(), MobileParty.NavigationType.Default, false);
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

    private void OnSessionLaunched(CampaignGameStarter starter)
    {
        _model = Campaign.Current.Models.GetFaithModel();

        _customSettlementMenus = new List<TORBaseSettlementMenuLogic>()
        {
            new ShrineMenuLogic(starter),
            new CursedSiteMenuLogic(starter),
            new RaidingSiteMenuLogic(starter),
            new OakOfAgesMenuLogic(starter)
        };


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
        if (_xmlItemIds == null)
            _xmlItemIds = MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Select(i => i.StringId)
                .ToHashSet();
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

    public override void SyncData(IDataStore dataStore)
    {
        dataStore.SyncData("_customSettlementActiveStates", ref _customSettlementActiveStates);
        dataStore.SyncData("_cursedSiteWardDurationLeft", ref _cursedSiteWardDurationLeft);
        dataStore.SyncData("_lastGhostRecruitmentTime", ref _lastGhostRecruitmentTime);
        dataStore.SyncData("_unlockedOakUpgrades", ref _unlockedOakUpgrades);
    }


}