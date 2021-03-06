using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.AbilitySystem.Spells;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class HeroExtendedInfo
    {
        [SaveableField(0)] public List<string> AcquiredAbilitySystem = new List<string>();
        [SaveableField(1)] public List<string> AcquiredAttributes = new List<string>();
        [SaveableField(2)] public float CurrentWindsOfMagic = 0;
        [SaveableField(3)] public int Corruption = 0; //between 0 and 100, 0 = pure af, 100 = fallen to chaos
        [SaveableField(4)] public SpellCastingLevel SpellCastingLevel = SpellCastingLevel.None;
        [SaveableField(5)] private CharacterObject _baseCharacter;
        [SaveableField(6)] private List<string> _knownLores = new List<string>();

        public CharacterObject BaseCharacter => _baseCharacter;

        public float MaxWindsOfMagic
        {
            get
            {
                if (!(Game.Current.GameType is Campaign)) return 50;
                else
                {
                    var hero = _baseCharacter.HeroObject;
                    var intelligence = hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
                    var retval = Math.Min(intelligence * 10, 99);
                    if (hero.Occupation == Occupation.Lord && hero != Hero.MainHero) retval += 20;
                    return retval;
                }
            }
        }
        public float WindsOfMagicRechargeRate
        {
            get
            {
                if (!(Game.Current.GameType is Campaign)) return 0.2f;
                else
                {
                    var hero = _baseCharacter.HeroObject;
                    var intelligence = hero.GetAttributeValue(DefaultCharacterAttributes.Intelligence);
                    return intelligence * 0.5f;
                }
            }
        }

        public List<LoreObject> KnownLores
        {
            get
            {
                List<LoreObject> list = new List<LoreObject>();
                foreach (var item in _knownLores)
                {
                    list.Add(LoreObject.GetLore(item));
                }
                return list;
            }
        }

        public List<string> AllAbilites
        {
            get
            {
                var list = new List<string>();
                if (_baseCharacter != null) list.AddRange(_baseCharacter.GetAbilities());
                list.AddRange(AcquiredAbilitySystem);
                return list;
            }
        }

        public List<string> AllAttributes
        {
            get
            {
                var list = new List<string>();
                if (_baseCharacter != null) list.AddRange(_baseCharacter.GetAttributes());
                list.AddRange(AcquiredAttributes);
                return list;
            }
        }

        public HeroExtendedInfo(CharacterObject character)
        {
            _baseCharacter = character;
        }

        public void AddKnownLore(string loreId)
        {
            if (LoreObject.GetLore(loreId) != null && !_knownLores.Contains(loreId)) _knownLores.Add(loreId);
        }

        public bool HasKnownLore(string loreId)
        {
            return _knownLores.Contains(loreId);
        }
    }
    public class HeroExtendedInfoInfoDefiner : SaveableTypeDefiner
    {
        public HeroExtendedInfoInfoDefiner() : base(1_543_132) { }
        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
            AddClassDefinition(typeof(HeroExtendedInfo), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
            ConstructContainerDefinition(typeof(Dictionary<string, HeroExtendedInfo>));
        }
    }
}
