using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerCampaignBase:CampaignBehaviorBase
    {
        private int ExtraHealthPoints;
        private int ExtraAmmo;
        private int ExtraWind;


        public int GetMaximumHealthPoints()
        {
            return 10;
        }
        public int GetExtraAmmoPoints()
        {
            return 2;
        }

        
        
        public override void RegisterEvents()
        {
            
        }
        
        

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}