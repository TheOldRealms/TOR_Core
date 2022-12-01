using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
namespace TOR_Core.CampaignMechanics.Career
{
    
    /// <summary>
    /// The main communicator, between the World and the serialization process.
    /// 1.Loads on startup the model of the Career,
    /// 2. checks all unlocked nodes with the model, removes all that's id is not existing in the model, removes it's properties,  and provides 
    /// </summary>
    public class CareerCampaignBase:CampaignBehaviorBase
    {
        private CareerId _careerId;
        private List<string> _torCareerSkillPoints;

        private int _extraHealthPoints;
        private int _extraAmmo;
        private int _extraWind;
        // extra faith
        private float[] _bonusMeleeDamage;
        private float[] _bonusRangeDamage;
        private float[] _bonusSpellDamge;
        
        //Career ability perks
        
        
        
        private List<WeaponClass> _requiredWeaponTypes=new List<WeaponClass>();
        private string statusEffectOverride;
        private bool _canBeUsedOnHorse = false;

        private CareerTemplate _currentSelectedCareerTemplate;
        
        //post attack behavior? Script name
        
        //modifiers
        private int _chargeModifier;
        private int _damageModifer;
        private int _usagesModifer;
        private float _durationModifier;
        private float _offsetModifier;
        private float _radiusModifier;
        private float _castTimeModifier;
        private float _impactRadius;
        private float _maxDistance;
        private float _minDistance;
        private float _baseMovementSpeed;
        private float _imbuedStatusEffectDuration;
        private float _windsOfMagicCost;
        
        //overrides
        private DamageType _damageTypeOverride;

        public DamageType DamageTypeOverride => _damageTypeOverride;


        public string GetCareerAbilityID()
        {
            if (_currentSelectedCareerTemplate == null) return "";
            return _currentSelectedCareerTemplate.AbilityTemplateId;
        }

        public bool HasRequiredWeaponFlags(WeaponClass weaponClass)
        {
            return _requiredWeaponTypes.Any(item => weaponClass == item);
        }
        public bool CanBeUsedWhileMounted()
        {
            return _canBeUsedOnHorse;
        }
        public int GetExtraHealthPoints()
        {
            return _extraHealthPoints;
        }
        public int GetExtraAmmoPoints()
        {
            return _extraAmmo;
        }
        
        public int GetExtraWindPoints()
        {
            return _extraWind;
        }
        
        public float[] GetCareerBonusSpellDamage()
        {
            return _bonusSpellDamge;
        }

        public float[] GetCareerBonusMeleeDamage()
        {
            return _bonusMeleeDamage;
        }
        
        public float[] GetCareerBonusRangeDamage()
        {
            return _bonusRangeDamage;
        }

        public void SelectCareer(CareerId id)
        {
            ResetCareerData();
            ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey())._AcquiredCareer=id;

            if (id == CareerId.MinorVampire)
            {
                Hero.MainHero.MakeVampire();
            }
            
            ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey()).AvailableTorSkillPoints=Campaign.Current.MainParty.LeaderHero.Level;
            InitializeCareer();
        }

        private void ResetCareerData()
        {
            _extraAmmo = 0;
            _extraWind = 0;
            _extraHealthPoints = 0;
            _currentSelectedCareerTemplate = null;
            _chargeModifier=0; 
            _damageModifer=0;
            _usagesModifer=0;
            _durationModifier=0;
            _offsetModifier=0;
            _radiusModifier=0;
            _castTimeModifier=0;
            _impactRadius=0;
            _maxDistance=0; 
            _minDistance=0;
            _baseMovementSpeed=0;
            _imbuedStatusEffectDuration=0;
            _windsOfMagicCost=0;
            _careerId = CareerId.None;
            
            //TODO Overrides are still needed
            CleanFromCareerSpecificAttributes();
        }
        

        private void CleanFromCareerSpecificAttributes()
        {
            if(_currentSelectedCareerTemplate==null) return;
            var list = _currentSelectedCareerTemplate.KeyStoneNodes.Select(node => node.CharacterAttribute).ToList();
            
            var info = ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey());

            foreach (var item in list.Where(item => info.AcquiredAbilities.Contains(item)))
            {
                Hero.MainHero.RemoveAttribute(item);
            }
            
        }
    
        
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }

        /// <summary>
        /// Initialize a new Career. Every launch, this is loaded again, to check unlocked points against it. If changes were made, these points were freed.
        /// This should avoid data un
        /// </summary>
        private void InitializeCareer()
        { 
            var info = ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey());
            _torCareerSkillPoints = info.AcquiredAbilitiesTORSkillPointIds;
            _careerId = info._AcquiredCareer;

            _bonusMeleeDamage = new float[(int)DamageType.All + 1];
            _bonusRangeDamage = new float[(int)DamageType.All + 1];
            _bonusSpellDamge = new float[(int)DamageType.All + 1];
            if (_careerId == CareerId.None)
            {
                return;
            }
            

            if (_careerId == CareerId.None&&_torCareerSkillPoints!=null)
            {
                _torCareerSkillPoints.Clear();
            }
            
            _currentSelectedCareerTemplate = CareerFactory.GetTemplate(_careerId);
            //_currentSelectedCareerTemplate = CareerFactory.GetTemplate(CareerId.MinorVampire);
            
            Campaign.Current.MainParty.LeaderHero.AddAttribute("AbilityUser");
            if (_currentSelectedCareerTemplate == null) return;


            _canBeUsedOnHorse = _currentSelectedCareerTemplate.CanBeUsedOnHorse;


            _requiredWeaponTypes = _currentSelectedCareerTemplate.CareerAbilityWeaponRequirements;
            
            var treeElements = _currentSelectedCareerTemplate.PassiveNodes.Cast<CareerTreeNode>().ToList();
            treeElements.AddRange(_currentSelectedCareerTemplate.KeyStoneNodes);
            
            //unlocked Skill points
            
            
            //Are all nodes reachable ? Traverse Tree for all stored ids. For Errors, Count them towards the unspend points

            foreach (var element in _torCareerSkillPoints.SelectMany(id => treeElements.Where(element => element.Id == id)))
            {
                element.State = TreeNodeState.Unlocked;
            }
            
            foreach (var node in _currentSelectedCareerTemplate.PassiveNodes)
            {
                //if (node.State != TreeNodeState.Unlocked) continue;
                AddPassiveNodeEffect(node);
            }

            foreach (var node in _currentSelectedCareerTemplate.KeyStoneNodes)
            {
               // if(node.State != TreeNodeState.Unlocked) continue;
                
            }
            
            var structure = _currentSelectedCareerTemplate.Structure;
            var SortedKeyStones = _currentSelectedCareerTemplate.KeyStoneNodes.OrderBy(x => structure.GetNodeLevel(x.Id));

            foreach (var node in SortedKeyStones)
            {
                AddAbilityModifiers(node); 
                ApplyAbilityOverrides(node);     //don't forget to sort!
            }
        }
        
        /// <summary>
        /// Adding passive effects that will affect regular attacks or spells , NOT the career skill
        /// </summary>
        /// <param name="node">Talent tree "Passive node"</param>
        private void AddPassiveNodeEffect(PassiveNode node)
        {
            switch (node.EffectType)
            {
                case PassiveEffect.HP:
                    _extraHealthPoints += (int)node.Amount;
                    break;
                case PassiveEffect.AP: 
                    _extraAmmo += (int)node.Amount;
                    break;
                case PassiveEffect.WP:
                    _extraWind += (int)node.Amount;
                    break;
                case PassiveEffect.MD:
                    _bonusMeleeDamage[(int) node.DamageType] += node.Amount;   //do not cast to int!
                    break;
                case PassiveEffect.RD:
                    _bonusRangeDamage[(int) node.DamageType] += node.Amount;
                    break;
                case PassiveEffect.SD:
                    _bonusSpellDamge[(int) node.DamageType] += node.Amount;
                    break;
                case PassiveEffect.None:
                    break;
                default:
                    return;
            }
        }

        /// <summary>
        /// Apply modifiers on the Career Skill ability to make it 
        /// </summary>
        /// <param name="node">Talent tree "Key stone"</param>
        private void AddAbilityModifiers(KeyStoneNode node)
        {
            _chargeModifier+= node.Modifier.Charge;
            _damageModifer += node.Modifier.Damage;
            _durationModifier += node.Modifier.Duration;
            _offsetModifier += node.Modifier.Offset;
            _radiusModifier += node.Modifier.Radius;
            _castTimeModifier += node.Modifier.CastTime;
            _usagesModifer += node.Modifier.Usages;
            _impactRadius += node.Modifier.ImpactRadius;
            _maxDistance += node.Modifier.MaxDistance;
            _minDistance += node.Modifier.MinDistance;
            _baseMovementSpeed += node.Modifier.BaseMovementSpeed;
            _imbuedStatusEffectDuration += node.Modifier.ImbuedStatusEffectDuration;
            _windsOfMagicCost += node.Modifier.WindsOfMagicCost;
        }
        
        /// <summary>
        /// Apply overrides to the ability, like changing the damage type from physical to holy.
        /// Former effects are overriden, meaning, the order of execution is important! 
        /// </summary>
        /// <param name="node">Talent tree "Key stone"</param>
        private void ApplyAbilityOverrides(KeyStoneNode node)
        {
            _damageTypeOverride = node.Overrides.DamageType;
        }
        
        
        
        

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
          InitializeCareer();
        }
        
        
    }
    
   
}