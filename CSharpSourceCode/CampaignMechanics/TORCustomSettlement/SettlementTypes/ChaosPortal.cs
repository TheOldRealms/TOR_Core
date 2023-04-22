using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.ChaosRaiding;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class ChaosPortal : ISettlementType
    {
        private string _gameMenuName;
        private Settlement _settlement;
        private TORCustomSettlementComponent _component;
        private readonly string _battleSceneName = "TOR_chaos_portal_001_forceatmo";
        public bool IsActive { get; set; } = true;
        public string GameMenuName => _gameMenuName;
        public bool IsRaidingPartySpawner => true;
        private bool _isBattleUnderway;
        public bool IsBattleUnderway
        {
            get => _isBattleUnderway; set => _isBattleUnderway = value; 
        }
        public string RewardItemId => "tor_empire_weapon_sword_runefang_001";

        public void OnInit(Settlement settlement, ReligionObject religion)
        {
            _gameMenuName = "customsettlement_menu_" + settlement.StringId;
            _settlement = settlement;
            _component = settlement.SettlementComponent as TORCustomSettlementComponent;
        }

        public void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu(GameMenuName, "{LOCATION_DESCRIPTION}", MenuInit);
            starter.AddGameMenuOption(GameMenuName, "dobattle", "{BATTLE_OPTION_TEXT", BattleCondition, BattleConsequence);
            starter.AddGameMenuOption(GameMenuName, "leave", "Leave...", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        }

        private void MenuInit(MenuCallbackArgs args)
        {
            var text = IsActive ? GameTexts.FindText("customsettlement_intro", _settlement.StringId) : GameTexts.FindText("customsettlement_disabled", _settlement.StringId);
            MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            args.MenuContext.SetBackgroundMeshName(_component.BackgroundMeshName);
        }

        private bool BattleCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.HostileAction;
            var text = GameTexts.FindText("customsettlement_battle", _settlement.StringId);
            MBTextManager.SetTextVariable("BATTLE_OPTION_TEXT", text);
            if (Hero.MainHero.IsWounded)
            {
                args.Tooltip = new TextObject("{=UL8za0AO}You are wounded.", null);
                args.IsEnabled = false;
            }
            return IsActive;
        }

        private void BattleConsequence(MenuCallbackArgs args)
        {
            var party = ChaosRaidingPartyComponent.CreateChaosRaidingParty(_settlement.StringId + "_defender_party", _settlement, _component, 250);
            PlayerEncounter.RestartPlayerEncounter(party.Party, PartyBase.MainParty, true);
            if (PlayerEncounter.Battle == null)
            {
                PlayerEncounter.StartBattle();
                PlayerEncounter.Update();
            }
            _isBattleUnderway = true;
            CampaignMission.OpenBattleMission(_battleSceneName);
        }

        public void SpawnNewParty()
        {
            var find = TORCommon.FindSettlementsAroundPosition(_settlement.Position2D, 60, x => !x.IsRaided && !x.IsUnderRaid && x.IsVillage).GetRandomElementInefficiently();
            var chaosRaidingParty = ChaosRaidingPartyComponent.CreateChaosRaidingParty("chaos_clan_1_party_" + _component.RaidingPartyCount + 1, _settlement, _component, MBRandom.RandomInt(75, 99));
            SetPartyAiAction.GetActionForRaidingSettlement(chaosRaidingParty, find);
            ((ChaosRaidingPartyComponent)chaosRaidingParty.PartyComponent).Target = find;
        }
    }
}
