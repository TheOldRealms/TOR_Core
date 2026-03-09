using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.BattleMechanics.AI.CastingAI.Components;
using TOR_Core.BattleMechanics.Crosshairs;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.GameManagers;
using TOR_Core.AbilitySystem.SpellCasting;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Items;
using TOR_Core.Missions;
using TOR_Core.Quests;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        private bool _shouldSheathWeapon;
        private bool _shouldWieldWeapon;
        private bool _shouldPlayIdleCastStanceAnim;
        private bool _hasInitializedForMainAgent;
        private bool _hasAppliedStartingPerkEffects;
        private static MapEvent _lastCatalystGrantedMapEvent;
        private AbilityModeState _currentState = AbilityModeState.Off;
        private EquipmentIndex _mainHand;
        private EquipmentIndex _offHand;
        private AbilityComponent _abilityComponent;
        private GameKeyContext _keyContext = HotKeyManager.GetCategory("CombatHotKeyCategory");
        private static ActionIndexCache? _idleAnimation;
        private ParticleSystem[] _psys = null;
        private readonly string _castingStanceParticleName = "psys_spellcasting_stance";
        private SummonedCombatant _defenderSummoningCombatant;
        private SummonedCombatant _attackerSummoningCombatant;
        private readonly float DamagePortionForChargingCareerAbility = 1f;
        private Dictionary<Team, int> _artillerySlots = [];
        private GameKey _quickCastMenuKey;
        private GameKey _quickCast;
        private GameKey _specialMoveKey;
        private AbilityHUDMissionView _abilityView;
        private int _timeRequestID = 1338;
        private float _lastActivationDeltaTime;
        private float _disableCombatActionsDuration = 0.3f;
        private bool _disableCombatActionsAfterCast;
        private float _elapsedTimeSinceLastActivation;
        private bool _wieldOffHandStaff;

        // Spell cast session tracking
        private readonly Dictionary<int, SpellCastSession> _activeSpellSessions = new();
        private readonly List<SpellCastSession> _pendingCollectSessions = new();
        private int _nextCastId = 1;

        public delegate void OnHideOutBossFightInit();
        public event OnHideOutBossFightInit OnInitHideOutBossFight;
        private static ActionIndexCache IdleAnimation
        {
            get
            {
                if (_idleAnimation == null)
                    _idleAnimation = ActionIndexCache.Create("act_spellcasting_idle");
                return _idleAnimation.Value;
            }
        }
        public AbilityModeState CurrentState => _currentState;

        public bool ShouldSuppressCombatActions => CurrentState == AbilityModeState.Targeting || CurrentState == AbilityModeState.Casting || _disableCombatActionsAfterCast;

        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Mission.OnItemPickUp += OnItemPickup;
        }

        public void InitHideOutBossFight()
        {
            OnInitHideOutBossFight?.Invoke();
        }

        public override void EarlyStart()
        {
            base.EarlyStart();
            _hasAppliedStartingPerkEffects = false;
            OnInitHideOutBossFight = null;
            _abilityView = Mission.Current.GetMissionBehavior<AbilityHUDMissionView>();
            Game.Current.EventManager.RegisterEvent(new Action<MissionPlayerToggledOrderViewEvent>(OnPlayerToggleOrder));
            _quickCastMenuKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey((int)TorKeyMap.QuickCastSelectionMenu);
            _quickCast = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey((int)TorKeyMap.QuickCast);
            _specialMoveKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey((int)TorKeyMap.CareerAbilityCast);
        }

        public override void OnPreMissionTick(float dt)
        {
            // Process any pending spell sessions that are ready to collect
            ProcessPendingSpellSessions();
            TriggeredEffect.ProcessPendingDisposals(Mission.Current.CurrentTime);

            _elapsedTimeSinceLastActivation += dt;
            if (_disableCombatActionsAfterCast && _elapsedTimeSinceLastActivation > (_lastActivationDeltaTime + _disableCombatActionsDuration))
            {
                _disableCombatActionsAfterCast = false;
            }

            if (!_hasInitializedForMainAgent)
            {
                if (Agent.Main != null)
                {
                    _abilityComponent = Agent.Main.GetComponent<AbilityComponent>();
                    SetUpCastStanceParticles();

                    if (!_hasAppliedStartingPerkEffects)
                    {
                        AddPerkEffectsToStartingWindsOfMagic();
                        _hasAppliedStartingPerkEffects = true;
                    }

                    _hasInitializedForMainAgent = true;
                }
            }
            else
            {
                if (_shouldSheathWeapon || _shouldWieldWeapon)
                {
                    UpdateWieldedItems();
                }

                if (IsAbilityModeAvailableForMainAgent())
                {
                    CheckIfMainAgentHasPendingActivation();
                    HandleInput(dt);
                    HandleAnimations();
                }
            }
        }

        private void CacheWieldedItemsForRestore()
        {
            if (_shouldWieldWeapon)
                return;

            _mainHand = Agent.Main.GetPrimaryWieldedItemIndex();
            _offHand = Agent.Main.GetOffhandWieldedItemIndex();
        }

        private void EnableTargetingMode()
        {
            CacheWieldedItemsForRestore();
            _currentState = AbilityModeState.Targeting;
            _abilityView.MissionScreen?.UnregisterRadialMenuObject(_abilityView);

            ChangeKeyBindings();
            SlowDownTime(true);
            SwitchOffhandStanceForStaffs();

            if (_abilityComponent.CurrentAbility.Template.AbilityType == AbilityType.Spell ||
                _abilityComponent.CurrentAbility.Template.AbilityType == AbilityType.Prayer)
            {
                _shouldSheathWeapon = true;
                _shouldPlayIdleCastStanceAnim = true;
                var traitcomp = Agent.Main.GetComponent<ItemTraitAgentComponent>();
                traitcomp?.EnableAllParticles(false);

                EnableCastStanceParticles(true);
            }
            else
            {
                _shouldSheathWeapon = false;
                _shouldPlayIdleCastStanceAnim = false;
            }
        }

        private void SwitchOffhandStanceForStaffs()
        {
            if (!Agent.Main.WieldedOffhandWeapon.IsEmpty)
            {
                if (Agent.Main.WieldedOffhandWeapon.Item.IsMagicalStaff())
                {
                    _wieldOffHandStaff = true;
                    _idleAnimation = ActionIndexCache.Create("act_ready_continue_throwing_axe_with_handshield");
                    return;
                }

            }
            _idleAnimation = ActionIndexCache.Create("act_spellcasting_idle");
            _wieldOffHandStaff = false;
            return;

        }

        private void EnableQuickSelectionMenuMode()
        {
            _currentState = AbilityModeState.QuickMenuSelection;
            _abilityView.MissionScreen?.RegisterRadialMenuObject(_abilityView);
            CacheWieldedItemsForRestore();
            ChangeKeyBindings();
            SlowDownTime(true);
        }

        private void SlowDownTime(bool enable)
        {
            bool isSlowTimeActive = Mission.Current.GetRequestedTimeSpeed(_timeRequestID, out _);
            if (isSlowTimeActive && !enable)
            {
                Mission.Current.RemoveTimeSpeedRequest(_timeRequestID);
                return;
            }
            else if (!isSlowTimeActive && enable)
            {
                Mission.TimeSpeedRequest timeRequest = new(0.3f, _timeRequestID);
                _timeRequestID = timeRequest.RequestID;
                Mission.Current.AddTimeSpeedRequest(timeRequest);
            }
        }

        private void DisableAbilityMode(bool isTakingNewWeapon, TextObject errorMessage)
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
            if (_abilityComponent != null) _abilityComponent.LastCastWasQuickCast = false;

            ChangeKeyBindings();
            SlowDownTime(false);
            _abilityView.MissionScreen?.UnregisterRadialMenuObject(_abilityView);
            var traitcomp = Agent.Main.GetComponent<ItemTraitAgentComponent>();
            traitcomp?.EnableAllParticles(true);

            EnableCastStanceParticles(false);
            if (errorMessage != null)
            {
                _abilityView.DisplayErrorMessage(errorMessage.ToString());
            }
        }

        private void ClearMainAgentAbilityStateAfterRemoval()
        {
            _shouldSheathWeapon = false;
            _shouldWieldWeapon = false;
            _shouldPlayIdleCastStanceAnim = false;
            _disableCombatActionsAfterCast = false;
            _elapsedTimeSinceLastActivation = 0f;
            _wieldOffHandStaff = false;
            _mainHand = EquipmentIndex.None;
            _offHand = EquipmentIndex.None;
            _currentState = AbilityModeState.Off;

            if (_abilityComponent != null)
            {
                _abilityComponent.LastCastWasQuickCast = false;
            }

            SlowDownTime(false);
            _abilityView?.MissionScreen?.UnregisterRadialMenuObject(_abilityView);
            EnableCastStanceParticles(false);
            BindWeaponKeys();
        }

        internal void OnCastStart(Ability ability, Agent agent)
        {
            if (agent == Agent.Main)
            {
                _currentState = AbilityModeState.Casting;
                SlowDownTime(false);
            }

            if (agent.GetHero().HasAnyCareer())
            {
                var playerHero = agent.GetHero();
                var choices = playerHero.GetAllCareerChoices();

                if (choices.Contains("SecretsOfTheGrailPassive3"))
                {
                    if (ability.Template.AbilityType == AbilityType.Prayer)
                    {
                        var choice = TORCareerChoices.GetChoice("SecretsOfTheGrailPassive3");
                        if (choice != null)
                        {
                            float random = MBRandom.RandomFloatRanged(0, 1);
                            if (random < choice.GetPassiveValue())
                            {
                                playerHero.AddWindsOfMagic(10);
                            }
                        }
                    }
                }
            }
        }

        internal void OnCastComplete(Ability ability, Agent agent)
        {
            // Decrement artillery slots for regular artillery, but not for Anvil of Doom
            if (ability is ItemBoundAbility && ability.Template.AbilityEffectType == AbilityEffectType.ArtilleryPlacement
                && ability.Template.StringID != "AnvilOfDoomSpawner")
            {
                if (_artillerySlots.ContainsKey(agent.Team))
                {
                    _artillerySlots[agent.Team]--;
                }
            }

            if (agent == Agent.Main)
            {
                if (CurrentState == AbilityModeState.Casting) DisableAbilityMode(false, null);
                if (Game.Current.GameType is Campaign)
                {
                    var quest = TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>();
                    quest?.IncrementCast();
                }
            }

            if (agent.IsHero && Game.Current.GameType is Campaign)
            {
                var hero = agent.GetHero();
                var model = Campaign.Current.Models.GetAbilityModel();
                if (model != null && hero != null)
                {
                    var skill = model.GetRelevantSkillForAbility(ability.Template);
                    if (skill != null) // Career abilities return null - no skill XP for them
                    {
                        var amount = model.GetSkillXpForCastingAbility(ability.Template);
                        hero.AddSkillXp(skill, amount);
                    }
                }
            }
        }

        private void HandleInput(float dt)
        {
            if (Input.IsKeyDown(InputKey.Tab))
                return;

            if (_currentState == AbilityModeState.QuickMenuSelection || _currentState == AbilityModeState.Targeting)
            {
                if (Input.IsKeyPressed(InputKey.RightMouseButton))
                {
                    DisableAbilityMode(false, null);
                    return;
                }
            }

            switch (_currentState)
            {
                case AbilityModeState.Off:
                    {
                        if (Input.IsKeyPressed(InputKey.RightMouseButton) || Input.IsKeyPressed(InputKey.LeftMouseButton))
                        {
                            if (_abilityComponent.CareerAbility != null && _abilityComponent.CareerAbility.IsActive) _abilityComponent.OnInterrupt();
                        }
                        else if (Input.IsKeyPressed(_quickCastMenuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCastMenuKey.ControllerKey.InputKey))
                        {
                            EnableQuickSelectionMenuMode();
                        }
                        else if (Input.IsKeyPressed(_specialMoveKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_specialMoveKey.ControllerKey.InputKey))
                        {
                            TextObject disabledReason = new("Error Casting Career Ability");
                            if (_abilityComponent.CareerAbility != null && !_abilityComponent.CareerAbility.IsDisabled(Agent.Main, out disabledReason) && IsSniperScopeDisabled())
                            {
                                _abilityComponent.SelectAbility(_abilityComponent.CareerAbility);
                                if (_abilityComponent.CurrentAbility.RequiresTargeting)
                                {
                                    EnableTargetingMode();
                                }
                                else
                                {
                                    if (!Agent.Main.TryCastCurrentAbility(out TextObject failureReason))
                                    {
                                        DisableAbilityMode(false, failureReason);
                                    }
                                    else
                                    {
                                        CacheWieldedItemsForRestore();
                                        _lastActivationDeltaTime = dt;
                                        _elapsedTimeSinceLastActivation = 0;
                                        _disableCombatActionsAfterCast = true;
                                    }
                                }
                            }
                            else
                            {
                                _abilityView.DisplayErrorMessage(disabledReason.ToString());
                            }
                        }
                        else if (Input.IsKeyPressed(_quickCast.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCast.ControllerKey.InputKey))
                        {
                            if (_abilityComponent.CurrentAbility != null && !_abilityComponent.CurrentAbility.IsDisabled(Agent.Main, out _) && IsSniperScopeDisabled())
                            {
                                CacheWieldedItemsForRestore();
                                _abilityComponent.LastCastWasQuickCast = true;
                                if (!Agent.Main.TryCastCurrentAbility(out TextObject failureReason))
                                {
                                    DisableAbilityMode(false, failureReason);
                                }
                            }
                        }
                    }
                    break;
                case AbilityModeState.QuickMenuSelection:
                    {
                        if (!Input.IsKeyDown(_quickCastMenuKey.KeyboardKey.InputKey) && !Input.IsKeyDown(_quickCastMenuKey.ControllerKey.InputKey))
                        {
                            if (_abilityComponent.CurrentAbility.IsDisabled(Agent.Main, out TextObject failureReason))
                            {
                                DisableAbilityMode(false, failureReason);
                                return;
                            }

                            if (_abilityComponent.CurrentAbility.RequiresTargeting)
                            {
                                EnableTargetingMode();
                            }
                            else
                            {
                                if (!Agent.Main.TryCastCurrentAbility(out failureReason))
                                {
                                    DisableAbilityMode(false, failureReason);
                                }
                                else
                                {
                                    CacheWieldedItemsForRestore();
                                    _lastActivationDeltaTime = dt;
                                    _elapsedTimeSinceLastActivation = 0;
                                    _disableCombatActionsAfterCast = true;
                                }
                            }
                        }
                    }
                    break;
                case AbilityModeState.Targeting:
                    {
                        if (Input.IsKeyPressed(InputKey.LeftMouseButton))
                        {
                            bool flag = _abilityComponent.CurrentAbility.Crosshair == null ||
                                        !_abilityComponent.CurrentAbility.Crosshair.IsVisible ||
                                        (_abilityComponent.CurrentAbility.Crosshair.CrosshairType == CrosshairType.SingleTarget &&
                                         !((SingleTargetCrosshair)_abilityComponent.CurrentAbility.Crosshair).IsTargetLocked);
                            if (!flag)
                            {
                                if (!Agent.Main.TryCastCurrentAbility(out TextObject failureReason))
                                {
                                    DisableAbilityMode(false, failureReason);
                                }
                                else
                                {
                                    _lastActivationDeltaTime = dt;
                                    _elapsedTimeSinceLastActivation = 0;
                                    _disableCombatActionsAfterCast = true;
                                }
                            }
                        }
                        else if (Input.IsKeyPressed(_quickCastMenuKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCastMenuKey.ControllerKey.InputKey))
                        {
                            EnableQuickSelectionMenuMode();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void CheckIfMainAgentHasPendingActivation()
        {
            if (_abilityComponent.CurrentAbility.IsActivationPending) _abilityComponent.CurrentAbility.ActivateAbility(Agent.Main);
        }

        private void HandleAnimations()
        {
            if (CurrentState != AbilityModeState.Off)
            {
                var action = Agent.Main.GetCurrentAction(1);
                if (CurrentState == AbilityModeState.Targeting && _shouldPlayIdleCastStanceAnim && action != _idleAnimation)
                {
                    Agent.Main.SetActionChannel(1, IdleAnimation);
                }
            }
        }

        private void UpdateWieldedItems()
        {
            if (Agent.Main == null || !Agent.Main.IsActive())
            {
                ClearMainAgentAbilityStateAfterRemoval();
                return;
            }

            if (_currentState == AbilityModeState.Targeting && _shouldSheathWeapon)
            {
                if (Agent.Main.GetPrimaryWieldedItemIndex() != EquipmentIndex.None)
                {
                    Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.MainHand, Agent.WeaponWieldActionType.WithAnimation);
                    return;
                }

                if (Agent.Main.GetOffhandWieldedItemIndex() != EquipmentIndex.None)
                {
                    if (!Agent.Main.WieldedOffhandWeapon.Item.IsMagicalStaff())
                    {
                        Agent.Main.TryToSheathWeaponInHand(Agent.HandIndex.OffHand, Agent.WeaponWieldActionType.WithAnimation);
                        return;
                    }
                }

                _shouldSheathWeapon = false;
            }

            if (_currentState == AbilityModeState.Off && _shouldWieldWeapon)
            {
                if (_disableCombatActionsAfterCast)
                    return;

                var currentMainHand = Agent.Main.GetPrimaryWieldedItemIndex();
                var currentOffhand = Agent.Main.GetOffhandWieldedItemIndex();

                bool isMainHandRestored = _mainHand == EquipmentIndex.None || currentMainHand == _mainHand;
                bool isOffhandRestored = _offHand == EquipmentIndex.None || currentOffhand == _offHand;

                if (isMainHandRestored && isOffhandRestored)
                {
                    _shouldWieldWeapon = false;
                    return;
                }

                if (_mainHand != EquipmentIndex.None && !isMainHandRestored)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_mainHand, Agent.WeaponWieldActionType.WithAnimation, false);
                    return;
                }

                if (_offHand != EquipmentIndex.None && !isOffhandRestored)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_offHand, Agent.WeaponWieldActionType.WithAnimation, false);
                    return;
                }
            }
        }

        public int GetArtillerySlotsLeftForTeam(Team team)
        {
            _artillerySlots.TryGetValue(team, out int slotsLeft);
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
                foreach (var agent in team.TeamAgents)
                {
                    if (agent.CanPlaceArtillery() || agent.IsHero && agent.HasAttribute("EngineerCompanion"))
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

        public override void OnMissionResultReady(MissionResult missionResult)
        {
            if (missionResult.PlayerDefeated || missionResult.PlayerVictory)
            {
                // Finalize all pending spell sessions before battle ends
                FinalizeAllPendingSessions();

                var agents = Mission.Current.Agents;
                foreach (var agent in agents)
                {
                    if (agent.IsMainAgent && agent.IsActive())
                    {
                        DisableAbilityMode(true, null);
                    }

                    var abilityComponent = agent.GetComponent<AbilityComponent>();
                    if (abilityComponent != null)
                    {
                        var abilities = abilityComponent.KnownAbilitySystem;
                        foreach (var ability in abilities)
                        {
                            ability.DeactivateAbility();
                        }
                    }

                    var comp = agent.GetComponent<StatusEffectComponent>();
                    comp?.Dispose();
                }
            }
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            BindWeaponKeys();
            Mission.OnItemPickUp -= OnItemPickup;

            // Reset Anvil of Doom position for Runelord rune magic
            TriggeredEffect.ClearPendingDisposals(Mission.Current.CurrentTime);
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

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent == Agent.Main)
            {
                ClearMainAgentAbilityStateAfterRemoval();
            }

            if (CareerHelper.IsValidCareerMissionInteractionBetweenAgents(affectorAgent, affectedAgent))
            {
                var attackMask = TORDamageHelper.DetermineMask(blow);
                CareerHelper.ApplyCareerAbilityCharge(1, ChargeType.NumberOfKills, attackMask, affectorAgent, affectedAgent);
            }
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if (CareerHelper.IsValidCareerMissionInteractionBetweenAgents(affectorAgent, affectedAgent))
            {
                var attackMask = TORDamageHelper.DetermineMask(blow);
                CareerHelper.ApplyCareerAbilityCharge(blow.InflictedDamage, ChargeType.DamageDone, attackMask, affectorAgent, affectedAgent, attackCollisionData);

                CareerHelper.ApplyCareerAbilityCharge(blow.InflictedDamage, ChargeType.DamageTaken, attackMask, affectorAgent, affectedAgent, attackCollisionData);
            }
        }

        protected override void OnAgentControllerChanged(Agent agent, AgentControllerType oldController)
        {
            if (agent.Controller == AgentControllerType.Player)
            {
                _hasInitializedForMainAgent = false;
            }
        }

        private bool IsSniperScopeDisabled()
        {
            var behaviour = Mission.Current.GetMissionBehavior<CustomCrosshairMissionBehavior>();
            if (behaviour == null) return true;
            else
            {
                if (behaviour.CurrentCrosshair is SniperScope) return !behaviour.CurrentCrosshair.IsVisible;
                else return true;
            }
        }

        public bool IsCastingMission()
        {
            // Custom combat missions that don't set CombatType properly
            if (Mission.GetMissionBehavior<TrollCaveMissionController>() != null)
                return true;

            return !Mission.IsFriendlyMission &&
                   Mission.CombatType != Mission.MissionCombatType.ArenaCombat &&
                   Mission.CombatType != Mission.MissionCombatType.NoCombat;
        }

        private bool IsAbilityModeAvailableForMainAgent()
        {
            return Agent.Main != null &&
                   Agent.Main.IsActive() &&
                   !ScreenManager.GetMouseVisibility() &&
                   IsCastingMission() &&
                   !Mission.IsInPhotoMode &&
                   !Mission.IsOrderMenuOpen &&
                   (Mission.Mode == MissionMode.Battle ||
                    Mission.Mode == MissionMode.Stealth) &&
                   _abilityComponent != null &&
                   _abilityComponent.CurrentAbility != null;
        }

        private void EnableCastStanceParticles(bool enable)
        {
            if (_psys != null)
            {
                if (_wieldOffHandStaff)
                {
                    _psys[0].SetEnable(enable);
                    return;
                }
                foreach (var psys in _psys)
                {
                    psys?.SetEnable(enable);
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

        private void OnItemPickup(Agent agent, SpawnedItemEntity item)
        {
            if (agent == Agent.Main) DisableAbilityMode(true, null);
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
                (team.Side == BattleSideEnum.Attacker ? _attackerSummoningCombatant
                : team.Side == BattleSideEnum.Defender ? _defenderSummoningCombatant
                : null) ?? throw new NullReferenceException(
                    String.Format("Summoning combatant for team: {0} is null!", team.Side)
                );
            return combatantToReturn;
        }

        private void SetUpCastStanceParticles()
        {
            if (_abilityComponent != null)
            {
                _psys = new ParticleSystem[2];
                _psys[0] = TORParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.DefaultMonster.MainHandItemBoneIndex, out GameEntity entity);
                _psys[1] = TORParticleSystem.ApplyParticleToAgentBone(Agent.Main, _castingStanceParticleName, Game.Current.DefaultMonster.OffHandItemBoneIndex, out entity);
                EnableCastStanceParticles(false);
            }
        }

        private void AddPerkEffectsToStartingWindsOfMagic()
        {
            if (!IsCastingMission())
                return;

            if (Game.Current?.GameType is not Campaign)
                return;
            var mainParty = Campaign.Current.MainParty;

            var currentPlayerMapEvent = MapEvent.PlayerMapEvent;
            bool shouldGrantCatalystForThisMission =
                currentPlayerMapEvent == null ||
                !ReferenceEquals(_lastCatalystGrantedMapEvent, currentPlayerMapEvent);

            // apply to all heroes
            var roster = mainParty.MemberRoster.GetTroopRoster();
            for (int i = 0; i < roster.Count; i++)
            {
                var element = roster[i];
                if (!element.Character.IsHero)
                    continue;

                var hero = element.Character.HeroObject;
                var info = hero.GetExtendedInfo();
                if (info == null)
                    continue;

                if (hero.GetPerkValue(TORPerks.Spellcraft.Improvision) &&
                    info.GetCustomResourceValue("WindsOfMagic") < TORPerks.Spellcraft.Improvision.PrimaryBonus)
                {
                    info.SetCustomResourceValue("WindsOfMagic", TORPerks.Spellcraft.Improvision.PrimaryBonus);
                }

                if (shouldGrantCatalystForThisMission && hero.GetPerkValue(TORPerks.Spellcraft.Catalyst))
                {
                    int magicItemCount = 0;
                    for (int slotIndex = 0; slotIndex < (int)EquipmentIndex.NumEquipmentSetSlots; slotIndex++)
                    {
                        var equipmentElement = hero.BattleEquipment.GetEquipmentFromSlot((EquipmentIndex)slotIndex);
                        var equippedItem = equipmentElement.Item;
                        if (equippedItem != null && equippedItem.IsMagicalItem())
                        {
                            magicItemCount++;
                        }
                    }

                    if (magicItemCount > 0)
                    {
                        info.AddCustomResource(
                            "WindsOfMagic",
                            magicItemCount * TORPerks.Spellcraft.Catalyst.PrimaryBonus
                        );
                    }
                }
            }

            if (shouldGrantCatalystForThisMission && currentPlayerMapEvent != null)
            {
                _lastCatalystGrantedMapEvent = currentPlayerMapEvent;
            }

            var mainHero = Agent.Main?.GetHero();
            if (mainHero != null && mainHero.HasAnyCareer())
            {
                Agent.Main
                    .GetComponent<AbilityComponent>()?
                    .SetIntialPrayerCoolDown();
            }
        }

        private void OnPlayerToggleOrder(MissionPlayerToggledOrderViewEvent @event)
        {
            if (@event.IsOrderEnabled)
            {
                if (_currentState == AbilityModeState.Targeting || _currentState == AbilityModeState.QuickMenuSelection)
                {
                    DisableAbilityMode(false, null);
                }
            }
        }

        public override void OnEarlyAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {
            if (affectedAgent == Agent.Main) SlowDownTime(false);
        }

        /// <summary>
        /// Creates a new spell cast session and returns its CastID.
        /// Called when an ability starts (in AbilityScript.Initialize).
        /// </summary>
        public int CreateSpellSession(Agent caster, AbilityTemplate abilityTemplate)
        {
            var castId = _nextCastId++;
            var session = new SpellCastSession(castId, caster, abilityTemplate);
            _activeSpellSessions[castId] = session;
            return castId;
        }
        
        public void BookSpellDamage(int castId, Agent victim, int damageDealt, int damageAbsorbed, DamageType damageType)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.BookDamage(victim, damageDealt, damageAbsorbed, damageType);
            }
        }
        
        public void BookSpellHealing(int castId, Agent target, int healingDone)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.BookHealing(target, healingDone);
            }
        }
        
        public void BookSpellKill(int castId, Agent victim)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.BookKill(victim);
                return;
            }

            // Also check pending sessions for DOT kills that happen after ability ends
            var pendingSession = _pendingCollectSessions.Find(s => s.CastID == castId);
            pendingSession?.BookKill(victim);
        }

        /// <summary>
        /// Records a tick for lasting effects.
        /// </summary>
        public void RecordSpellTick(int castId)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.RecordTick();
            }
        }

        /// <summary>
        /// Books a status effect application to an active spell session.
        /// </summary>
        public void BookSpellStatusEffect(int castId, Agent target)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.BookStatusEffect(target);
            }
        }

        /// <summary>
        /// Extends the session collect time to wait for status effects to expire.
        /// Called from TriggeredEffect when status effects are applied.
        /// </summary>
        public void ExtendSessionCollectTime(int castId, float duration)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.ExtendCollectTime(duration);
            }
        }

        /// <summary>
        /// Called when an ability ends. If status effects are still pending, queues for later collection.
        /// </summary>
        public void CollectSpellSession(int castId)
        {
            if (!_activeSpellSessions.TryGetValue(castId, out var session))
                return;

            _activeSpellSessions.Remove(castId);

            // If not ready to collect (status effects still pending), queue for later
            if (!session.IsReadyToCollect)
            {
                _pendingCollectSessions.Add(session);
                return;
            }

            FinalizeSession(session);
        }

        /// <summary>
        /// Processes pending sessions that are ready to be collected.
        /// Called from OnPreMissionTick.
        /// </summary>
        private void ProcessPendingSpellSessions()
        {
            for (int i = _pendingCollectSessions.Count - 1; i >= 0; i--)
            {
                var session = _pendingCollectSessions[i];
                if (session.IsReadyToCollect)
                {
                    _pendingCollectSessions.RemoveAt(i);
                    FinalizeSession(session);
                }
            }
        }

        /// <summary>
        /// Finalizes all active and pending sessions immediately.
        /// Called when battle ends to ensure all spell results are displayed.
        /// </summary>
        private void FinalizeAllPendingSessions()
        {
            // Finalize all active sessions
            foreach (var session in _activeSpellSessions.Values)
            {
                FinalizeSession(session);
            }
            _activeSpellSessions.Clear();

            // Finalize all pending sessions
            foreach (var session in _pendingCollectSessions)
            {
                FinalizeSession(session);
            }
            _pendingCollectSessions.Clear();
        }

        /// <summary>
        /// Finalizes a spell session - displays results and grants XP.
        /// </summary>
        private void FinalizeSession(SpellCastSession session)
        {
            if (!session.HasData)
                return;
            
            var sessionAgent = session.Caster;

            // Display results only for player or controlled agent
            bool shouldDisplay = sessionAgent == Agent.Main || sessionAgent.BelongsToMainParty();

            if (shouldDisplay)
            {
                string spellName = session.AbilityTemplate?.Name?.ToString();

                if (session.TotalDamageDealt > 0)
                {
                    TORDamageDisplay.DisplayAggregateSpellDamage(
                        session.PrimaryDamageType,
                        session.TotalDamageDealt,
                        session.AgentsDamagedCount,
                        session.AgentsKilledCount,
                        spellName);
                }

                if (session.TotalHealingDone > 0)
                {
                    TORDamageDisplay.DisplayAggregateSpellHealing(
                        session.TotalHealingDone,
                        session.AgentsHealedCount,
                        spellName);
                }

                if (session.TotalFriendlyFireDamage > 0)
                {
                    TORDamageDisplay.DisplayAggregateSpellFriendlyFire(
                        session.TotalFriendlyFireDamage,
                        session.AgentsFriendlyFiredCount,
                        session.AgentsFriendlyKilledCount,
                        spellName);
                }
            }

            // Grant XP for all hero casters (player and companions)
            if (Game.Current.GameType is Campaign && session.CasterHero != null)
            {
                var model = Campaign.Current.Models.GetAbilityModel();
                if (model != null)
                {
                    var skill = model.GetRelevantSkillForAbility(session.AbilityTemplate);
                    if (skill != null) // Career abilities return null - no skill XP for them
                    {
                        var xpAmount = model.CalculateSpellSessionXp(session);
                        if (xpAmount > 0)
                        {
                            session.CasterHero.AddSkillXp(skill, xpAmount);

                            // DarkVisionPassive3 - also grants Roguery XP
                            if (session.CasterHero.HasAnyCareer() && session.CasterHero.HasCareerChoice("DarkVisionPassive3"))
                            {
                                session.CasterHero.AddSkillXp(DefaultSkills.Roguery, xpAmount);
                            }
                        }
                    }
                }
            }
        }
    }

    public enum AbilityModeState
    {
        Off,
        QuickMenuSelection,
        Targeting,
        Casting
    }
}