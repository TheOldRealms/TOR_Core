using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TOR_Core.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;

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
            var mainhero = Hero.MainHero;
            var tierText = "CareerTier";
            if(mainhero.HasUnlockedCareerChoiceTier(tier))return;
            
            switch (tier)
            {
                case 1: UnlockCareerBenefitsTier1();
                    break;
                case 2 : UnlockCareerBenefitsTier2();
                    break;
                case 3 : UnlockCareerBenefitsTier3();
                    break;
            }
            mainhero.AddAttribute(tierText + tier);
  
        }

        protected virtual void UnlockCareerBenefitsTier1()
        {
          
        }

        protected virtual void UnlockCareerBenefitsTier2()
        {
            
        }

        protected virtual void UnlockCareerBenefitsTier3()
        {
            
        }

        public virtual void InitialCareerSetup()
        {
            
        }
    }
}