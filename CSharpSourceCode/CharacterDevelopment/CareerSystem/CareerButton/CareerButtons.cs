using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class CareerButtons
    {
        private Dictionary<string, CareerButtonBehaviorBase> _careerButtons = new Dictionary<string, CareerButtonBehaviorBase>();
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
            _careerButtons.Add(TORCareers.Mercenary.StringId, new MercenaryCareerButtonBehavior(TORCareers.Mercenary));
            _careerButtons.Add(TORCareers.GrailKnight.StringId, new GrailKnightCareerButtonBehavior(TORCareers.GrailKnight));
            _careerButtons.Add(TORCareers.WitchHunter.StringId, new WitchHunterCareerButtonBehavior(TORCareers.WitchHunter));
        }
        
        public CareerButtonBehaviorBase GetCareerButton(CareerObject careerObject)
        {
            if (_careerButtons.ContainsKey(careerObject.StringId))
            {
                return _careerButtons[careerObject.StringId];
            }

            return null;
        }
    }
}