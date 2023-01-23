using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.Utilities;
using TOR_Core.Extensions;
using TOR_Core.BattleMechanics.AI.AgentBehavior.Components;
using TOR_Core.Items;
using TOR_Core.BattleMechanics.Crosshairs;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TaleWorlds.CampaignSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.GameManagers;
using TOR_Core.Quests;

namespace TOR_Core.AbilitySystem
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        private bool _shouldSheathWeapon;
        private bool _shouldWieldWeapon;
        private bool _hasInitializedForMainAgent;
        private AbilityModeState _currentState = AbilityModeState.Off;
        private EquipmentIndex _mainHand;
        private EquipmentIndex _offHand;
        private AbilityComponent _abilityComponent;
        private GameKeyContext _keyContext = HotKeyManager.GetCategory("CombatHotKeyCategory");
        private static readonly ActionIndexCache _idleAnimation = ActionIndexCache.Create("act_spellcasting_idle");
        private ParticleSystem[] _psys = null;
        private readonly string _castingStanceParticleName = "psys_spellcasting_stance";
        private SummonedCombatant _defenderSummoningCombatant;
        private SummonedCombatant _attackerSummoningCombatant;
        private readonly float DamagePortionForChargingCareerAbility = 0.25f;
        private Dictionary<Team, int> _artillerySlots = new Dictionary<Team, int>();

        private GameKey _spellcastingModeKey;
        private GameKey _nextAbilitySelection;
        private GameKey _previousAbilitySelection;
        private GameKey _quickCast;
        private GameKey _specialMoveKey;

        public AbilityModeState CurrentState => _currentState;

        public int GetArtillerySlotsLeftForTeam(Team team)
        {
            int slotsLeft = 0;
            _artillerySlots.TryGetValue(team, out slotsLeft);
            return slotsLeft;
        }

        public override void OnTeamDeployed(Team team)
        {
            InitTeam(team);
        }

        private void InitTeam(Team team)
        {
            if (team is null || team.TeamAgents.IsEmpty())
                return;

            if (team.Side == BattleSideEnum.Attacker && _attackerSummoningCombatant == null)
            {
                var culture = team.Leader == null ? team.TeamAgents.FirstOrDefault().Character.Culture : team.Leader.Character.Culture;
                _attackerSummoningCombatant = new SummonedCombatant(team, culture);
            }
            else if (team.Side == BattleSideEnum.Defender && _defenderSummoningCombatant == null)
            {
                var culture = team.Leader == null ? team.TeamAgents.FirstOrDefault().Character.Culture : team.Leader.Character.Culture;
                _defenderSummoningCombatant = new SummonedCombatant(team, culture);
            }
            RefreshMaxArtilleryCountForTeam(team);
        }

        private void RefreshMaxArtilleryCountForTeam(Team team)
        {
            if (_artillerySlots.ContainsKey(team))
            {
                _artillerySlots[team] = 0;
                foreach(var agent in team.TeamAgents)
                {
                    if (agent.CanPlaceArtillery())
                    {
                        _artillerySlots[team] += agent.GetPlaceableArtilleryCount();
                    }
                }
            }
            else
            {
                _artillerySlots.Add(team, 0);
                RefreshMaxArtilleryCountForTeam(team);
            }
        }

        protected override void OnEndMission()
        {
            BindWeaponKeys();
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            var comp = affectorAgent.GetComponent<AbilityComponent>();
            if (comp != null)
            {
                if (comp.CareerAbility != null && comp.CareerAbility.ChargeType == ChargeType.DamageDone) comp.CareerAbility.AddCharge(blow.InflictedDamage * DamagePortionForChargingCareerAbility);
            }
        }

        public override void OnMissionTick(float dt)
        {
            if (!_hasInitializedForMainAgent)
            {
                if(Agent.Main != null)
                {
                    SetUpCastStanceParticles();
                    AddPerkEffectsToStartingWindsOfMagic();
                    _hasInitializedForMainAgent = true;
                }
            }
            else if (IsAbilityModeAvailableForMainAgent())
            {
                CheckIfMainAgentHasPendingActivation();
                
                HandleInput();

                UpdateWieldedItems();

                HandleAnimations();
            }
        }

        private void CheckIfMainAgentHasPendingActivation()
        {
            if (_abilityComponent.CurrentAbility.IsActivationPending) _abilityComponent.CurrentAbility.ActivateAbility(Agent.Main);
        }

        private void HandleAnimations()
        {
            if(CurrentState != AbilityModeState.Off)
            {
                var action = Agent.Main.GetCurrentAction(1);
                if(CurrentState == AbilityModeState.Idle && action != _idleAnimation)
                {
                    Agent.Main.SetActionChannel(1, _idleAnimation);
                }
            }
        }

        internal void OnCastComplete(Ability ability, Agent agent)
        {
            if(ability is ItemBoundAbility && ability.Template.AbilityEffectType == AbilityEffectType.ArtilleryPlacement)
            {
                if (_artillerySlots.ContainsKey(agent.Team))
                {
                    _artillerySlots[agent.Team]--;
                }
            }

            if(agent == Agent.Main)
            {
                if (CurrentState == AbilityModeState.Casting) _currentState = AbilityModeState.Idle;
                if (Game.Current.GameType is Campaign)
                {
                    var quest = TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>();
                    if (quest != null)
                    {
                        quest.IncrementCast();
                    }
                }
            }

            if (agent.IsHero && Game.Current.GameType is Campaign)
            {
                var hero = agent.GetHero();
                var model = Campaign.Current.Models.GetSpellcraftModel();
                if (model != null && hero != null)
                {
                    var skill = model.GetRelevantSkillForAbility(ability.Template);
                    var amount = model.GetSkillXpForCastingAbility(ability.Template);
                    hero.AddSkillXp(skill, amount);
                }
            }
        }

        internal void OnCastStart(Ability ability, Agent agent) 
        {
            if(agent == Agent.Main)
            {
                if (CurrentState == AbilityModeState.Idle) _currentState = AbilityModeState.Casting;
            }
        }

        private void UpdateWieldedItems()
        {
            if (_currentState == AbilityModeState.Idle && _shouldSheathWeapon)
            {
                if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand) != EquipmentIndex.None)
                {
                    Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                }
                else if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand) != EquipmentIndex.None)
                {
                    Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
                }
                else
                {
                    _shouldSheathWeapon = false;
                }
            }
            if (_currentState == AbilityModeState.Off && _shouldWieldWeapon)
            {
                if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand) != _mainHand)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_mainHand, Agent.WeaponWieldActionType.WithAnimation, false);
                }
                else if (Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand) != _offHand)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_offHand, Agent.WeaponWieldActionType.WithAnimation, false);
                }
                else
                {
                    _shouldWieldWeapon = false;
                }
            }
        }

        private void HandleInput()
        {
            //Turning ability mode on/off

            if (Input.IsKeyPressed(_specialMoveKey.KeyboardKey.InputKey) ||
                Input.IsKeyPressed(_specialMoveKey.ControllerKey.InputKey))
            {
                if( _abilityComponent != null && _abilityComponent.CareerAbility != null)
                    if (_currentState == AbilityModeState.Off  &&
                        IsCurrentCrossHairCompatible())
                    {
                        _abilityComponent.CareerAbility.TryCast(Agent.Main);
                    }
            }
            
            if(Input.IsKeyPressed(_nextAbilitySelection.KeyboardKey.InputKey)||Input.IsKeyPressed(_nextAbilitySelection.ControllerKey.InputKey))
                Agent.Main.SelectNextAbility();
            
            if(Input.IsKeyPressed(_previousAbilitySelection.KeyboardKey.InputKey)||Input.IsKeyPressed(_previousAbilitySelection.ControllerKey.InputKey))
                Agent.Main.SelectPreviousAbility();

            if (Input.IsKeyPressed(_quickCast.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCast.ControllerKey.InputKey))
            {
                if(_abilityComponent != null && _abilityComponent.CurrentAbility.AbilityEffectType != AbilityEffectType.SeekerMissile)
                    Agent.Main.CastCurrentAbility();
            }
               
            
            if (Input.IsKeyPressed(_spellcastingModeKey.KeyboardKey.InputKey)||Input.IsKeyPressed(_spellcastingModeKey.ControllerKey.InputKey))
            {
                switch (_currentState)
                {
                    case AbilityModeState.Off:
                        EnableAbilityMode();
                        break;
                    case AbilityModeState.Idle:
                        DisableAbilityMode(false);
                        break;
                    default:
                        break;
                }
            }
            else if (Input.IsKeyPressed(InputKey.LeftMouseButton))
            {
                bool flag = _abilityComponent.CurrentAbility.Crosshair == null ||
                            !_abilityComponent.CurrentAbility.Crosshair.IsVisible ||
                            _currentState != AbilityModeState.Idle ||
                            (_abilityComponent.CurrentAbility.Crosshair.CrosshairType == CrosshairType.SingleTarget &&
                            !((SingleTargetCrosshair)_abilityComponent.CurrentAbility.Crosshair).IsTargetLocked);
                if (!flag)
                {
                    Agent.Main.CastCurrentAbility();
                }
                if(_abilityComponent.CareerAbility != null && _abilityComponent.CareerAbility.IsActive) _abilityComponent.OnInterrupt();
            }
            else if (Input.IsKeyPressed(InputKey.RightMouseButton))
            {
                if (_abilityComponent.CareerAbility != null && _abilityComponent.CareerAbility.IsActive) _abilityComponent.OnInterrupt();
            }
            else if (Input.IsKeyPressed(InputKey.MouseScrollUp) && _currentState != AbilityModeState.Off)
            {
                Agent.Main.SelectNextAbility();
            }
            else if (Input.IsKeyPressed(InputKey.MouseScrollDown) && _currentState != AbilityModeState.Off)
            {
                Agent.Main.SelectPreviousAbility();
            }
        }

        private bool IsCurrentCrossHairCompatible()
        {
            var behaviour = Mission.Current.GetMissionBehavior<CustomCrosshairMissionBehavior>();
            if (behaviour == null) return true;
            else
            {
                if (behaviour.CurrentCrosshair is SniperScope) return !behaviour.CurrentCrosshair.IsVisible;
                else return true;
            }
        }

        public override void OnAgentCreated(Agent agent)
        {
            if (IsCastingMission())
            {
                if (agent.IsAbilityUser())
                {
                    agent.AddComponent(new AbilityComponent(agent));
                    if (agent.IsAIControlled)
                    {
                        agent.AddComponent(new WizardAIComponent(agent));
                    }
                }
            }
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            _spellcastingModeKey=  HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("Spellcasting");
            _nextAbilitySelection = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("NextAbility");
            _previousAbilitySelection = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("PreviousAbility");
            _quickCast = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("QuickCast");
            _specialMoveKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("SpecialMove");
        }

        public bool IsCastingMission()
        {
            return !Mission.IsFriendlyMission &&
                    Mission.CombatType != Mission.MissionCombatType.ArenaCombat &&
                    Mission.CombatType != Mission.MissionCombatType.NoCombat;
;
        }

        private bool IsAbilityModeAvailableForMainAgent()
        {
            return Agent.Main != null &&
                   Agent.Main.IsActive() &&
                   !ScreenManager.GetMouseVisibility() &&
                   IsCastingMission() &&
                   !(ScreenManager.TopScreen as MissionScreen).IsPhotoModeEnabled &&
                   (Mission.Mode == MissionMode.Battle ||
                    Mission.Mode == MissionMode.Stealth) &&
                   _abilityComponent != null &&
                   _abilityComponent.CurrentAbility != null;
                   
        }

        private void EnableAbilityMode()
        {
            _mainHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.MainHand);
            _offHand = Agent.Main.GetWieldedItemIndex(Agent.HandIndex.OffHand);
            _shouldSheathWeapon = true;
            _currentState = AbilityModeState.Idle;
            ChangeKeyBindings();
            var traitcomp = Agent.Main.GetComponent<ItemTraitAgentComponent>();
            if (traitcomp != null)
            {
                traitcomp.EnableAllParticles(false);
            }
            EnableCastStanceParticles(true);
        }

        private void DisableAbilityMode(bool isTakingNewWeapon)
        {
            if (isTakingNewWeapon)
            {
                _mainHand = EquipmentIndex.None;
                _offHand = EquipmentIndex.None;
            }
            else
            {
                _shouldWieldWeapon = true;
            }
            _currentState = AbilityModeState.Off;
            ChangeKeyBindings();
            var traitcomp = Agent.Main.GetComponent<ItemTraitAgentComponent>();
            if (traitcomp != null)
            {
                traitcomp.EnableAllParticles(true);
            }
            EnableCastStanceParticles(false);
        }
        private void EnableCastStanceParticles(bool enable)
        {
            if(_psys != null)
            {
                foreach (var psys in _psys)
                {
                    if(psys != null)
                    {
                        psys.SetEnable(enable);
                    }
                }
            }
        }

        private void ChangeKeyBindings()
        {
            if (_abilityComponent != null && _currentState != AbilityModeState.Off)
            {
                UnbindWeaponKeys();
            }
            else
            {
                BindWeaponKeys();
            }
        }

        private void BindWeaponKeys()
        {
            _keyContext.GetGameKey(11).KeyboardKey.ChangeKey(InputKey.MouseScrollUp);
            _keyContext.GetGameKey(12).KeyboardKey.ChangeKey(InputKey.MouseScrollDown);
            _keyContext.GetGameKey(18).KeyboardKey.ChangeKey(InputKey.Numpad1);
            _keyContext.GetGameKey(19).KeyboardKey.ChangeKey(InputKey.Numpad2);
            _keyContext.GetGameKey(20).KeyboardKey.ChangeKey(InputKey.Numpad3);
            _keyContext.GetGameKey(21).KeyboardKey.ChangeKey(InputKey.Numpad4);
        }

        private void UnbindWeaponKeys()
        {
            _keyContext.GetGameKey(11).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(12).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(18).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(19).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(20).KeyboardKey.ChangeKey(InputKey.Invalid);
            _keyContext.GetGameKey(21).KeyboardKey.ChangeKey(InputKey.Invalid);
        }

        public override void OnItemPickup(Agent agent, SpawnedItemEntity item)
        {
            if(agent == Agent.Main) DisableAbilityMode(true);
        }

        public SummonedCombatant GetSummoningCombatant(Team team)
        {
            // OnFormationTroopsSpawned() isn't always called by missions
            // ex. hideout missions
            // and thus we need to add an extra check to make sure that 
            // summoning combatants are initialized properly.
            if (_attackerSummoningCombatant == null
                || _defenderSummoningCombatant == null)
            {
                InitTeam(Mission.Current.Teams.Attacker);
                InitTeam(Mission.Current.Teams.Defender);
            }

            var combatantToReturn =
                team.Side == BattleSideEnum.Attacker ? _attackerSummoningCombatant 
                : team.Side == BattleSideEnum.Defender ? _defenderSummoningCombatant 
                    : null;

            if (combatantToReturn == null)
            {
                // Crash the thread early to make it easier to debug instead of
                // letting it the thread die on TalesWorld's end.
                throw new NullReferenceException(
                    String.Format("Summoning combatant for team: {0} is null!", team.Side)
                );
            }

            return combatantToReturn;
        }

        protected override void OnAgentControllerChanged(Agent agent, Agent.ControllerType oldController)
        {
            if (agent.Controller == Agent.ControllerType.Player)
            {
                _hasInitializedForMainAgent = false;
            }
        }

        private void SetUpCastStanceParticles()
        {
            _abilityComponent = Agent.Main.GetComponent<AbilityComponent>();
            if (_abilityComponent != null)
            {
                _psys = new ParticleSystem[2];
                GameEntity entity;
                _psys[0] = TORParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.DefaultMonster.MainHandItemBoneIndex, out entity);
                _psys[1] = TORParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.DefaultMonster.OffHandItemBoneIndex, out entity);
                EnableCastStanceParticles(false);
            }
        }

        private void AddPerkEffectsToStartingWindsOfMagic()
        {
            var hero = Agent.Main?.GetHero();
            if(hero != null)
            {
                var info = hero.GetExtendedInfo();
                if(info != null)
                {
                    if (hero.GetPerkValue(TORPerks.SpellCraft.Improvision) && info.CurrentWindsOfMagic < TORPerks.SpellCraft.Improvision.PrimaryBonus)
                    {
                        info.CurrentWindsOfMagic = TORPerks.SpellCraft.Improvision.PrimaryBonus;
                    }
                    if (hero.GetPerkValue(TORPerks.SpellCraft.Catalyst))
                    {
                        int magicItemCount = 0;
                        for(int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
                        {
                            var equipmentElement = hero.BattleEquipment.GetEquipmentFromSlot((EquipmentIndex)i);
                            var equippedItem = equipmentElement.Item;
                            if (equippedItem != null && equippedItem.IsMagicalItem()) magicItemCount++;
                        }
                        if(magicItemCount > 0)
                        {
                            info.CurrentWindsOfMagic += magicItemCount * TORPerks.SpellCraft.Catalyst.PrimaryBonus;
                            if (info.CurrentWindsOfMagic > info.MaxWindsOfMagic) info.CurrentWindsOfMagic = info.MaxWindsOfMagic;
                        }
                    }
                }
            }
        }
    }

    public enum AbilityModeState
    {
        Off,
        Idle,
        Casting
    }
}