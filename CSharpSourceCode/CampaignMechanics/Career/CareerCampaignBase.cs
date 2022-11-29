using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.SaveSystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Quests;
using TOR_Core.Utilities;

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
        private bool CanBeUsedOnHorse = false;

        private CareerTemplate _currentSelectedCareerTemplate;
        
        //post attack behavior? Scriptname
        
        //modifiers
        private int _chargeModfier;
        private int _damageModifer;
        private int _usagesModifer;
        private float _durationModifier;
        private float _offsetModfier;
        private float _radiusModfier;
        private float _castTimeModifier;
        private float _impactRadius;
        private float _maxDistance;
        private float _minDistance;
        private float _baseMovementSpeed;
        private float _imbuedStatusEffectDuration;
        private float _windsOfMagicCost;
        
        //overrides



        public bool HasRequiredWeaponFlags(WeaponClass weaponClass)
        {
            return _requiredWeaponTypes.Any(item => weaponClass == item);
        }
        public bool CanBeUsedWhileMounted()
        {
            return CanBeUsedOnHorse;
        }
        public int GetExtraHealthPoints()
        {
            return _extraHealthPoints;
        }
        public int GetExtraAmmoPoints()
        {
            return 0;
        }
        
        public int GetExtraWindPoints()
        {
            return 10;
        }
        
        public float[] GetCareerBonusSpellDamage()
        {
            //return _bonusMeleeDamage
            float[] damage = new float[(int)DamageType.All + 1];

            damage[(int)DamageType.Fire] = 0.25f;
            return damage;
        }

        public float[] GetCareerBonusMeleeDamage()
        {
            
            //return _bonusMeleeDamage
            float[] damage = new float[(int)DamageType.All + 1];

            damage[(int)DamageType.Holy] = 0.25f;
            return damage;

        }
        
        public float[] GetCareerBonusRangeDamage()
        {
            //return _bonusMeleeDamage
            float[] damage = new float[(int)DamageType.All + 1];
            damage[(int)DamageType.Lightning] = 0.25f;
            return damage;
        }

        public void SelectCareer(CareerId id)
        {
            if (_currentSelectedCareerTemplate != null)
            {
                //Remove all Career Information
            }
            LoadCareerTemplate(id);
        }


        

        private void  LoadCareerTemplate(CareerId id)
        {
            _currentSelectedCareerTemplate = CareerFactory.GetTemplate(id);
        }
        
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            var info = ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey());
            _torCareerSkillPoints = info.AcquiredAbilitiesTORSkillPoints;
            _careerId = info._AcquiredCareer;
            

            if (_careerId == CareerId.None&&_torCareerSkillPoints!=null)
            {
                _torCareerSkillPoints.Clear();
            }
            
            //dynamically creates all career information, checks all unlocked abilities and applies the buffs, modifiers and overrides accordingly.
            
            //_currentSelectedCareerTemplate = CareerFactory.GetTemplate(_careerId);
            _currentSelectedCareerTemplate = CareerFactory.GetTemplate(CareerId.GrailKnight);
            
            if (_currentSelectedCareerTemplate == null) return;


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
                switch (node.EffectType)
                {
                    case PassiveEffect.HP when node.State == TreeNodeState.Unlocked:
                        _extraHealthPoints = (int)node.Amount;
                        break;
                    case PassiveEffect.AP when node.State == TreeNodeState.Unlocked:
                        _extraAmmo = (int)node.Amount;
                        break;
                    case PassiveEffect.MD when node.State == TreeNodeState.Unlocked:
                        _bonusMeleeDamage[(int) node.DanageType] = (int)node.Amount;
                        break;
                    case PassiveEffect.RD when node.State == TreeNodeState.Unlocked:
                        _bonusRangeDamage[(int) node.DanageType] = (int)node.Amount;
                        break;
                    case PassiveEffect.SD when node.State == TreeNodeState.Unlocked:
                        _bonusSpellDamge[(int) node.DanageType] = (int)node.Amount;
                        break;
                    case PassiveEffect.None:
                        break;
                    case PassiveEffect.WP:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            foreach (var node in _currentSelectedCareerTemplate.KeyStoneNodes)
            {
                if(node.State != TreeNodeState.Unlocked) return;
                
                _chargeModfier+= node.Modifier.Charge;
                _damageModifer += node.Modifier.Damage;
                _durationModifier += node.Modifier.Duration;
                _offsetModfier += node.Modifier.Offset;
                _radiusModfier += node.Modifier.Radius;
                _castTimeModifier += node.Modifier.CastTime;
                _usagesModifer += node.Modifier.Usages;
                _impactRadius += node.Modifier.ImpactRadius;
                _maxDistance += node.Modifier.MaxDistance;
                _minDistance += node.Modifier.MinDistance;
                _baseMovementSpeed += node.Modifier.BaseMovementSpeed;
                _imbuedStatusEffectDuration += node.Modifier.ImbuedStatusEffectDuration;
                _windsOfMagicCost += node.Modifier.WindsOfMagicCost;
            }
            



        }
        
        
    }
    
   
}