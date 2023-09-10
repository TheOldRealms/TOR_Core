using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.Library;
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

        private List<TORCareerChoicesBase> _allCareers =new List<TORCareerChoicesBase>();

        public TORCareerChoices()
        {
            Instance = this;
            WarriorPriestCareerChoices = new WarriorPriestCareerChoices(TORCareers.WarriorPriest);
            VampireCountCareerChoices = new VampireCountCareerChoices(TORCareers.MinorVampire);
            BloodKnightCareerChoices = new BloodKnightCareerChoices(TORCareers.BloodKnight);
            MercenaryCareerChoices = new MercenaryCareerChoices(TORCareers.Mercenary);
            GrailDamselCareerChoices = new GrailDamselCareerChoices(TORCareers.GrailDamsel);
            GrailKnightCareerChoices = new GrailKnightCareerChoices(TORCareers.GrailKnight);
            _allCareers.Add(WarriorPriestCareerChoices);
            _allCareers.Add(VampireCountCareerChoices);
            _allCareers.Add(BloodKnightCareerChoices);
            _allCareers.Add(MercenaryCareerChoices);
            _allCareers.Add(GrailKnightCareerChoices);
            _allCareers.Add(GrailDamselCareerChoices);
        }

        public static CareerChoiceObject GetChoice(string id) => Game.Current.ObjectManager.GetObject<CareerChoiceObject>(x => x.StringId == id);


        public TORCareerChoicesBase  GetCareerChoices(CareerObject id)
        {
            return _allCareers.FirstOrDefault(x => x.GetID() ==id);
        }
        
        

    }
}
