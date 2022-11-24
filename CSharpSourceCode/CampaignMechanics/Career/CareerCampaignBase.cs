using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.Career
{
    public class CareerCampaignBase:CampaignBehaviorBase
    {
        private int _extraHealthPoints;
        private int _extraAmmo;
        private int _extraWind;


        public int GetMaximumHealthPoints()
        {
            return 10;
        }
        public int GetExtraAmmoPoints()
        {
            return 5;
        }

        
        
        public override void RegisterEvents()
        {
            
        }
        
        

        public override void SyncData(IDataStore dataStore)
        {
            
        }
    }
}