using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
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
        private Hero _mainHero;
        
        private int _currentHighestNodeLevel = 0;
        private CareerId _careerId;
        private List<string> _torCareerSkillPoints =new List<string>();

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

        private CareerBody _currentSelectedCareer;
        
        private List<CareerTreeNode>TreeStructure;
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
            if (_currentSelectedCareer == null) return "";
            return _currentSelectedCareer.AbilityTemplateId;
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
            
            Hero.MainHero.ChangeCareer(id);
            Hero.MainHero.SetAvailableCareerTreePoints(Hero.MainHero.Level);
                
            if (id == CareerId.MinorVampire)
            {
                Hero.MainHero.MakeVampire();
            }
            
            _careerId = id;
            InitializeCareer();
        }

        private void ResetCareerData()
        {
            _currentSelectedCareer = null;
            
            _extraAmmo = 0;
            _extraWind = 0;
            _extraHealthPoints = 0;
            
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
            //ClearFromCareerSpecificAttributes();
        }

        private void ResetPassives()
        {
            _extraAmmo = 0;
            _extraWind = 0;
            _extraHealthPoints = 0;
        }
        
            //TODO rework now with attached tree in body
        /*private void ClearFromCareerSpecificAttributes()
        {
            if(_currentSelectedCareer==null) return;
            var list = _currentSelectedCareer.CareerTree.Select(node => node.State).ToList();
            
            var info = ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey());

            foreach (var item in list.Where(item => info.AcquiredAbilities.Contains(item)))
            {
                Hero.MainHero.RemoveAttribute(item);
            }
            
        }*/
    
        
        
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("TreeStructure", ref TreeStructure);
        }

        /// <summary>
        /// Initialize a new Career. Every launch, this is loaded again, to check unlocked points against it. If changes were made, these points were freed.
        /// This should avoid data un
        /// </summary>
        private void InitializeCareer()
        {
            var info = ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey());
            
            _currentHighestNodeLevel = 0;

            _careerId = info.AcquiredCareer;

            _bonusMeleeDamage = new float[(int)DamageType.All + 1];
            _bonusRangeDamage = new float[(int)DamageType.All + 1];
            _bonusSpellDamge = new float[(int)DamageType.All + 1];
            if (_careerId == CareerId.None)
            {
                return;
            }
            
            var careerBody = CareerFactory.GetCareerBody(_careerId);
            if(careerBody.Id == CareerId.None) return;

            _currentSelectedCareer = careerBody;
            Campaign.Current.MainParty.LeaderHero.AddAttribute("AbilityUser");
            _canBeUsedOnHorse = careerBody.CanBeUsedOnHorse;
            _requiredWeaponTypes = careerBody.CareerAbilityWeaponRequirements;

            var maintainedStructure = false;

            maintainedStructure = TreeStructure.IsReplicant(careerBody.CareerTree);

            if (maintainedStructure)
            {
                var passiveNodes = TreeStructure.GetPassiveNodes();

                var usedPoints=0;

                foreach (var node in passiveNodes)
                {
                    if (node.State == TreeNodeState.Unlocked)
                    {
                        AddPassiveNodeEffect(node);
                        usedPoints++;
                    }
                        
                }
                
                var SortedKeyStones = _currentSelectedCareer.CareerTree.GetKeyStoneNodes().OrderBy(x => x.Level);
                foreach (var node in SortedKeyStones)
                {
                    AddAbilityModifiers(node); 
                    ApplyAbilityOverrides(node);     //don't forget to sort!
                }


                var keyStones = TreeStructure.GetKeyStoneNodes().OrderBy(x=> x.Level);
                foreach (var keyStone in keyStones)
                {
                    if (keyStone.State == TreeNodeState.Unlocked)
                    {
                        AddAbilityModifiers(keyStone);
                        ApplyAttribute(keyStone);
                        usedPoints++;
                    }
                        
                }

                info.AvailableTorCareerTreePoints = Campaign.Current.MainParty.LeaderHero.Level - usedPoints;
                
                

            }
            else
            {
                var node = careerBody.CareerTree.GetRootNode();

                node.State = TreeNodeState.Unlocked;

                foreach (var id in node.ChildrenIDs)
                {
                    var child = careerBody.CareerTree.FirstOrDefault(x => x.Id == id);
                    if(child!=null)
                        child.State = TreeNodeState.Available;
                }
                
                
                info.AvailableTorCareerTreePoints = Campaign.Current.MainParty.LeaderHero.Level;
            }

            //else we have to reestablish the tree, reset all points
                

            /*
            if (AcquiredCareer == CareerId.None&&_torCareerSkillPoints!=null)
            {
                
            }
            */
            
          
            Campaign.Current.MainParty.LeaderHero.AddAttribute("AbilityUser");
            if (_currentSelectedCareer == null) return;
            _canBeUsedOnHorse = _currentSelectedCareer.CanBeUsedOnHorse;
            _requiredWeaponTypes = _currentSelectedCareer.CareerAbilityWeaponRequirements;
            TreeStructure = careerBody.CareerTree;

            //var nodes = _currentSelectedCareer.CareerTree;
            //var nodes = _currentSelectedCareer.PassiveNodes.Cast<CareerTreeNode>().ToList();
           // nodes.AddRange(_currentSelectedCareer.KeyStoneNodes);
            
            //unlocked Skill points
            
            //_torCareerSkillPoints.Add("3");
            
            //_torCareerSkillPoints.Add("1");
            
            //_torCareerSkillPoints.Add("10");
            /*if (_torCareerSkillPoints != null && _torCareerSkillPoints.Count > 0)
            {
                foreach (var node in nodes.Where(node => _torCareerSkillPoints.Any(x => node.Id==x)))
                { 
                    if(_currentSelectedCareerTemplate.KeyStoneNodes.Fir((x => x.Id == node.Id)))
                    {
                        _currentSelectedCareerTemplate.KeyStoneNodes.FirstOrDefault((x => x.Id == node.Id));
                    }
                    _currentSelectedCareerTemplate.PassiveNodes.FirstOrDefault((x => x.Id == node.Id)).State=TreeNodeState.Unlocked;
                }

                foreach (var id in _torCareerSkillPoints.Where(id => nodes.All(x => x.Id != id)))
                {
                    ExtendedInfoManager.Instance.GetHeroInfoFor(Hero.MainHero.GetInfoKey()).AcquiredAbilitiesTORSkillPointIds.Remove(id);
                }
            }*/

            //TODO compare with other version
            
            
            
            /*
            //Are all nodes reachable ? Traverse Tree for all stored ids. For Errors, Count them towards the unspend points
            var extraPoints = 0;
            var spendPoints = _torCareerSkillPoints.Count;
            _torCareerSkillPoints.RemoveAll(x => !nodes.Any(y => y.Id.Contains(x)));
            extraPoints = spendPoints - _torCareerSkillPoints.Count;
            var LeftPoints = _torCareerSkillPoints.Count;
            
            foreach (var element in _torCareerSkillPoints.SelectMany(id => nodes.Where(element => element.Id == id)))
            {
                element.State = TreeNodeState.Unlocked;
            }
            
            

            foreach (var node in _currentSelectedCareer.CareerTree.GetPassiveNodes())
            {
                if (node.State != TreeNodeState.Unlocked) continue;
                    AddPassiveNodeEffect(node);
            }*/
            
            

            //_treeStructure = _currentSelectedCareer.CareerTree;
        }


        public bool SelectNode(string nodeID, out string callback)
        {
            
            if (_careerId == CareerId.None)
            {
                callback = "Currently no Career is Selected";
                return false;
            }
            var t= _currentSelectedCareer.CareerTree.FirstOrDefault(x =>nodeID==x.Id);
            if(t!=null)
                if (SelectNode(t))
                {
                    callback = $"assigned Sucessful node {nodeID}";
                    return true;
                }
                else
                {
                    callback = $"Node could not be assigned {nodeID}";
                    return false;
                }
            callback = $"Could not find {nodeID}";
            return false;
        }
        
        
        private bool SelectNode(CareerTreeNode selected)
        {
            if (_mainHero.GetExtendedInfo().AvailableTorCareerTreePoints <= 0)
                return false;
            
            
            var neighbors = _currentSelectedCareer.CareerTree.GetNeighbors(selected);
            
            //Is the selection valid?

            foreach (var neighbor  in neighbors)
            {
                if (neighbor.State == TreeNodeState.Unlocked)
                {
                    _currentSelectedCareer.CareerTree.UnlockNode(selected);

                    if (selected.GetType() == typeof(PassiveNode))
                    {
                        var t = (PassiveNode)selected;
                        AddPassiveNodeEffect(t);
                    }

                    if (selected.GetType() == typeof(KeyStoneNode))
                    {
                        var t = (KeyStoneNode)selected;
                        AddAbilityModifiers(t);
                    }

                    return true;
                }
            }

            return false;


        }


        public void AddNodeIdToCareerPointIds(string id)
        {
          // Campaign.Current.MainParty.LeaderHero.AddCareerPointId(id);
        }
        
        
        
        
        /*private void ResetSpendPoints(List<CareerTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                node.State = TreeNodeState.Locked;
            }
            ClearFromCareerSpecificAttributes();
             = Campaign.Current.MainParty.LeaderHero.Level;
        }*/
        
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
            
            AddNodeIdToCareerPointIds(node.Id);
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
            AddNodeIdToCareerPointIds(node.Id);
        }
        
        /// <summary>
        /// Apply overrides to the ability, like changing the damage type from physical to holy.
        /// Former effects are overriden, meaning, the order of execution is important! 
        /// </summary>
        /// <param name="node">Talent tree "Key stone"</param>
        private void ApplyAbilityOverrides(KeyStoneNode node)
        {
            if (node.Level > _currentHighestNodeLevel)
            {
                _currentHighestNodeLevel = node.Level;
            }
            else return;

            _damageTypeOverride = node.Overrides.DamageType;
        }
        
        private void ApplyAttribute(KeyStoneNode keyStoneNode)
        {
            if(keyStoneNode.CharacterAttribute=="") return;
            var attribute = keyStoneNode.CharacterAttribute;
            _mainHero.AddAttribute(attribute);
        }
        
        
        
        

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            _mainHero= Hero.MainHero;
            InitializeCareer();
        }



        public List<string> GetAllNodes(TreeNodeState state)
        { 
            var nodes = TreeStructure.Where(x => x.State == state);
            var list =new List<string>();
            list.AddRange(nodes.Select(node => node.Id));
            return list;
        }
        
       
        


    }
}