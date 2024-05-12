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
        public WarriorPriestCareerChoices WarriorPriestCareerChoices { get; private set; }
        
        public VampireCountCareerChoices VampireCountCareerChoices { get; private set; }
        
        public BloodKnightCareerChoices BloodKnightCareerChoices { get; private set; }
        
        public MercenaryCareerChoices MercenaryCareerChoices { get; private set; }
        
        public GrailKnightCareerChoices GrailKnightCareerChoices { get; private set; }
        
        public GrailDamselCareerChoices GrailDamselCareerChoices { get; private set; }
        
        public WitchHunterCareerChoices WitchHunterCareerChoices { get; private set; }
        
        public NecromancerCareerChoices NecromancerCareerChoices { get; private set; }
        
        public BlackGrailKnightCareerChoices BlackGrailKnightCareerChoices { get; private set; }
        
        public NecrarchCareerChoices NecrarchCareerChoices { get; private set; }
        
        public WarriorPriestUlricCareerChoices WarriorPriestUlricCareerChoices { get; private set; }
        
        public ImperialMagisterCareerChoices ImperialMagisterCareerChoices { get; private set; }

        private List<TORCareerChoicesBase> _allCareers =new List<TORCareerChoicesBase>();

        public TORCareerChoices()
        {
            Instance = this;
            SetBasicTextVariables();
            WarriorPriestCareerChoices = new WarriorPriestCareerChoices(TORCareers.WarriorPriest);
            VampireCountCareerChoices = new VampireCountCareerChoices(TORCareers.MinorVampire);
            BloodKnightCareerChoices = new BloodKnightCareerChoices(TORCareers.BloodKnight);
            MercenaryCareerChoices = new MercenaryCareerChoices(TORCareers.Mercenary);
            GrailDamselCareerChoices = new GrailDamselCareerChoices(TORCareers.GrailDamsel);
            GrailKnightCareerChoices = new GrailKnightCareerChoices(TORCareers.GrailKnight);
            WitchHunterCareerChoices = new WitchHunterCareerChoices(TORCareers.WitchHunter);
            NecromancerCareerChoices = new NecromancerCareerChoices(TORCareers.Necromancer);
            BlackGrailKnightCareerChoices = new BlackGrailKnightCareerChoices(TORCareers.BlackGrailKnight);
            NecrarchCareerChoices = new NecrarchCareerChoices(TORCareers.Necrarch);
            WarriorPriestUlricCareerChoices = new WarriorPriestUlricCareerChoices(TORCareers.WarriorPriestUlric);
            ImperialMagisterCareerChoices = new ImperialMagisterCareerChoices(TORCareers.ImperialMagister);
            
            _allCareers.Add(WarriorPriestCareerChoices);
            _allCareers.Add(WitchHunterCareerChoices);
            _allCareers.Add(VampireCountCareerChoices);
            _allCareers.Add(BloodKnightCareerChoices);
            _allCareers.Add(MercenaryCareerChoices);
            _allCareers.Add(GrailKnightCareerChoices);
            _allCareers.Add(GrailDamselCareerChoices);
            _allCareers.Add(NecromancerCareerChoices);
            _allCareers.Add(NecrarchCareerChoices);
            _allCareers.Add(BlackGrailKnightCareerChoices);
            _allCareers.Add(WarriorPriestUlricCareerChoices);
            _allCareers.Add(ImperialMagisterCareerChoices);
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
