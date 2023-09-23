using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
    public class TORSpecialSettlementBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickSettlementEvent.AddNonSerializedListener(this,ExtraFood);    
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this,AfterNewGameStart);
        }

        public void ExtraFood(Settlement settlement)
        {
            if (settlement.IsBloodKeep())
            {
                if (settlement.ItemRoster.TotalFood < 500)
                {
                    settlement.ItemRoster.AddToCounts(DefaultItems.Meat, 100);
                    settlement.ItemRoster.AddToCounts(DefaultItems.Grain, 100);
                }
            }
        }

        private void AfterNewGameStart(CampaignGameStarter starter, int index)
        {
            if(index != CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 1) return;
            BonusGarrision();
        }

        private void BonusGarrision()
        {
            var bloodKeep = Campaign.Current.Settlements.FirstOrDefault(x => x.IsBloodKeep());
            if(bloodKeep==null) return;
            if (bloodKeep.Owner.IsVampire())
            {
                var bloodDragon = MBObjectManager.Instance.GetObject<CharacterObject>("tor_bd_blooddragon_templar");
                bloodKeep.MilitiaPartyComponent.Party.AddMember(bloodDragon, 500);
                bloodKeep.SetGarrisonWagePaymentLimit(800000);
            }
        }

   


        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}