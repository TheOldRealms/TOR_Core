using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.Scripts;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class AbilityComponent : AgentComponent
    {
        private Ability _currentAbility = null;
        private readonly List<Ability> _knownAbilitySystem = new List<Ability>();
        public bool LastCastWasQuickCast;
        public delegate void CurrentAbilityChangedHandler(AbilityCrosshair crosshair);
        public event CurrentAbilityChangedHandler CurrentAbilityChanged;
        public CareerAbility CareerAbility { get; private set; }
        public List<Ability> KnownAbilitySystem { get => _knownAbilitySystem; }//Sly : unfortunate name as these are the selected abilities available to them in the current mission, but the name overlaps with their actual known abilities list in hero extended info.

        // Anvil of Doom position for Runelord proximity check
        public  Vec3 AnvilOfDoomPosition { get; set; } = Vec3.Invalid;
        public bool IsAnvilPlaced { get; set; } = false;
        public const float AnvilProximityRadius = 15f;


        public AbilityComponent(Agent agent) : base(agent)
        {
            if (Game.Current.GameType is Campaign && agent?.GetHero() != null && agent.GetHero() == Hero.MainHero)
            {
                var career = Hero.MainHero.GetCareer();
                if (career != null)
                {
                    var ability = AbilityFactory.CreateNew(career.AbilityTemplateID, agent);
                    if (ability != null && ability is CareerAbility)
                    {
                        CareerAbility = (CareerAbility)ability;
                        CareerAbility.OnCastStart += OnCastStart;
                        CareerAbility.OnCastComplete += OnCastComplete;
                        _knownAbilitySystem.Add(CareerAbility);
                    }
                }
            }
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
                            if (ability.Template.AbilityType != AbilityType.ItemBound) _knownAbilitySystem.Add(ability);
                        }
                        else
                        {
                            TORCommon.Log("Attempted to add an ability to agent: " + agent.Character.StringId + ", but it wasn't of type BaseAbility. Item was " + item + ".", LogLevel.Warn);
                        }
                    }
                    catch (Exception)
                    {
                        TORCommon.Log("Failed instantiating ability class: " + item, LogLevel.Error);
                    }
                }
            }

            if (Agent.CanPlaceArtillery() && CanUseDeployableArtilleryAbilities())
            {
                var hero = agent?.GetHero();
                if (hero != null)
                {
                    if (hero == Hero.MainHero)
                    {
                        var artilleryRoster = hero.PartyBelongedTo.GetArtilleryItems();
                        if (MobileParty.MainParty.GetMaxNumberOfArtillery() > 0 && artilleryRoster.Count > 0)
                        {
                            for (int i = 0; i < artilleryRoster.Count; i++)
                            {
                                var artillery = artilleryRoster[i];
                                var ability = (ItemBoundAbility)AbilityFactory.CreateNew(artillery.EquipmentElement.Item.PrefabName, agent);
                                if (ability != null)
                                {
                                    ability.OnCastStart += OnCastStart;
                                    ability.OnCastComplete += OnCastComplete;
                                    ability.SetChargeNum(artillery.Amount, true);
                                    _knownAbilitySystem.Add(ability);
                                }
                            }
                        }
                    }
                    else if (hero.Culture?.StringId == TORConstants.Cultures.EMPIRE)
                    {
                        var ability1 = (ItemBoundAbility)AbilityFactory.CreateNew("GreatCannonSpawner", agent);
                        if (ability1 != null)
                        {
                            ability1.OnCastStart += OnCastStart;
                            ability1.OnCastComplete += OnCastComplete;
                            ability1.SetChargeNum(1, true);
                            _knownAbilitySystem.Add(ability1);
                        }

                        var ability2 = (ItemBoundAbility)AbilityFactory.CreateNew("MortarSpawner", agent);
                        if (ability2 != null)
                        {
                            ability2.OnCastStart += OnCastStart;
                            ability2.OnCastComplete += OnCastComplete;
                            ability2.SetChargeNum(2, true);
                            _knownAbilitySystem.Add(ability2);
                        }
                    }
                    else if (hero.Culture?.StringId == TORConstants.Cultures.BRETONNIA)
                    {
                        var ability3 = (ItemBoundAbility)AbilityFactory.CreateNew("FieldTrebuchetSpawner", agent);
                        if (ability3 != null)
                        {
                            ability3.OnCastStart += OnCastStart;
                            ability3.OnCastComplete += OnCastComplete;
                            ability3.SetChargeNum(2, true);
                            _knownAbilitySystem.Add(ability3);
                        }
                    }
                    else if (hero.Culture?.StringId == TORConstants.Cultures.DAWI)
                    {
                        var ability4 = (ItemBoundAbility)AbilityFactory.CreateNew("DwarfCannonSpawner", agent);
                        if (ability4 != null)
                        {
                            ability4.OnCastStart += OnCastStart;
                            ability4.OnCastComplete += OnCastComplete;
                            ability4.SetChargeNum(1, true);
                            _knownAbilitySystem.Add(ability4);
                        }
                    }


                }
                else if (Game.Current.GameType is CustomGame)
                {
                    var heroChar = Agent.Character;
                    if (heroChar.Culture?.StringId == TORConstants.Cultures.EMPIRE)
                    {
                        var ability1 = (ItemBoundAbility)AbilityFactory.CreateNew("GreatCannonSpawner", agent);
                        if (ability1 != null)
                        {
                            ability1.OnCastStart += OnCastStart;
                            ability1.OnCastComplete += OnCastComplete;
                            ability1.SetChargeNum(1, true);
                            _knownAbilitySystem.Add(ability1);
                        }

                        var ability2 = (ItemBoundAbility)AbilityFactory.CreateNew("MortarSpawner", agent);
                        if (ability2 != null)
                        {
                            ability2.OnCastStart += OnCastStart;
                            ability2.OnCastComplete += OnCastComplete;
                            ability2.SetChargeNum(2, true);
                            _knownAbilitySystem.Add(ability2);
                        }
                    }
                    else if (heroChar.Culture?.StringId == TORConstants.Cultures.BRETONNIA)
                    {
                        var ability3 = (ItemBoundAbility)AbilityFactory.CreateNew("FieldTrebuchetSpawner", agent);
                        if (ability3 != null)
                        {
                            ability3.OnCastStart += OnCastStart;
                            ability3.OnCastComplete += OnCastComplete;
                            ability3.SetChargeNum(2, true);
                            _knownAbilitySystem.Add(ability3);
                        }
                    }
                    else if (heroChar.Culture?.StringId == TORConstants.Cultures.DAWI)
                    {
                        var ability4 = (ItemBoundAbility)AbilityFactory.CreateNew("DwarfCannonSpawner", agent);
                        if (ability4 != null)
                        {
                            ability4.OnCastStart += OnCastStart;
                            ability4.OnCastComplete += OnCastComplete;
                            ability4.SetChargeNum(1, true);
                            _knownAbilitySystem.Add(ability4);
                        }
                    }
                }
            }

            // Add Anvil of Doom spawner for Runelords
            if (agent.IsHero && agent.GetHero() != null)
            {
                if (agent.GetHero().HasCareer(TORCareers.Runelord) && agent.HasPartyAnvilOfDoom())
                {
                    var anvilAbility = (ItemBoundAbility)AbilityFactory.CreateNew("AnvilOfDoomSpawner", agent);
                    if (anvilAbility != null)
                    {
                        anvilAbility.OnCastStart += OnCastStart;
                        anvilAbility.OnCastComplete += OnCastComplete;
                        anvilAbility.SetChargeNum(1, true);
                        _knownAbilitySystem.Add(anvilAbility);
                    }
                }
            }



            if (_knownAbilitySystem.Count > 0)
            {
                SelectAbility(0);
            }
        }

        ~AbilityComponent()
        {
            IsAnvilPlaced = false;
        }

        private bool CanUseDeployableArtilleryAbilities() // disabling artillery for sieges
        {
            return !Agent.Mission.IsSiegeBattle;
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
        }

        public void SelectAbility(Ability ability)
        {
            if (KnownAbilitySystem.Contains(ability)) CurrentAbility = ability;
        }

        public void SelectAbility(int index)
        {
            if (_knownAbilitySystem.Count > 0)
            {
                CurrentAbility = _knownAbilitySystem[index];
            }
        }
        public void OnInterrupt()
        {
            if (CareerAbility?.AbilityScript is ShadowStepScript shadowStepScript)
            {
                shadowStepScript.Stop();
            }
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

        public int GetCurrentAbilityIndex()
        {
            for (var index = 0; index < _knownAbilitySystem.Count; index++)
            {
                var ability = _knownAbilitySystem[index];
                if (ability == CurrentAbility)
                    return index;
            }

            return 0;
        }

        public override void OnTick(float dt)
        {
            foreach (var ability in _knownAbilitySystem)
            {
                ability.TickCastingState();

                if (ability.IsActivationPending)
                    ability.ActivateAbility(Agent);
            }
        }

        public Ability CurrentAbility
        {
            get => _currentAbility;
            set
            {
                _currentAbility = value;
                CurrentAbilityChanged?.Invoke(_currentAbility.Crosshair);
            }
        }

        public void SetIntialPrayerCoolDown()
        {
            foreach (var ability in _knownAbilitySystem.Where(ability => ability.Template.AbilityType == AbilityType.Prayer || ability.Template.BelongsToLoreID == "RuneMagic"))
            {
                ExplainedNumber cooldown = new ExplainedNumber(ability.Template.CoolDown);
                if (this.Agent.IsMainAgent)
                {
                    if (Game.Current.GameType is Campaign)
                    {
                        CareerHelper.ApplyBasicCareerPassives(Agent.GetHero(), ref cooldown, PassiveEffectType.PrayerCoolDownReduction, true);


                        if (Hero.MainHero.HasCareerChoice("CrusherOfTheWeakPassive4") || Hero.MainHero.HasCareerChoice("ArchLectorPassive1"))
                        {
                            ability.SetCoolDown(0);
                            return;
                        }
                    }
                }

                ability.SetCoolDown((int)cooldown.ResultNumber - 15);
            }
        }
        public void SetPrayerCoolDown(int time)
        {
            foreach (var ability in _knownAbilitySystem)
            {
                if (ability.Template.AbilityType == AbilityType.Prayer)
                {
                    if (!ability.IsOnCooldown() || (ability.GetCoolDownLeft() < time))
                    {
                        ability.SetCoolDown(time);
                    }
                }
            }
        }
    }
}