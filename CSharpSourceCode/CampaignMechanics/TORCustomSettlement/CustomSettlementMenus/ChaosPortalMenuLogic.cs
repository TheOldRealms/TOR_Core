using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RaidingParties;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public class ChaosPortalMenuLogic : TORBaseSettlementMenuLogic
{
    public ChaosPortalMenuLogic(CampaignGameStarter campaignGameStarter) : base(campaignGameStarter)
    {
    }

    protected override void AddSettlementMenu(CampaignGameStarter campaignGameStarter)
    {
        AddRaidingSiteMenus(campaignGameStarter);
    }

    public void AddRaidingSiteMenus(CampaignGameStarter starter)
    {
        starter.AddGameMenu("raidingsite_menu", "{LOCATION_DESCRIPTION}", RaidingSiteMenuInit);
        starter.AddGameMenuOption("raidingsite_menu", "dobattle", "{BATTLE_OPTION_TEXT}", RaidingSiteBattleCondition, RaidingSiteBattleConsequence);
        starter.AddGameMenuOption("raidingsite_menu", "leave", "{tor_custom_settlement_menu_leave_str}Leave...", delegate(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Leave;
            return true;
        }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
    }

    private void RaidingSiteMenuInit(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TORBaseSettlementComponent;
        var text = component.IsActive ? GameTexts.FindText("customsettlement_intro", settlement.StringId) : GameTexts.FindText("customsettlement_disabled", settlement.StringId);
        MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
        args.MenuContext.SetBackgroundMeshName(component.BackgroundMeshName);
    }

    private bool RaidingSiteBattleCondition(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as TORBaseSettlementComponent;
        args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
        var text = GameTexts.FindText("customsettlement_battle", settlement.StringId);
        MBTextManager.SetTextVariable("BATTLE_OPTION_TEXT", text);
        if (Hero.MainHero.IsWounded)
        {
            args.Tooltip = new TextObject("{=UL8za0AO}You are wounded.", null);
            args.IsEnabled = false;
        }

        return component.IsActive;
    }

    private void RaidingSiteBattleConsequence(MenuCallbackArgs args)
    {
        var settlement = Settlement.CurrentSettlement;
        var component = settlement.SettlementComponent as BaseRaiderSpawnerComponent;
        PartyTemplateObject template = settlement.Culture?.DefaultPartyTemplate;
        template ??= MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_lordparty_template");
        Clan ownerClan = settlement.OwnerClan;
        ownerClan ??= Clan.FindFirst(x => x.StringId == "chaos_clan_1");
        template = ownerClan.DefaultPartyTemplate;
        var party = RaidingPartyComponent.CreateRaidingParty(settlement.StringId + "_defender_party", settlement, "Defenders", template, ownerClan, 250);
        PlayerEncounter.RestartPlayerEncounter(party.Party, PartyBase.MainParty, false);
        if (PlayerEncounter.Battle == null)
        {
            PlayerEncounter.StartBattle();
            PlayerEncounter.Update();
        }

        component.IsBattleUnderway = true;
        CampaignMission.OpenBattleMission(component.BattleSceneName, false);
    }

    private void OnMissionEnded(IMission obj)
    {
        var battleSettlement = Settlement.FindFirst(delegate(Settlement settlement)
        {
            if (settlement.SettlementComponent is BaseRaiderSpawnerComponent)
            {
                var comp = settlement.SettlementComponent as BaseRaiderSpawnerComponent;
                return comp.IsBattleUnderway;
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
                var inq = new MultiSelectionInquiryData("Victory!", "{=tor_custom_settlement_chaos_portal_victory_str}You are Victorious! Claim your reward!", list, false, 1, 1, "OK", null, OnRewardClaimed, null);
                MBInformationManager.ShowMultiSelectionInquiry(inq);
            }
            else
            {
                var inq = new InquiryData("Defeated!", "{=tor_custom_settlement_chaos_portal_lose_str}The enemy proved more than a match for you. Better luck next time!", true, false, "OK", null, null, null);
                InformationManager.ShowInquiry(inq);
            }
        }
    }

 

    private void OnRewardClaimed(List<InquiryElement> obj)
    {
        var item = obj[0].Identifier as ItemObject;
        Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, 1);
    }
    
    
}