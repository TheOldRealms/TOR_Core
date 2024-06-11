using System.Collections.Generic;
using TOR_Core.CharacterDevelopment.CareerSystem.Button;

namespace TOR_Core.CharacterDevelopment.CareerSystem.CareerButton
{
    public class CareerButtons
    {
        private readonly Dictionary<string, CareerButtonBehaviorBase> _careerButtons = new();
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
            _careerButtons.Add(TORCareers.BlackGrailKnight.StringId, new BlackGrailKnightCareerButtonBehavior(TORCareers.BlackGrailKnight));
            _careerButtons.Add(TORCareers.Necrarch.StringId, new NecrarchCareerButtonBehavior(TORCareers.Necrarch));
            _careerButtons.Add(TORCareers.ImperialMagister.StringId, new ImperialMagisterCareerButtonBehavior(TORCareers.ImperialMagister));
            _careerButtons.Add(TORCareers.Waywatcher.StringId, new WaywatcherCareerButtonBehavior(TORCareers.Waywatcher) );
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