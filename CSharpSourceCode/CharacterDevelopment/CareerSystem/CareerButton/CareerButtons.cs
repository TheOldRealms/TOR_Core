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
            _careerButtons.Add(TORCareers.Waywatcher.StringId, new WaywatcherCareerButtonBehavior(TORCareers.Waywatcher));
            _careerButtons.Add(TORCareers.KnightOldWorld.StringId, new KnightOldWorldCareerButtonBehavior(TORCareers.KnightOldWorld));
            _careerButtons.Add(TORCareers.Ironbreaker.StringId, new IronbreakerCareerButtonBehavior(TORCareers.Ironbreaker));
            _careerButtons.Add(TORCareers.Slayer.StringId, new SlayerCareerButtonBehavior(TORCareers.Slayer));
            _careerButtons.Add(TORCareers.Runelord.StringId, new RunelordCareerButtonBehavior(TORCareers.Runelord));
            _careerButtons.Add(TORCareers.OrcBoss.StringId, new OrcBossCareerButton(TORCareers.OrcBoss));
            _careerButtons.Add(TORCareers.OrcShaman.StringId, new OrcShamanCareerButton(TORCareers.OrcShaman));
        }

        public CareerButtonBehaviorBase GetCareerButton(CareerObject careerObject)
        {
            return careerObject != null && _careerButtons.TryGetValue(careerObject.StringId, out var result) ? result : null;
        }
    }
}