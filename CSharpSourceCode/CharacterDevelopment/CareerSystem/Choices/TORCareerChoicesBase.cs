using TOR_Core.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CampaignMechanics.Choices
{
    public class TORCareerChoicesBase
    {
        protected CareerObject CareerID=null;


        public CareerObject GetID()
        {
            return CareerID;
        }
        
        public TORCareerChoicesBase(CareerObject careerId)
        {
            CareerID = careerId;
            RegisterAll();
            InitializePassives();
            InitializeKeyStones();
        }
        
        protected TORCareerChoicesBase()
        {
            RegisterAll();
            InitializePassives();
            InitializeKeyStones();
        }

        


        protected virtual void RegisterAll()
        {
            
        }

        protected virtual void InitializeKeyStones()
        {
            
        }

        protected virtual void InitializePassives()
        {
            
        }

        public void UnlockRewards(int tier)
        {
            switch (tier)
            {
                case 1: UnlockRewardTier1();
                    break;
                case 2 : UnlockRewardTier2();
                    break;
                case 3 : UnlockRewardTier3();
                    break;
            }
        }
        
        /**
         * Allows to set special on achieving certain tiers. Will be invoked every time the career screen opens, therefore requires previous unlock checks. Will potentially only work for Attributes or similiar, that have inbuilt checks
         */
        protected virtual void UnlockRewardTier1()
        {
            
        }

        /**
         * Allows to set special on achieving certain tiers. Will be invoked every time the career screen opens, therefore requires previous unlock checks. Will potentially only work for Attributes or similiar, that have inbuilt checks
         */
        protected virtual void UnlockRewardTier2()
        {
            
        }

        /**
         * Allows to set special on achieving certain tiers. Will be invoked every time the career screen opens, therefore requires previous unlock checks. Will potentially only work for Attributes or similiar, that have inbuilt checks
         */
        protected virtual void UnlockRewardTier3()
        {
            
        }

        public virtual void ClearCareerRewards()
        {
            
        }



    }
}