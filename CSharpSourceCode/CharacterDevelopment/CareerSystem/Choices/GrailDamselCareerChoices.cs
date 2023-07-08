using TaleWorlds.Core;
using TOR_Core.CampaignMechanics.Choices;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class GrailDamselCareerChoices : TORCareerChoicesBase
    {
        public GrailDamselCareerChoices(CareerObject id) : base(id) {}
        
        
        private CareerChoiceObject _grailDamselRootNode;
        
        
        protected override void RegisterAll()
        {
            _grailDamselRootNode = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrailDamselRoot"));
        }

        protected override void InitializeKeyStones()
        {
            _grailDamselRootNode.Initialize(CareerID, "No Career Ability", null, true, ChoiceType.Keystone);
        }

        protected override void InitializePassives()
        {
            
        }
    }
}