using TOR_Core.CharacterDevelopment;

namespace TOR_Core.CampaignMechanics.Choices
{
    public class TORCareerChoiceBase
    {
        protected TORCareerChoiceBase()
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