using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class Shrine : ISettlementType
    {
        private string _gameMenuName;
        private ReligionObject _religion;
        private Settlement _settlement;
        private TORCustomSettlementComponent _component;

        public string GameMenuName => _gameMenuName;
        public bool IsRaidingPartySpawner => false;
        public bool IsActive { get; set; } = true;
        public bool IsBattleUnderway { get; set; } = false;
        public string RewardItemId => string.Empty;
        public ReligionObject Religion => _religion;

        public void AddGameMenus(CampaignGameStarter starter)
        {
            starter.AddGameMenu(GameMenuName, "{LOCATION_DESCRIPTION}", MenuInit);
            starter.AddGameMenuOption(GameMenuName, "leave", "Leave...", delegate (MenuCallbackArgs args)
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return true;
            }, (MenuCallbackArgs args) => PlayerEncounter.Finish(true), true);
        }

        public void OnInit(Settlement settlement, ReligionObject religion)
        {
            _gameMenuName = "customsettlement_menu_" + settlement.StringId;
            _settlement = settlement;
            _component = settlement.SettlementComponent as TORCustomSettlementComponent;
            _religion = religion;
        }

        private void MenuInit(MenuCallbackArgs args)
        {
            var text = IsActive ? GameTexts.FindText("customsettlement_intro", _settlement.StringId) : GameTexts.FindText("customsettlement_disabled", _settlement.StringId);
            if(_religion != null)
            {
                MBTextManager.SetTextVariable("RELIGION_LINK", _religion.EncyclopediaLinkWithName);
            }
            MBTextManager.SetTextVariable("LOCATION_DESCRIPTION", text);
            args.MenuContext.SetBackgroundMeshName(_component.BackgroundMeshName);
        }

        public void SpawnNewParty() { }
    }
}
