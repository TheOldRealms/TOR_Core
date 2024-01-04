using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class CareerButtons
    {
        private Dictionary<CareerObject, CareerButtonBehaviorBase> _careerButtons = new Dictionary<CareerObject, CareerButtonBehaviorBase>();
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
                return _instance;
            }
        }
        public CareerButtons()
        {
            _careerButtons.Add(TORCareers.Mercenary, new MercenaryCareerButtonBehavior(TORCareers.Mercenary));
            _careerButtons.Add(TORCareers.GrailKnight, new GrailKnightCareerButtonBehavior(TORCareers.GrailKnight));
            _careerButtons.Add(TORCareers.WitchHunter, new WitchHunterCareerButtonBehavior(TORCareers.WitchHunter));
        }
        
        public CareerButtonBehaviorBase GetCareerButton(CareerObject careerObject)
        {
            if (_careerButtons.ContainsKey(careerObject))
            {
                return _careerButtons[careerObject];
            }

            return null;
        }
    }
}