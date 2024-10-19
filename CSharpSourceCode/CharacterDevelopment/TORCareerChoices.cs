using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoices
    {
        public static TORCareerChoices Instance { get; private set; }

        private readonly List<TORCareerChoicesBase> _allCareers;

        public TORCareerChoices()
        {
            Instance = this;
            SetBasicTextVariables();
            
            _allCareers =
            [
                new WarriorPriestCareerChoices(TORCareers.WarriorPriest),
                new VampireCountCareerChoices(TORCareers.MinorVampire),
                new BloodKnightCareerChoices(TORCareers.BloodKnight),
                new MercenaryCareerChoices(TORCareers.Mercenary),
                new GrailDamselCareerChoices(TORCareers.GrailDamsel),
                new GrailKnightCareerChoices(TORCareers.GrailKnight),
                new WitchHunterCareerChoices(TORCareers.WitchHunter),
                new NecromancerCareerChoices(TORCareers.Necromancer),
                new BlackGrailKnightCareerChoices(TORCareers.BlackGrailKnight),
                new NecrarchCareerChoices(TORCareers.Necrarch),
                new WarriorPriestUlricCareerChoices(TORCareers.WarriorPriestUlric),
                new ImperialMagisterCareerChoices(TORCareers.ImperialMagister),
                new WaywatcherCareerChoices(TORCareers.Waywatcher),
                new SpellsingerCareerChoices(TORCareers.Spellsinger),
                new GreyLordCareerChoices(TORCareers.GreyLord)
            ];
        }



        public static CareerChoiceObject GetChoice(string id) => Game.Current.ObjectManager.GetObject<CareerChoiceObject>(x => x.StringId == id);


        public TORCareerChoicesBase  GetCareerChoices(CareerObject id)
        {
            return _allCareers.FirstOrDefault(x => x.GetID() ==id);
        }

        private void SetBasicTextVariables()
        {
            foreach (var type in Enum.GetValues(typeof(PassiveEffectType)).Cast<PassiveEffectType>())
            {
                GameTexts.SetVariable("TOR_CHOICE_"+type.ToString().ToUpper(),GameTexts.FindText("tor_careerchoice_basic", type.ToString()));
            }
            
        }
    }
}
