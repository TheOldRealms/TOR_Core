using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement
{
    public class TORCustomSettlementCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            var list = Settlement.FindAll(x => x.SettlementComponent is TORCustomSettlementComponent);
            foreach(var item in list)
            {
                var comp = item.SettlementComponent as TORCustomSettlementComponent;
                comp.SettlementType.AddGameMenus(starter);
            }
        }

        private void OnMissionEnded(IMission obj)
        {
            var battleSettlement = Settlement.FindFirst(delegate (Settlement settlement)
            {
                if(settlement.SettlementComponent is TORCustomSettlementComponent)
                {
                    var comp = settlement.SettlementComponent as TORCustomSettlementComponent;
                    return comp.SettlementType.IsBattleUnderway;
                }
                return false;
            });
            if(battleSettlement != null)
            {
                var mission = obj as Mission;
                if (mission.MissionResult != null && mission.MissionResult.BattleResolved && mission.MissionResult.PlayerVictory)
                {
                    var settlementType = ((TORCustomSettlementComponent)battleSettlement.SettlementComponent).SettlementType;
                    settlementType.IsActive = false;
                    var list = new List<InquiryElement>();
                    var item = MBObjectManager.Instance.GetObject<ItemObject>(settlementType.RewardItemId);
                    list.Add(new InquiryElement(item, item.Name.ToString(), new ImageIdentifier(item)));
                    var inq = new MultiSelectionInquiryData("Victory!", "You are Victorious! Claim your reward!", list, false, 1, "OK", null, onRewardClaimed, null);
                    MBInformationManager.ShowMultiSelectionInquiry(inq);
                }
                else
                {
                    var inq = new InquiryData("Defeated!", "The enemy proved more than a match for you. Better luck next time!", true, false, "OK", null, null, null);
                    InformationManager.ShowInquiry(inq);
                }
            }
        }

        private void onRewardClaimed(List<InquiryElement> obj)
        {
            var item = obj[0].Identifier as ItemObject;
            Hero.MainHero.PartyBelongedTo.Party.ItemRoster.AddToCounts(item, 1);
        }

        public override void SyncData(IDataStore dataStore)
        {
            //throw new NotImplementedException();
        }
    }
}
