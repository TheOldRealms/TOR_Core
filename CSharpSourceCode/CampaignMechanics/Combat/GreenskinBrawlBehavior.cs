using Helpers;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.Combat
{
    public class GreenskinBrawlBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
        }

        private void OnSessionStart(CampaignGameStarter starter)
        {
            AddTownMenuButton(starter);
        }

        private void AddTownMenuButton(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption("town", "town_greenskin_brawl", "Start a brawl",
                args =>
                {
                    args.optionLeaveType = GameMenuOption.LeaveType.Mission;
                    bool canBrawl = CanStartBrawl();
                    return MenuHelper.SetOptionProperties(args, canBrawl, false, TextObject.GetEmpty());
                },
                args =>
                {
                    StartBrawl();
                },
                false, 5, false, null);
        }

        private bool CanStartBrawl()
        {
            if (Settlement.CurrentSettlement?.Culture?.StringId != TORConstants.Cultures.GREENSKIN)
                return false;

            return Settlement.CurrentSettlement.IsTown && Settlement.CurrentSettlement.Town?.GarrisonParty?.MemberRoster?.TotalManCount > 0;
        }

        private void StartBrawl()
        {
            // TODO: Implement brawl logic here
            // For now, just show a placeholder message
            var message = new TextObject("The brawl begins! (Placeholder - implementation coming soon)");
            InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}