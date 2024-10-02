using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;
using TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;
using TOR_Core.Extensions;
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

    public static MBReadOnlyList<Settlement> AllCustomSettlements { get; private set; } = [];

    public int LastDefileTime(Hero hero)
    {
        if (_lastDefileTime.TryGetValue(hero.StringId, out int value))
        {
            return value;
        }

        _lastDefileTime.Add(hero.StringId, (int)CampaignTime.Now.ToDays);
        return _lastDefileTime[hero.StringId];
    }

    public int LastGhostRecruitmentTime(Hero hero)
    {
        if (_lastGhostRecruitmentTime.TryGetValue(hero.StringId, out int value))
        {
            return value;
        }
        else
        {
            _lastGhostRecruitmentTime.Add(hero.StringId, (int)CampaignTime.Now.ToDays);
            return _lastGhostRecruitmentTime[hero.StringId];
        }
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
        return _unlockedOakUpgrades.Where(x=> x.StartsWith(unlockedUpgradeCategory)).ToList();
    }
    
    
    private void OnMissionEnded(IMission obj)
    {
        var battleSettlement = Settlement.FindFirst(delegate(Settlement settlement)
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
            if (mission.MissionResult != null && mission.MissionResult.BattleResolved && mission.MissionResult.PlayerVictory)
            {
                comp.IsActive = false;
                var list = new List<InquiryElement>();
                var item = MBObjectManager.Instance.GetObject<ItemObject>(comp.RewardItemId);
                list.Add(new InquiryElement(item, item.Name.ToString(), new ImageIdentifier(item)));
                var inq = new MultiSelectionInquiryData("Victory!", new TextObject("{=tor_custom_settlement_chaos_portal_victory_str}You are Victorious! Claim your reward!").ToString(), list, false, 1, 1, "OK", null, OnRewardClaimed, null);
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
        Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, 1);
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

    private void OnSettlementEntered(MobileParty party, Settlement settlement, Hero hero)
    {
        if (party == MobileParty.MainParty) return;
        if (settlement.SettlementComponent is ShrineComponent)
        {
            if (settlement.SettlementComponent is not ShrineComponent shrine) return;

            party.AddBlessingToParty(shrine.Religion.StringId);
            LeaveSettlementAction.ApplyForParty(party);
            party.Ai.SetMoveModeHold();
            party.Ai.SetDoNotMakeNewDecisions(false);
            party.Ai.RethinkAtNextHourlyTick = true;
        }
        else if (settlement.SettlementComponent is CursedSiteComponent)
        {
            var troop = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_spirit_host");
            var freeSlots = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
            int raisePower = Math.Max(1, (int)party.LeaderHero.GetExtendedInfo().SpellCastingLevel);
            var count = MBRandom.RandomInt(0, 4);
            count *= raisePower;
            if (freeSlots > 0)
            {
                if (freeSlots < count) count = freeSlots;
                party.MemberRoster.AddToCounts(troop, count);
                CampaignEventDispatcher.Instance.OnTroopRecruited(party.LeaderHero, settlement, null, troop, count);
            }

            LeaveSettlementAction.ApplyForParty(party);
            if (_lastGhostRecruitmentTime.ContainsKey(party.LeaderHero.StringId))
            {
                _lastGhostRecruitmentTime[party.LeaderHero.StringId] = (int)CampaignTime.Now.ToDays;
            }
            else
            {
                _lastGhostRecruitmentTime.Add(party.LeaderHero.StringId, (int)CampaignTime.Now.ToDays);
            }

            party.Ai.SetDoNotMakeNewDecisions(false);
            party.Ai.RethinkAtNextHourlyTick = true;
        }
    }

    private void OnAiTick(MobileParty party)
    {
        if (party == MobileParty.MainParty) return;
        if (ShrineMenuLogic.CanPartyGoToShrine(party))
        {
            var settlements = TORCommon.FindSettlementsAroundPosition(party.Position2D, 20, x => x.SettlementComponent is ShrineComponent);
            if (settlements.Count > 0)
            {
                var shrine = settlements.First().SettlementComponent as ShrineComponent;
                if (party.LeaderHero.GetDominantReligion() == shrine.Religion)
                {
                    party.Ai.SetMoveGoToSettlement(settlements.First());
                    party.Ai.SetDoNotMakeNewDecisions(true);
                }
            }
        }

        if (CursedSiteMenuLogic.CanPartyRecruitGhosts(party))
        {
            if (!_lastGhostRecruitmentTime.ContainsKey(party.LeaderHero.StringId) || _lastGhostRecruitmentTime[party.LeaderHero.StringId] + CursedSiteMenuLogic.MinimumDaysBetweenRaisingGhosts < (int)CampaignTime.Now.ToDays)
            {
                var settlements = TORCommon.FindSettlementsAroundPosition(party.Position2D, 20, x => x.SettlementComponent is CursedSiteComponent);
                if (settlements.Count > 0)
                {
                    party.Ai.SetMoveGoToSettlement(settlements.First());
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
    }

    private void OnSettlementHourlyTick(Settlement settlement)
    {
        if (settlement.SettlementComponent is CursedSiteComponent)
        {
            var site = settlement.SettlementComponent as CursedSiteComponent;
            if (site.IsActive)
            {

                var affectedParties = TORCommon.FindPartiesAroundPosition(settlement.Position2D, TORConstants.DEFAULT_CURSE_RADIUS, x => (x.IsLordParty && x.LeaderHero != null && x.LeaderHero.GetDominantReligion() != site.Religion) && (x.IsLordParty && x.LeaderHero != null && x.LeaderHero.Culture.StringId != "mousillon"));

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
                            party.MemberRoster.WoundNumberOfTroopsRandomly((int)Math.Ceiling(party.MemberRoster.TotalHealthyCount * (_model.CalculateCursedRegionDamagePerHour(party) / 100f)));
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