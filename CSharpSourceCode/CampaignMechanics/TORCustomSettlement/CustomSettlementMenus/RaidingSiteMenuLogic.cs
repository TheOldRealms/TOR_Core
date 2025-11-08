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

public class RaidingSiteMenuLogic(CampaignGameStarter starter) : TORBaseSettlementMenuLogic(starter)
{
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
        var ownerClan = settlement.OwnerClan;
        ownerClan ??= Clan.FindFirst(x => x.StringId == "chaos_clan_1");
        var template = ownerClan.DefaultPartyTemplate;
        var partysize = component.BattlePartySize;
        var party = RaidingPartyComponent.CreateRaidingParty(settlement.StringId + "_defender_party", settlement, "Defenders", template, ownerClan, partysize);
        PlayerEncounter.RestartPlayerEncounter(party.Party, PartyBase.MainParty, false);
        if (PlayerEncounter.Battle == null)
        {
            PlayerEncounter.StartBattle();
            PlayerEncounter.Update();
        }

        component.IsBattleUnderway = true;
        CampaignMission.OpenBattleMission(component.BattleSceneName, false);
    }

   
}