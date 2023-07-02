using TOR_Core.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment.CareerSystem;

namespace TOR_Core.CampaignMechanics.Choices
{
    public class TORCareerChoicesBase
    {
        protected CareerObject CareerID=null;
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
        
       
       
    }
}