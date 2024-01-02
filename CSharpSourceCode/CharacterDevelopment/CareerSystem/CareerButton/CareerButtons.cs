using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class CareerButtons
    {
        private CareerObject _bloodKnight;
        private CareerObject _grailDamsel;
        private CareerObject _grailKnight;
        private CareerObject _mercenary;
        private CareerObject _minorVampire;
        private CareerObject _necromancer;
        private CareerObject _warriorPriest;
        private CareerObject _witchHunter;
        
        public CareerButtons()
        {
            _careerButtons.Add(TORCareers.Mercenary, new MercenaryButtonBehavior(TORCareers.Mercenary));
            _careerButtons.Add(TORCareers.GrailKnight, new MercenaryButtonBehavior(TORCareers.GrailKnight));
            _careerButtons.Add(TORCareers.WitchHunter, new MercenaryButtonBehavior(TORCareers.WitchHunter));
        }
        
        private static CareerButtons _instance;



        public static CareerButtons Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CareerButtons();
                    return _instance;
                }
                else
                {
                    return _instance;
                }

                return null;
            }
        }
        
        private Dictionary<CareerObject, CareerButtonBehaviorBase> _careerButtons =new Dictionary<CareerObject, CareerButtonBehaviorBase>();
       
        
        public CareerButtonBehaviorBase GetCareerButtons(CareerObject careerObject)
        {
            if (_careerButtons.ContainsKey(careerObject))
            {
                return _careerButtons[careerObject];
            }

            return null;
        }
    }
}