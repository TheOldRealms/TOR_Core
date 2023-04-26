using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Overlay;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.SettlementTypes
{
    public class Shrine : ISettlementType
    {
        private ReligionObject _religion;
        private Settlement _settlement;
        private TORCustomSettlementComponent _component;
        public const int DEFAULT_BLESSING_DURATION = 6;

        public bool IsRaidingPartySpawner => false;
        public bool IsActive { get; set; } = true;
        public ReligionObject Religion => _religion;

        public void OnInit(Settlement settlement, ReligionObject religion)
        {
            _settlement = settlement;
            _component = settlement.SettlementComponent as TORCustomSettlementComponent;
            _religion = religion;
        }

        private bool HireCondition(MenuCallbackArgs args)
        {
            args.optionLeaveType = GameMenuOption.LeaveType.Recruit;
            var godName = GameTexts.FindText("tor_religion_name_of_god", _religion.StringId);
            var baseTroop = _religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);
            MBTextManager.SetTextVariable("HIRE_TEXT", "Hire a " + baseTroop.Name + " to fight for you.");
            if ((int)Hero.MainHero.GetDevotionLevelForReligion(Hero.MainHero.GetDominantReligion()) < (int)DevotionLevel.Devoted)
            {
                args.Tooltip = new TextObject("{=!}You need to be at least Devoted to " + godName + " in order to hire religious troops", null);
                args.IsEnabled = false;
            }
            return IsActive;
        }

        private void HireConsequence(MenuCallbackArgs args)
        {
            var baseTroop = _religion.ReligiousTroops.FirstOrDefault(x => x.IsBasicTroop && x.Occupation == Occupation.Soldier);

        }

        public void SpawnNewParty() { }
    }
}
