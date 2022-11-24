using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using TaleWorlds.CampaignSystem;
using TOR_Core.AbilitySystem.Spells;

namespace TOR_Core.AbilitySystem
{
    public class AbilityComponent : AgentComponent
    {
        public AbilityComponent(Agent agent) : base(agent)
        {
            var abilities = agent.GetSelectedAbilities();
            if (abilities.Count > 0)
            {
                foreach (var item in abilities)
                {
                    try
                    {
                        var ability = AbilityFactory.CreateNew(item, agent);
                        if (ability != null)
                        {
                            ability.OnCastStart += OnCastStart;
                            ability.OnCastComplete += OnCastComplete;

                            switch (ability)
                            {
                                case CareerAbility careerAbility:
                                    _careerAbility = careerAbility;
                                    break;
                                case SpecialMove move:
                                    _specialMove = move;
                                    break;
                                default:
                                {
                                    if(ability.Template.AbilityType != AbilityType.ItemBound) _knownAbilitySystem.Add(ability);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            TORCommon.Log("Attempted to add an ability to agent: " + agent.Character.StringId + ", but it wasn't of type BaseAbility", LogLevel.Warn);
                        }
                    }
                    catch (Exception)
                    {
                        TORCommon.Log("Failed instantiating ability class: " + item, LogLevel.Error);
                    }
                }
                if (Agent.IsVampire() && _specialMove == null) _specialMove = (SpecialMove)AbilityFactory.CreateNew("ShadowStep", Agent);
            }
            if (Agent.CanPlaceArtillery())
            {
                var hero = agent.GetHero();
                if (hero != null)
                {
                    if(hero == Hero.MainHero)
                    {
                        var artilleryRoster = hero.PartyBelongedTo.GetArtilleryItems();
                        if (artilleryRoster.Count > 0)
                        {
                            for (int i = 0; i < artilleryRoster.Count; i++)
                            {
                                var artillery = artilleryRoster[i];
                                var ability = (ItemBoundAbility)AbilityFactory.CreateNew(artillery.EquipmentElement.Item.PrefabName, agent);
                                if (ability != null)
                                {
                                    ability.OnCastStart += OnCastStart;
                                    ability.OnCastComplete += OnCastComplete;
                                    ability.SetChargeNum(artillery.Amount);
                                    _knownAbilitySystem.Add(ability);
                                }
                            }
                        }
                    }
                    else
                    {
                        var ability1 = (ItemBoundAbility)AbilityFactory.CreateNew("GreatCannonSpawner", agent);
                        if (ability1 != null)
                        {
                            ability1.OnCastStart += OnCastStart;
                            ability1.OnCastComplete += OnCastComplete;
                            ability1.SetChargeNum(1);
                            _knownAbilitySystem.Add(ability1);
                        }

                        var ability2 = (ItemBoundAbility)AbilityFactory.CreateNew("MortarSpawner", agent);
                        if (ability2 != null)
                        {
                            ability2.OnCastStart += OnCastStart;
                            ability2.OnCastComplete += OnCastComplete;
                            ability2.SetChargeNum(2);
                            _knownAbilitySystem.Add(ability2);
                        }
                    }
                    
                }
                else if(Game.Current.GameType is CustomGame)
                {
                    var ability1 = (ItemBoundAbility)AbilityFactory.CreateNew("GreatCannonSpawner", agent);
                    if (ability1 != null)
                    {
                        ability1.OnCastStart += OnCastStart;
                        ability1.OnCastComplete += OnCastComplete;
                        ability1.SetChargeNum(2);
                        _knownAbilitySystem.Add(ability1);
                    }

                    var ability2 = (ItemBoundAbility)AbilityFactory.CreateNew("MortarSpawner", agent);
                    if (ability2 != null)
                    {
                        ability2.OnCastStart += OnCastStart;
                        ability2.OnCastComplete += OnCastComplete;
                        ability2.SetChargeNum(2);
                        _knownAbilitySystem.Add(ability2);
                    }
                }
            }
            if (_knownAbilitySystem.Count > 0)
            {
                SelectAbility(0);
            }

           
        }

        private void OnCastStart(Ability ability)
        {
            var manager = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (manager != null)
            {
                manager.OnCastStart(ability, Agent);
            }
        }

        private void OnCastComplete(Ability ability)
        {
            var manager = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (manager != null)
            {
                manager.OnCastComplete(ability, Agent);
            }
        }

        public void InitializeCrosshairs()
        {
            foreach (var ability in KnownAbilitySystem)
            {
                AbilityCrosshair crosshair = AbilityFactory.InitializeCrosshair(ability.Template);
                ability.SetCrosshair(crosshair);
            }
            SelectAbility(0);
        }

        public void SelectAbility(int index)
        {
            if (_knownAbilitySystem.Count > 0)
            {
                CurrentAbility = _knownAbilitySystem[index];
            }
        }

        public void SelectNextAbility()
        {
            if (_currentAbilityIndex < _knownAbilitySystem.Count - 1)
            {
                _currentAbilityIndex++;
            }
            else
            {
                _currentAbilityIndex = 0;
            }
            SelectAbility(_currentAbilityIndex);
        }

        public void SelectPreviousAbility()
        {
            if (_currentAbilityIndex > 0)
            {
                _currentAbilityIndex--;
            }
            else
            {
                _currentAbilityIndex = _knownAbilitySystem.Count - 1;
            }
            SelectAbility(_currentAbilityIndex);
        }

        public void StopSpecialMove()
        {
            ((ShadowStepScript)SpecialMove.AbilityScript)?.Stop();
        }

        public List<AbilityTemplate> GetKnownAbilityTemplates()
        {
            return _knownAbilitySystem.ConvertAll(ability => ability.Template);
        }

        public Ability GetAbility(int index)
        {
            if (_knownAbilitySystem.Count > 0 && index >= 0)
            {
                return _knownAbilitySystem[index % _knownAbilitySystem.Count];
            }

            return null;
        }

        public override void OnTickAsAI(float dt)
        {
            base.OnTickAsAI(dt);
            foreach(var ability in _knownAbilitySystem)
            {
                if (ability.IsActivationPending) ability.ActivateAbility(Agent);
            }
        }

        private Ability _currentAbility = null;
        private SpecialMove _specialMove = null;
        private CareerAbility _careerAbility = null;
        private readonly List<Ability> _knownAbilitySystem = new List<Ability>();
        private int _currentAbilityIndex;
        public Ability CurrentAbility
        {
            get => _currentAbility;
            set
            {
                _currentAbility = value;
                CurrentAbilityChanged?.Invoke(_currentAbility.Crosshair);
            }
        }
        
        public CareerAbility CareerAbility { get => _careerAbility; private set => _careerAbility = value; }
        public SpecialMove SpecialMove { get => _specialMove; private set => _specialMove = value; }
        public List<Ability> KnownAbilitySystem { get => _knownAbilitySystem; }
        public delegate void CurrentAbilityChangedHandler(AbilityCrosshair crosshair);
        public event CurrentAbilityChangedHandler CurrentAbilityChanged;
    }
}