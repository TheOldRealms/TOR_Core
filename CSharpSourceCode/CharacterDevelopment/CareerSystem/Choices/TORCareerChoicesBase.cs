using TOR_Core.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CampaignMechanics.Choices
{
    public abstract class TORCareerChoicesBase
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
        
        protected abstract void RegisterAll();


        protected abstract void InitializeKeyStones();

        protected abstract void InitializePassives();


        public void UnlockCareerBenefits(int tier)
        {
            switch (tier)
            {
                case 1: UnlockCareerBenefitsTier1();
                    break;
                case 2 : UnlockCareerBenefitsTier2();
                    break;
                case 3 : UnlockCareerBenefitsTier3();
                    break;
            }
        }
        
        /**
         * Allows to set special on achieving certain tiers. Will be invoked every time the career screen opens, therefore requires previous unlock checks. Will potentially only work for Attributes or similiar, that have inbuilt checks
         */
        protected virtual void UnlockCareerBenefitsTier1()
        {
            
        }

        /**
         * Allows to set special on achieving certain tiers. Will be invoked every time the career screen opens, therefore requires previous unlock checks. Will potentially only work for Attributes or similiar, that have inbuilt checks
         */
        protected virtual void UnlockCareerBenefitsTier2()
        {
            
        }

        /**
         * Allows to set special on achieving certain tiers. Will be invoked every time the career screen opens, therefore requires previous unlock checks. Will potentially only work for Attributes or similiar, that have inbuilt checks
         */
        protected virtual void UnlockCareerBenefitsTier3()
        {
            
        }

        public virtual void ClearCareerBenefits()
        {
            //this should only be meaningful in 
        }



    }
}