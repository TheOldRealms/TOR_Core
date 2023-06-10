using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCareerChoices
    {
        public static TORCareerChoices Instance { get; private set; }
        public WarriorPriestCareerChoices WarriorPriestCareerChoices { get; private set; }
        
        public VampireCountCareerChoices VampireCountCareerChoices { get; private set; }

        public TORCareerChoices()
        {
            Instance = this;
            WarriorPriestCareerChoices = new WarriorPriestCareerChoices();
            VampireCountCareerChoices = new VampireCountCareerChoices();
        }

        public static CareerChoiceObject GetChoice(string id) => Game.Current.ObjectManager.GetObject<CareerChoiceObject>(x => x.StringId == id);

    }
}
