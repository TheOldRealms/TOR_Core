using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.AbilitySystem.SpellCasting;
using TOR_Core.Battle.CrosshairMissionBehavior;
using TOR_Core.BattleMechanics.AI.CastingAI.Components;
using TOR_Core.BattleMechanics.Crosshairs;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.GameManagers;
using TOR_Core.Items;
using TOR_Core.Missions;
using TOR_Core.Quests;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        private DefaultBattleMissionAgentSpawnLogic _missionAgentSpawnLogic;
        private bool _shouldSheathWeapon;
        private bool _shouldWieldWeapon;
        private bool _shouldPlayIdleCastStanceAnim;
        private bool _hasInitializedForMainAgent;
        private bool _hasAppliedStartingPerkEffects;
        private static MapEvent _lastCatalystGrantedMapEvent;
        private AbilityModeState _currentState = AbilityModeState.Off;
        private EquipmentIndex _mainHand = EquipmentIndex.None;
        private EquipmentIndex _offHand = EquipmentIndex.None;
        private AbilityComponent _abilityComponent;
        private GameKeyContext _keyContext = HotKeyManager.GetCategory("CombatHotKeyCategory");
        private static ActionIndexCache? _idleAnimation;
        private ParticleSystem[] _psys = null;
        private GameEntity[] _castStanceParticleEntities = null;
        private Agent _castStanceParticleAgent;
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
        private readonly Queue<QueuedSpellDamage> _queuedSpellDamage = new();
        private readonly Dictionary<int, int> _queuedSpellDamageCountByCastId = new();
        private readonly Queue<QueuedSpellHealing> _queuedSpellHealing = new();
        private readonly Dictionary<int, int> _queuedSpellHealingCountByCastId = new();
        private readonly Queue<QueuedStatusDotDamage> _queuedStatusDotDamage = new();
        private readonly Dictionary<int, int> _queuedStatusDotDamageCountByCastId = new();
        private readonly Queue<QueuedStatusHealing> _queuedStatusHealing = new();
        private readonly Queue<TriggeredEffectQueueItem> _queuedTriggeredStatusEffects = new();
        private readonly Dictionary<int, List<TriggeredEffectQueueItem>> _triggeredStatusEffectsWaitingForPrimaryHit = new();
        private readonly Dictionary<int, int> _queuedTriggeredStatusEffectCountByCastId = new();
        private readonly Queue<TriggeredEffectQueueItem> _queuedTriggeredCosmetics = new();
        private readonly Dictionary<int, int> _pendingTriggeredEffectPrimaryWork = new();
        private int _nextCastId = 1;
        private int _nextTriggeredEffectResolutionId = 1;
        private int _spellBlowsAppliedThisTick;
        private int _largestQueuedSpellDamageCount;
        private const int ABILITY_WORK_BUDGET_PER_TICK = 12; // for spells, statuses, triggered effects
        private const int SPELL_DAMAGE_QUEUE_LOG_THRESHOLD = 24;
        private const bool ENABLE_LOG_SPELLS = true; //fn

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

            TORSummonHelper.ResetInitialSpawnedTroopCount();

            _missionAgentSpawnLogic = Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
            if (_missionAgentSpawnLogic != null)
            {
                _missionAgentSpawnLogic.OnInitialTroopsSpawned += OnInitialTroopsSpawned;
            }
        }

        public override void OnPreMissionTick(float dt)
        {
            // Process any pending spell sessions that are ready to collect
            _spellBlowsAppliedThisTick = 0;
            ProcessQueuedAbilityWork();
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
                        AddPerkEffectsToStartingWindsOfMagic();//Sly : this tick occurs when deployment begins which ends up allowing things like prayer cooldowns to count down while formations are being rearranged.
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
        public override void OnClearScene()
        {
            if (_missionAgentSpawnLogic != null)
            {
                _missionAgentSpawnLogic.OnInitialTroopsSpawned -= OnInitialTroopsSpawned;
                _missionAgentSpawnLogic = null;
            }

            TORSummonHelper.ResetInitialSpawnedTroopCount();

            base.OnClearScene();
        }

        private void OnInitialTroopsSpawned(BattleSideEnum battleSide, int numberOfTroopsSpawned)
        {
            TORSummonHelper.RegisterInitialTroopsSpawned(numberOfTroopsSpawned);
        }
        private void CacheWieldedItemsForRestore()
        {
            if (_shouldWieldWeapon)
                return;

            _wieldOffHandStaff = false;
            _mainHand = Agent.Main.GetPrimaryWieldedItemIndex();
            _offHand = Agent.Main.GetOffhandWieldedItemIndex();
        }

        private static bool ShouldKeepOffhandStaffWielded(MissionWeapon offhandWeapon)
        {
            if (offhandWeapon.IsEmpty)
            {
                return false;
            }

            return offhandWeapon.Item.StringId.IndexOf("staff", StringComparison.OrdinalIgnoreCase) >= 0;
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
            _wieldOffHandStaff = ShouldKeepOffhandStaffWielded(Agent.Main.WieldedOffhandWeapon);
            _idleAnimation = _wieldOffHandStaff
                ? ActionIndexCache.Create("act_ready_continue_throwing_axe_with_handshield")
                : ActionIndexCache.Create("act_spellcasting_idle");
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
            // only restore if cast actually changed wield state
            else
            {
                var currentMainHand = Agent.Main?.GetPrimaryWieldedItemIndex() ?? EquipmentIndex.None;
                var currentOffHand = Agent.Main?.GetOffhandWieldedItemIndex() ?? EquipmentIndex.None;

                bool needsMainHandRestore = _mainHand != EquipmentIndex.None && currentMainHand != _mainHand;
                bool needsOffHandRestore = !_wieldOffHandStaff && _offHand != EquipmentIndex.None && currentOffHand != _offHand;

                _shouldWieldWeapon = needsMainHandRestore || needsOffHandRestore;
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
            RemoveCastStanceParticles();
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
            // Decrement artillery slots
            if (ability.Template.AbilityEffectType == AbilityEffectType.ArtilleryPlacement)
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
                                    CacheWieldedItemsForRestore();
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
                                CacheWieldedItemsForRestore();
                                if (!Agent.Main.TryCastCurrentAbility(out failureReason))
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
                    if (!ShouldKeepOffhandStaffWielded(Agent.Main.WieldedOffhandWeapon))
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
                bool isOffhandRestored = _wieldOffHandStaff || _offHand == EquipmentIndex.None || currentOffhand == _offHand;

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

                if (!_wieldOffHandStaff && _offHand != EquipmentIndex.None && !isOffhandRestored)
                {
                    Agent.Main.TryToWieldWeaponInSlot(_offHand, Agent.WeaponWieldActionType.WithAnimation, false);
                    return;
                }
            }
        }

        public int GetArtillerySlotsLeftForTeam(Team team)
        {
            if (_artillerySlots.TryGetValue(team, out int slotsLeft))
            {
                return slotsLeft;
            }
            
            return 0;
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
            if (team.GeneralAgent == null) return;
            var artillerySlots = team.GeneralAgent.GetOriginMobileParty()?.GetMaxNumberOfArtillery() ?? team.GeneralAgent.GetPlaceableArtilleryCount();
            //The backup agent lookup is to handle the custom battle case where a team is not derived from a party origin.


            if (artillerySlots <= 0) return;

            if (_artillerySlots.ContainsKey(team))
            {
                _artillerySlots[team] = artillerySlots;
            }
            else
            {
                _artillerySlots.Add(team, artillerySlots);
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
            ClampExceedingWinds();

            if (_missionAgentSpawnLogic != null)
            {
                _missionAgentSpawnLogic.OnInitialTroopsSpawned -= OnInitialTroopsSpawned;
                _missionAgentSpawnLogic = null;
            }

            TORSummonHelper.ResetInitialSpawnedTroopCount();
            BindWeaponKeys();
            Mission.OnItemPickUp -= OnItemPickup;

            RemoveCastStanceParticles();
            _queuedSpellDamage.Clear();
            _queuedSpellDamageCountByCastId.Clear();
            _queuedSpellHealing.Clear();
            _queuedSpellHealingCountByCastId.Clear();
            _queuedStatusDotDamage.Clear();
            _queuedStatusDotDamageCountByCastId.Clear();
            _queuedStatusHealing.Clear();
            _queuedTriggeredStatusEffects.Clear();
            _triggeredStatusEffectsWaitingForPrimaryHit.Clear();
            _queuedTriggeredStatusEffectCountByCastId.Clear();
            _queuedTriggeredCosmetics.Clear();
            _pendingTriggeredEffectPrimaryWork.Clear();

            // Reset Anvil of Doom position for Runelord rune magic
            TriggeredEffect.ClearPendingDisposals(Mission.Current.CurrentTime);
        }

        public override void OnAgentCreated(Agent agent)
        {
            if (IsCastingMission())
            {
                if (agent.IsAbilityUser())
                {
                    var abilityComponent = new AbilityComponent(agent);
                    agent.AddComponent(abilityComponent);
                    if (agent.IsAIControlled && abilityComponent.KnownAbilitySystem.Count > 0)
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

        private void RemoveCastStanceParticles() // cast stance visuals needing entity and bone cleanup

        {
            if (_psys == null)
            {
                return;
            }

            for (int i = 0; i < _psys.Length; i++)
            {
                var particle = _psys[i];
                TORParticleSystem.RemoveParticleFromAgentBone(_castStanceParticleAgent, particle);

                var entity = _castStanceParticleEntities != null && i < _castStanceParticleEntities.Length
                    ? _castStanceParticleEntities[i]
                    : null;

                if (entity == null)
                {
                    continue;
                }

                entity.RemoveAllParticleSystems();
                if (_castStanceParticleAgent == null ||
                    !_castStanceParticleAgent.HasUsableVisuals() ||
                    !_castStanceParticleAgent.TryRemoveChildEntity(entity))
                {
                    entity.Remove(0);
                }
            }

            _psys = null;
            _castStanceParticleEntities = null;
            _castStanceParticleAgent = null;
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
                RemoveCastStanceParticles();

                _castStanceParticleAgent = Agent.Main;
                _psys = new ParticleSystem[2];
                _castStanceParticleEntities = new GameEntity[2];
                _psys[0] = TORParticleSystem.ApplyParticleToAgentBone(_castStanceParticleAgent, _castingStanceParticleName, Game.Current.DefaultMonster.MainHandItemBoneIndex, out _castStanceParticleEntities[0]);
                _psys[1] = TORParticleSystem.ApplyParticleToAgentBone(_castStanceParticleAgent, _castingStanceParticleName, Game.Current.DefaultMonster.OffHandItemBoneIndex, out _castStanceParticleEntities[1]);
                EnableCastStanceParticles(false);
            }
        }
        private void ClampExceedingWinds()
        {
            if (Game.Current?.GameType is not Campaign)
            {
                return;
            }

            var roster = Campaign.Current.MainParty.MemberRoster.GetTroopRoster();
            for (int i = 0; i < roster.Count; i++)
            {
                var character = roster[i].Character;
                if (!character.IsHero)
                {
                    continue;
                }

                var info = character.HeroObject.GetExtendedInfo();
                if (info == null)
                {
                    continue;
                }

                var maximumWindsOfMagic = info.MaxWindsOfMagic;
                if (info.GetCustomResourceValue("WindsOfMagic") > maximumWindsOfMagic)
                {
                    info.SetCustomResourceValue("WindsOfMagic", maximumWindsOfMagic);
                }
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
                            magicItemCount * TORPerks.Spellcraft.Catalyst.PrimaryBonus,
                            allowWindsOfMagicOverMaximum: true
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
                return;
            }

            var pendingSession = _pendingCollectSessions.Find(s => s.CastID == castId);
            pendingSession?.BookDamage(victim, damageDealt, damageAbsorbed, damageType);
        }
        private static bool IsMissionAgentReferenceCurrent(Agent agent)
        {
            var mission = Mission.Current;
            if (mission == null ||
                mission.MissionEnded ||
                mission.IsMissionEnding ||
                mission.MissionIsEnding ||
                agent == null)
            {
                return false;
            }

            return mission.FindAgentWithIndex(agent.Index) == agent;
        }

        private static bool IsLiveMissionAgent(Agent agent, bool requireHuman = false)
        {
            if (!IsMissionAgentReferenceCurrent(agent))
            {
                return false;
            }

            if (requireHuman && !agent.IsHuman)
            {
                return false;
            }

            return agent.IsActive() && agent.Health >= 1f && !agent.IsFadingOut();
        }

        private static bool CanQueueTriggeredStatusTarget(Agent target, Agent applierAgent)
        {
            if (!IsLiveMissionAgent(target, requireHuman: true))
            {
                return false;
            }

            return applierAgent == null || IsMissionAgentReferenceCurrent(applierAgent);
        }

        public int BeginTriggeredEffectResolution()
        {
            return _nextTriggeredEffectResolutionId++;
        }

        public void QueueTriggeredEffectSound(string effectId, string soundEffectId, Vec3 position, int castId)
        {
            soundEffectId = soundEffectId?.Trim();
            if (string.IsNullOrWhiteSpace(soundEffectId) ||
                soundEffectId.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _queuedTriggeredCosmetics.Enqueue(TriggeredEffectQueueItem.ForSound(effectId, soundEffectId, position, castId));
        }

        public void QueueTriggeredEffectVisual(string effectId, string burstPrefab, float fadeOutTime, Vec3 position, Vec3 normal, int castId)
        {
            burstPrefab = burstPrefab?.Trim();
            if (string.IsNullOrWhiteSpace(burstPrefab) ||
                burstPrefab.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _queuedTriggeredCosmetics.Enqueue(TriggeredEffectQueueItem.ForVisual(effectId, burstPrefab, fadeOutTime, position, normal, castId));
        }

        public void QueueTriggeredStatusEffect(Agent target, StatusEffectTemplate statusEffectTemplate, Agent applierAgent, float duration, bool append, bool isMutated, int castId, int resolutionId = -1)
        {
            if (statusEffectTemplate == null || !CanQueueTriggeredStatusTarget(target, applierAgent))
            {
                return;
            }

            QueueTriggeredStatusEffect(
                TriggeredEffectQueueItem.ForStatus(target, statusEffectTemplate, applierAgent, duration, append, isMutated, stackStatusEffect: false, castId, resolutionId));
        }

        public void QueueTriggeredStatusEffect(Agent target, string effectId, Agent applierAgent, float duration, bool append, bool isMutated, bool stackStatusEffect = false, int castId = -1, int resolutionId = -1)
        {
            effectId = effectId?.Trim();
            if (string.IsNullOrWhiteSpace(effectId) || !CanQueueTriggeredStatusTarget(target, applierAgent))
            {
                return;
            }

            QueueTriggeredStatusEffect(
                TriggeredEffectQueueItem.ForStatus(target, effectId, applierAgent, duration, append, isMutated, stackStatusEffect, castId, resolutionId));
        }

        private void QueueTriggeredStatusEffect(TriggeredEffectQueueItem work)
        {
            TrackTriggeredStatusEffect(work.CastId);

            // status work is processed before damage and heal queues; keep associated status pending until all priority work for this effect is completed
            if (IsTriggeredEffectPrimaryWorkPending(work.ResolutionId))
            {
                if (!_triggeredStatusEffectsWaitingForPrimaryHit.TryGetValue(work.ResolutionId, out var waitingStatusEffects))
                {
                    waitingStatusEffects = new List<TriggeredEffectQueueItem>();
                    _triggeredStatusEffectsWaitingForPrimaryHit.Add(work.ResolutionId, waitingStatusEffects);
                }

                waitingStatusEffects.Add(work);
                return;
            }

            _queuedTriggeredStatusEffects.Enqueue(work);
        }

        public void QueueStatusDotDamage(Agent target, int damage, Vec3 impactPosition, Agent applier, int castId)
        {
            if (damage <= 0 || !IsLiveMissionAgent(target, requireHuman: true))
            {
                return;
            }

            if (applier != null && !IsMissionAgentReferenceCurrent(applier))
            {
                return;
            }

            var statusDotDamage = new QueuedStatusDotDamage(target, damage, impactPosition, applier, castId);

            _queuedStatusDotDamage.Enqueue(statusDotDamage);
            TrackQueuedStatusDotDamage(statusDotDamage.CastId);
        }

        public void QueueStatusHealing(Agent target, int healing)
        {
            if (healing <= 0 || !IsLiveMissionAgent(target))
            {
                return;
            }

            _queuedStatusHealing.Enqueue(new QueuedStatusHealing(target, healing));
        }

        public void ApplySpellHealinginBudget(Agent target, int healing, Agent healer, AbilityTemplate abilityTemplate, int castId, int resolutionId = -1)
        {
            if (healing <= 0)
            {
                return;
            }

            var spellHealing = new QueuedSpellHealing(target, healing, healer, abilityTemplate, castId, resolutionId);
            if (!CanApplyResolvedSpellHealing(spellHealing))
            {
                return;
            }

            EnqueueSpellHealing(spellHealing);
        }

        public void ApplySpellDamageinBudget(Agent target, int damage, Vec3 impactPosition, Agent caster, DamageType damageType, AbilityTemplate abilityTemplate, TriggeredEffectTemplate triggeredEffectTemplate, bool hasShockWave, int castId, int resolutionId = -1, bool bookSpellResult = true)
        {
            if (damage <= 0 ||
                !IsLiveMissionAgent(target, requireHuman: true) ||
                caster == null ||
                !IsMissionAgentReferenceCurrent(caster))
            {
                return;
            }

            EnqueueSpellDamage(new QueuedSpellDamage(target, damage, impactPosition, caster, damageType, abilityTemplate, triggeredEffectTemplate?.StringID, hasShockWave, abilityTemplate != null, castId, resolutionId, bookSpellResult));
        }

        public void QueueOnHitSecondaryDamage(Agent target, int damage, Vec3 impactPosition, Agent source = null, bool originatesFromAbility = false)
        {
            if (damage <= 0 || !IsLiveMissionAgent(target, requireHuman: true))
            {
                return;
            }

            if (source != null && !IsMissionAgentReferenceCurrent(source))
            {
                return;
            }

            EnqueueSpellDamage(new QueuedSpellDamage(target, damage, impactPosition, source, DamageType.Physical, null, null, hasShockWave: false, originatesFromAbility: originatesFromAbility, castId: -1, resolutionId: -1, bookSpellResult: false));
        }

        private void ProcessQueuedAbilityWork()
        {
            // each queued call fully returns through the engine before the next hit or health change begins (inherited from the native kill all implementation; whether this is a good approach remains to be seen)
            if (!HasQueuedGameplayWork())
            {
                while (_spellBlowsAppliedThisTick < ABILITY_WORK_BUDGET_PER_TICK && TryProcessTriggeredEffectCosmetic())
                {
                }

                return;
            }

            if (_queuedTriggeredCosmetics.Count > 0)
            {
                TryProcessTriggeredEffectCosmetic();
            }

            bool processedWork;
            do
            {
                processedWork = false;
                processedWork |= TryProcessTriggeredStatusEffect();
                processedWork |= TryProcessQueuedSpellDamage();
                processedWork |= TryProcessQueuedSpellHealing();
                processedWork |= TryProcessQueuedStatusDotDamage();
                processedWork |= TryProcessQueuedStatusHealing();
            }
            while (processedWork && _spellBlowsAppliedThisTick < ABILITY_WORK_BUDGET_PER_TICK);

            if (!HasQueuedGameplayWork())
            {
                while (_spellBlowsAppliedThisTick < ABILITY_WORK_BUDGET_PER_TICK && TryProcessTriggeredEffectCosmetic())
                {
                }
            }
        }

        private bool TryProcessTriggeredStatusEffect()
        {
            if (_queuedTriggeredStatusEffects.Count == 0 ||
                _spellBlowsAppliedThisTick >= ABILITY_WORK_BUDGET_PER_TICK)
            {
                return false;
            }

            while (_queuedTriggeredStatusEffects.Count > 0)
            {
                var work = _queuedTriggeredStatusEffects.Dequeue();
                if (!CanQueueTriggeredStatusTarget(work.Target, work.ApplierAgent))
                {
                    CompleteTriggeredStatusEffect(work.CastId);
                    continue;
                }

                var budgetBeforeWork = _spellBlowsAppliedThisTick;
                ApplyResolvedTriggeredStatusEffect(work);
                CompleteTriggeredStatusEffect(work.CastId);
                ConsumeQueuedWorkBudgetIfNothingResolved(budgetBeforeWork);
                return true;
            }

            return false;
        }

        private bool TryProcessTriggeredEffectCosmetic()
        {
            if (_queuedTriggeredCosmetics.Count == 0 ||
                _spellBlowsAppliedThisTick >= ABILITY_WORK_BUDGET_PER_TICK)
            {
                return false;
            }

            var budgetBeforeWork = _spellBlowsAppliedThisTick;
            ApplyResolvedTriggeredEffectWork(_queuedTriggeredCosmetics.Dequeue());
            ConsumeQueuedWorkBudgetIfNothingResolved(budgetBeforeWork);
            return true;
        }

        private bool TryProcessQueuedSpellDamage()
        {
            if (_queuedSpellDamage.Count == 0 ||
                _spellBlowsAppliedThisTick >= ABILITY_WORK_BUDGET_PER_TICK)
            {
                return false;
            }

            while (_queuedSpellDamage.Count > 0)
            {
                var spellDamage = _queuedSpellDamage.Dequeue();

                if (!CanApplyResolvedSpellDamage(spellDamage))
                {
                    CompleteQueuedSpellDamage(spellDamage.CastId);
                    CompleteTriggeredEffectPrimaryWork(spellDamage.ResolutionId);
                    continue;
                }

                var budgetBeforeWork = _spellBlowsAppliedThisTick;
                ApplyResolvedSpellDamage(spellDamage);
                CompleteQueuedSpellDamage(spellDamage.CastId);
                CompleteTriggeredEffectPrimaryWork(spellDamage.ResolutionId);
                ConsumeQueuedWorkBudgetIfNothingResolved(budgetBeforeWork);
                return true;
            }

            return false;
        }


        private bool TryProcessQueuedSpellHealing()
        {
            if (_queuedSpellHealing.Count == 0 ||
                _spellBlowsAppliedThisTick >= ABILITY_WORK_BUDGET_PER_TICK)
            {
                return false;
            }

            while (_queuedSpellHealing.Count > 0)
            {
                var spellHealing = _queuedSpellHealing.Dequeue();
                if (!CanApplyResolvedSpellHealing(spellHealing))
                {
                    CompleteQueuedSpellHealing(spellHealing.CastId);
                    CompleteTriggeredEffectPrimaryWork(spellHealing.ResolutionId);
                    continue;
                }

                var budgetBeforeWork = _spellBlowsAppliedThisTick;
                ApplyResolvedSpellHealing(spellHealing);
                CompleteQueuedSpellHealing(spellHealing.CastId);
                CompleteTriggeredEffectPrimaryWork(spellHealing.ResolutionId);
                ConsumeQueuedWorkBudgetIfNothingResolved(budgetBeforeWork);
                return true;
            }

            return false;
        }

        private bool TryProcessQueuedStatusDotDamage()
        {
            if (_queuedStatusDotDamage.Count == 0 ||
                _spellBlowsAppliedThisTick >= ABILITY_WORK_BUDGET_PER_TICK)
            {
                return false;
            }

            while (_queuedStatusDotDamage.Count > 0)
            {
                var statusDotDamage = _queuedStatusDotDamage.Dequeue();

                if (!CanApplyResolvedStatusDotDamage(statusDotDamage))
                {
                    CompleteQueuedStatusDotDamage(statusDotDamage.CastId);
                    continue;
                }

                var budgetBeforeWork = _spellBlowsAppliedThisTick;
                ApplyResolvedStatusDotDamage(statusDotDamage);
                CompleteQueuedStatusDotDamage(statusDotDamage.CastId);
                ConsumeQueuedWorkBudgetIfNothingResolved(budgetBeforeWork);
                return true;
            }

            return false;
        }

        private bool TryProcessQueuedStatusHealing()
        {
            if (_queuedStatusHealing.Count == 0 ||
                _spellBlowsAppliedThisTick >= ABILITY_WORK_BUDGET_PER_TICK)
            {
                return false;
            }

            while (_queuedStatusHealing.Count > 0)
            {
                var statusHealing = _queuedStatusHealing.Dequeue();
                if (!CanApplyResolvedStatusHealing(statusHealing))
                {
                    continue;
                }

                var budgetBeforeWork = _spellBlowsAppliedThisTick;
                ApplyResolvedStatusHealing(statusHealing);
                ConsumeQueuedWorkBudgetIfNothingResolved(budgetBeforeWork);
                return true;
            }

            return false;
        }

        private void ConsumeQueuedWorkBudgetIfNothingResolved(int budgetBeforeWork)
        {
            if (_spellBlowsAppliedThisTick == budgetBeforeWork)
            {
                _spellBlowsAppliedThisTick++;
            }
        }

        private void ApplyResolvedSpellDamage(QueuedSpellDamage spellDamage)
        {
            if (!CanApplyResolvedSpellDamage(spellDamage))
            {
                return;
            }

            spellDamage.Target.ApplyDamage(
                spellDamage.Damage,
                spellDamage.ImpactPosition,
                spellDamage.Caster,
                doBlow: true,
                hasShockWave: spellDamage.HasShockWave,
                originatesFromAbility: spellDamage.OriginatesFromAbility);

            _spellBlowsAppliedThisTick++;

            if (!spellDamage.BookSpellResult || spellDamage.CastId < 0)
            {
                return;
            }

            BookSpellDamage(spellDamage.CastId, spellDamage.Target, spellDamage.Damage, 0, spellDamage.DamageType);

            if (spellDamage.Target.Health <= 0 ||
                spellDamage.Target.State == AgentState.Killed ||
                spellDamage.Target.State == AgentState.Unconscious)
            {
                BookSpellKill(spellDamage.CastId, spellDamage.Target);
            }
        }

        private void ApplyResolvedSpellHealing(QueuedSpellHealing spellHealing)
        {
            if (!CanApplyResolvedSpellHealing(spellHealing))
            {
                return;
            }

            spellHealing.Target.Heal(spellHealing.Healing);
            _spellBlowsAppliedThisTick++;

            if (spellHealing.CastId >= 0)
            {
                BookSpellHealing(spellHealing.CastId, spellHealing.Target, spellHealing.Healing);
            }
        }

        private void ApplyResolvedStatusDotDamage(QueuedStatusDotDamage statusDotDamage)
        {
            if (!CanApplyResolvedStatusDotDamage(statusDotDamage))
            {
                return;
            }

            statusDotDamage.Target.ApplyDamage(statusDotDamage.Damage, statusDotDamage.ImpactPosition, statusDotDamage.Applier, doBlow: false, hasShockWave: false);
            _spellBlowsAppliedThisTick++;

            if (statusDotDamage.CastId < 0)
            {
                return;
            }

            if (statusDotDamage.Target.Health <= 0 ||
                statusDotDamage.Target.State == AgentState.Killed ||
                statusDotDamage.Target.State == AgentState.Unconscious)
            {
                BookSpellKill(statusDotDamage.CastId, statusDotDamage.Target);
            }
        }

        private void ApplyResolvedStatusHealing(QueuedStatusHealing statusHealing)
        {
            if (!CanApplyResolvedStatusHealing(statusHealing))
            {
                return;
            }

            statusHealing.Target.Heal(statusHealing.Healing);
            _spellBlowsAppliedThisTick++;
        }

        private void ApplyResolvedTriggeredEffectWork(TriggeredEffectQueueItem work)
        {
            if (Mission.Current == null || Mission.Current.MissionEnded || Mission.Current.IsMissionEnding || Mission.Current.MissionIsEnding)
            {
                return;
            }

            switch (work.Kind)
            {
                case TriggeredEffectQueueItemKind.Sound:
                    ApplyResolvedTriggeredSound(work);
                    break;
                case TriggeredEffectQueueItemKind.Visual:
                    ApplyResolvedTriggeredVisual(work);
                    break;
            }
        }

        private void ApplyResolvedTriggeredSound(TriggeredEffectQueueItem work)
        {
            var soundIndex = SoundEvent.GetEventIdFromString(work.SoundEffectId);
            if (soundIndex < 0)
            {
                TORCommon.Log(
                    "missing triggered effect sound" + " | effect=" + work.EffectId + " | sound=" + work.SoundEffectId,
                    NLog.LogLevel.Warn);

                return;
            }

            Mission.Current.MakeSound(soundIndex, work.Position, soundCanBePredicted: false, isReliable: false, relatedAgent1: -1, relatedAgent2: -1);
            _spellBlowsAppliedThisTick++;
        }

        private void ApplyResolvedTriggeredVisual(TriggeredEffectQueueItem work)
        {
            var effect = GameEntity.CreateEmpty(Mission.Current.Scene);
            MatrixFrame frame = MatrixFrame.Identity;
            ParticleSystem.CreateParticleSystemAttachedToEntity(work.BurstParticleEffectPrefab, effect, ref frame);
            var effectForward = work.Normal;
            if (Math.Abs(effectForward.x) + Math.Abs(effectForward.y) + Math.Abs(effectForward.z) < 0.0001f)
            {
                effectForward = Vec3.Forward;
            }

            var globalFrame = new MatrixFrame(Mat3.CreateMat3WithForward(in effectForward), work.Position);
            effect.SetGlobalFrame(globalFrame);
            effect.FadeOut(work.FadeOutTime, true);

            _spellBlowsAppliedThisTick++;
        }

        private void ApplyResolvedTriggeredStatusEffect(TriggeredEffectQueueItem work)
        {
            if (!CanQueueTriggeredStatusTarget(work.Target, work.ApplierAgent))
            {
                return;
            }

            bool applied = TORMissionHelper.ApplyStatusEffectToAgent(work.Target, work.StatusEffectId, work.ApplierAgent, work.StatusEffectDuration, work.AppendStatusEffect, work.IsMutatedStatusEffect, work.StackStatusEffect, work.CastId);
            if (!applied)
            {
                return;
            }

            _spellBlowsAppliedThisTick++;
            BookResolvedTriggeredStatusEffect(work);
        }

        private void BookResolvedTriggeredStatusEffect(TriggeredEffectQueueItem work)
        {
            if (work.CastId < 0 || work.StatusEffectTemplate == null)
            {
                return;
            }

            BookSpellStatusEffect(work.CastId, work.Target);

            int expectedTicks = (int)work.StatusEffectDuration;
            int expectedValuePerTarget = (int)(expectedTicks * work.StatusEffectTemplate.BaseEffectValue);

            if (work.StatusEffectTemplate.Type == StatusEffectTemplate.EffectType.DamageOverTime)
            {
                BookSpellDamage(work.CastId, work.Target, expectedValuePerTarget, 0, work.StatusEffectTemplate.DamageType);
            }
            else if (work.StatusEffectTemplate.Type == StatusEffectTemplate.EffectType.HealthOverTime)
            {
                BookSpellHealing(work.CastId, work.Target, expectedValuePerTarget);
            }

            ExtendSessionCollectTime(work.CastId, work.StatusEffectDuration);
        }

        private static bool CanApplyResolvedSpellDamage(QueuedSpellDamage spellDamage)
        {
            if (!IsLiveMissionAgent(spellDamage.Target, requireHuman: true))
            {
                return false;
            }

            return spellDamage.Caster == null || IsMissionAgentReferenceCurrent(spellDamage.Caster);
        }

        private static bool CanApplyResolvedSpellHealing(QueuedSpellHealing spellHealing)
        {
            if (!IsLiveMissionAgent(spellHealing.Target))
            {
                return false;
            }

            return spellHealing.Healer == null || IsMissionAgentReferenceCurrent(spellHealing.Healer);
        }

        private static bool CanApplyResolvedStatusDotDamage(QueuedStatusDotDamage statusDotDamage)
        {
            if (!IsLiveMissionAgent(statusDotDamage.Target, requireHuman: true))
            {
                return false;
            }

            return statusDotDamage.Applier == null || IsMissionAgentReferenceCurrent(statusDotDamage.Applier);
        }

        private static bool CanApplyResolvedStatusHealing(QueuedStatusHealing statusHealing)
        {
            return IsLiveMissionAgent(statusHealing.Target);
        }

        private void EnqueueSpellDamage(QueuedSpellDamage spellDamage)
        {
            _queuedSpellDamage.Enqueue(spellDamage);
            TrackQueuedSpellDamage(spellDamage.CastId);
            TrackTriggeredEffectPrimaryWork(spellDamage.ResolutionId);

            if (spellDamage.AbilityTemplate != null || !string.IsNullOrWhiteSpace(spellDamage.TriggeredEffectId))
            {
                LogSpellDamageQueueSpike(spellDamage);
            }
        }

        private void EnqueueSpellHealing(QueuedSpellHealing spellHealing)
        {
            _queuedSpellHealing.Enqueue(spellHealing);
            TrackQueuedSpellHealing(spellHealing.CastId);
            TrackTriggeredEffectPrimaryWork(spellHealing.ResolutionId);
        }

        private bool HasQueuedGameplayWork()
        {
            return _queuedTriggeredStatusEffects.Count > 0 ||
                   _queuedSpellDamage.Count > 0 ||
                   _queuedSpellHealing.Count > 0 ||
                   _queuedStatusDotDamage.Count > 0 ||
                   _queuedStatusHealing.Count > 0;
        }

        private void TrackQueuedSpellDamage(int castId)
        {
            if (castId < 0)
            {
                return;
            }

            _queuedSpellDamageCountByCastId.TryGetValue(castId, out var count);
            _queuedSpellDamageCountByCastId[castId] = count + 1;
        }

        private void TrackQueuedSpellHealing(int castId)
        {
            if (castId < 0)
            {
                return;
            }

            _queuedSpellHealingCountByCastId.TryGetValue(castId, out var count);
            _queuedSpellHealingCountByCastId[castId] = count + 1;
        }

        private void CompleteQueuedSpellHealing(int castId)
        {
            if (castId < 0 || !_queuedSpellHealingCountByCastId.TryGetValue(castId, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                _queuedSpellHealingCountByCastId.Remove(castId);
                return;
            }

            _queuedSpellHealingCountByCastId[castId] = count - 1;
        }

        private void TrackQueuedStatusDotDamage(int castId)
        {
            if (castId < 0)
            {
                return;
            }

            _queuedStatusDotDamageCountByCastId.TryGetValue(castId, out var count);
            _queuedStatusDotDamageCountByCastId[castId] = count + 1;
        }

        private void CompleteQueuedStatusDotDamage(int castId)
        {
            if (castId < 0 || !_queuedStatusDotDamageCountByCastId.TryGetValue(castId, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                _queuedStatusDotDamageCountByCastId.Remove(castId);
                return;
            }

            _queuedStatusDotDamageCountByCastId[castId] = count - 1;
        }

        private void TrackTriggeredStatusEffect(int castId)
        {
            if (castId < 0)
            {
                return;
            }

            _queuedTriggeredStatusEffectCountByCastId.TryGetValue(castId, out var count);
            _queuedTriggeredStatusEffectCountByCastId[castId] = count + 1;
        }

        private void CompleteTriggeredStatusEffect(int castId)
        {
            if (castId < 0 || !_queuedTriggeredStatusEffectCountByCastId.TryGetValue(castId, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                _queuedTriggeredStatusEffectCountByCastId.Remove(castId);
                return;
            }

            _queuedTriggeredStatusEffectCountByCastId[castId] = count - 1;
        }

        private void CompleteQueuedSpellDamage(int castId)
        {
            if (castId < 0 || !_queuedSpellDamageCountByCastId.TryGetValue(castId, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                _queuedSpellDamageCountByCastId.Remove(castId);
                return;
            }

            _queuedSpellDamageCountByCastId[castId] = count - 1;
        }

        private void TrackTriggeredEffectPrimaryWork(int resolutionId)
        {
            if (resolutionId < 0)
            {
                return;
            }

            _pendingTriggeredEffectPrimaryWork.TryGetValue(resolutionId, out var count);
            _pendingTriggeredEffectPrimaryWork[resolutionId] = count + 1;
        }

        private void CompleteTriggeredEffectPrimaryWork(int resolutionId)
        {
            if (resolutionId < 0 || !_pendingTriggeredEffectPrimaryWork.TryGetValue(resolutionId, out var count))
            {
                return;
            }

            if (count <= 1)
            {
                _pendingTriggeredEffectPrimaryWork.Remove(resolutionId);

                if (_triggeredStatusEffectsWaitingForPrimaryHit.TryGetValue(resolutionId, out var waitingStatusEffects))
                {
                    foreach (var statusEffect in waitingStatusEffects)
                    {
                        _queuedTriggeredStatusEffects.Enqueue(statusEffect);
                    }

                    _triggeredStatusEffectsWaitingForPrimaryHit.Remove(resolutionId);
                }

                return;
            }

            _pendingTriggeredEffectPrimaryWork[resolutionId] = count - 1;
        }

        private bool IsTriggeredEffectPrimaryWorkPending(int resolutionId)
        {
            return resolutionId >= 0 &&
                   _pendingTriggeredEffectPrimaryWork.TryGetValue(resolutionId, out var count) &&
                   count > 0;
        }

        private bool HasQueuedAbilityCast(int castId)
        {
            if (castId < 0)
            {
                return false;
            }

            return _queuedSpellDamageCountByCastId.TryGetValue(castId, out var spellDamageCount) && spellDamageCount > 0 ||
                   _queuedSpellHealingCountByCastId.TryGetValue(castId, out var spellHealingCount) && spellHealingCount > 0 ||
                   _queuedStatusDotDamageCountByCastId.TryGetValue(castId, out var statusDotDamageCount) && statusDotDamageCount > 0 ||
                   _queuedTriggeredStatusEffectCountByCastId.TryGetValue(castId, out var triggeredStatusCount) && triggeredStatusCount > 0;
        }

        private enum TriggeredEffectQueueItemKind { Sound, Visual, Status }

        private readonly struct QueuedSpellHealing
        {
            public QueuedSpellHealing(Agent target, int healing, Agent healer, AbilityTemplate abilityTemplate, int castId, int resolutionId)
            {
                Target = target;
                Healing = healing;
                Healer = healer;
                AbilityTemplate = abilityTemplate;
                CastId = castId;
                ResolutionId = resolutionId;
            }

            public Agent Target { get; }
            public int Healing { get; }
            public Agent Healer { get; }
            public AbilityTemplate AbilityTemplate { get; }
            public int CastId { get; }
            public int ResolutionId { get; }
        }

        private readonly struct QueuedStatusDotDamage
        {
            public QueuedStatusDotDamage(Agent target, int damage, Vec3 impactPosition, Agent applier, int castId)
            {
                Target = target;
                Damage = damage;
                ImpactPosition = impactPosition;
                Applier = applier;
                CastId = castId;
            }

            public Agent Target { get; }
            public int Damage { get; }
            public Vec3 ImpactPosition { get; }
            public Agent Applier { get; }
            public int CastId { get; }
        }

        private readonly struct QueuedStatusHealing
        {
            public QueuedStatusHealing(Agent target, int healing)
            {
                Target = target;
                Healing = healing;
            }

            public Agent Target { get; }
            public int Healing { get; }
        }

        private readonly struct TriggeredEffectQueueItem
        {
            private TriggeredEffectQueueItem(TriggeredEffectQueueItemKind kind, string effectId, string soundEffectId, string burstParticleEffectPrefab,
                Vec3 position, Vec3 normal, float fadeOutTime, Agent target, string statusEffectId, StatusEffectTemplate statusEffectTemplate, Agent applierAgent, float statusEffectDuration,
                bool appendStatusEffect, bool isMutatedStatusEffect, bool stackStatusEffect, int castId, int resolutionId)
            {
                Kind = kind;
                EffectId = effectId;
                SoundEffectId = soundEffectId;
                BurstParticleEffectPrefab = burstParticleEffectPrefab;
                Position = position;
                Normal = normal;
                FadeOutTime = fadeOutTime;
                Target = target;
                StatusEffectId = statusEffectId;
                StatusEffectTemplate = statusEffectTemplate;
                ApplierAgent = applierAgent;
                StatusEffectDuration = statusEffectDuration;
                AppendStatusEffect = appendStatusEffect;
                IsMutatedStatusEffect = isMutatedStatusEffect;
                StackStatusEffect = stackStatusEffect;
                CastId = castId;
                ResolutionId = resolutionId;
            }

            public TriggeredEffectQueueItemKind Kind { get; }
            public string EffectId { get; }
            public string SoundEffectId { get; }
            public string BurstParticleEffectPrefab { get; }
            public Vec3 Position { get; }
            public Vec3 Normal { get; }
            public float FadeOutTime { get; }
            public Agent Target { get; }
            public string StatusEffectId { get; }
            public StatusEffectTemplate StatusEffectTemplate { get; }
            public Agent ApplierAgent { get; }
            public float StatusEffectDuration { get; }
            public bool AppendStatusEffect { get; }
            public bool IsMutatedStatusEffect { get; }
            public bool StackStatusEffect { get; }
            public int CastId { get; }
            public int ResolutionId { get; }

            public static TriggeredEffectQueueItem ForSound(string effectId, string soundEffectId, Vec3 position, int castId)
            {
                return new TriggeredEffectQueueItem(TriggeredEffectQueueItemKind.Sound, effectId, soundEffectId, null,
                    position, Vec3.Zero, 0f, null, null, null, null, 0f, false, false, false, castId, -1);
            }

            public static TriggeredEffectQueueItem ForVisual(string effectId, string burstParticleEffectPrefab, float fadeOutTime, Vec3 position, Vec3 normal, int castId)
            {
                return new TriggeredEffectQueueItem(
                    TriggeredEffectQueueItemKind.Visual, effectId, null, burstParticleEffectPrefab, position, normal, fadeOutTime,
                    null, null, null, null, 0f, false, false, false, castId, -1);
            }

            public static TriggeredEffectQueueItem ForStatus(Agent target, StatusEffectTemplate statusEffectTemplate, Agent applierAgent,
                float duration, bool append, bool isMutated, bool stackStatusEffect, int castId, int resolutionId)
            {
                return new TriggeredEffectQueueItem(TriggeredEffectQueueItemKind.Status,
                    null, null, null, Vec3.Zero, Vec3.Zero, 0f,
                    target, statusEffectTemplate.StringID, statusEffectTemplate, applierAgent, duration, append, isMutated, stackStatusEffect, castId, resolutionId);
            }

            public static TriggeredEffectQueueItem ForStatus(Agent target, string statusEffectId, Agent applierAgent,
                float duration, bool append, bool isMutated, bool stackStatusEffect, int castId, int resolutionId)
            {
                return new TriggeredEffectQueueItem(TriggeredEffectQueueItemKind.Status, null, null, null, Vec3.Zero, Vec3.Zero, 0f,
                    target, statusEffectId, null, applierAgent, duration, append, isMutated, stackStatusEffect, castId, resolutionId);
            }
        }
        private void LogSpellDamageQueueSpike(QueuedSpellDamage spellDamage)
        {
            if (!ENABLE_LOG_SPELLS ||
                _queuedSpellDamage.Count < SPELL_DAMAGE_QUEUE_LOG_THRESHOLD)
            {
                return;
            }

            var nextLoggedQueueSize = Math.Max(
                SPELL_DAMAGE_QUEUE_LOG_THRESHOLD,
                _largestQueuedSpellDamageCount + SPELL_DAMAGE_QUEUE_LOG_THRESHOLD);

            if (_queuedSpellDamage.Count < nextLoggedQueueSize)
            {
                return;
            }

            _largestQueuedSpellDamageCount = _queuedSpellDamage.Count;

            TORCommon.Log(
                $"spell damage queue spike | queued={_queuedSpellDamage.Count} | castId={spellDamage.CastId} | effect={spellDamage.TriggeredEffectId ?? "unknown_effect"}",
                NLog.LogLevel.Warn);
        }

        private readonly struct QueuedSpellDamage
        {
            public QueuedSpellDamage(Agent target, int damage, Vec3 impactPosition, Agent caster, DamageType damageType, AbilityTemplate abilityTemplate,
                string triggeredEffectId, bool hasShockWave, bool originatesFromAbility, int castId, int resolutionId, bool bookSpellResult)
            {
                Target = target;
                Damage = damage;
                ImpactPosition = impactPosition;
                Caster = caster;
                DamageType = damageType;
                AbilityTemplate = abilityTemplate;
                TriggeredEffectId = triggeredEffectId;
                HasShockWave = hasShockWave;
                OriginatesFromAbility = originatesFromAbility;
                CastId = castId;
                ResolutionId = resolutionId;
                BookSpellResult = bookSpellResult;
            }

            public Agent Target { get; }
            public int Damage { get; }
            public Vec3 ImpactPosition { get; }
            public Agent Caster { get; }
            public DamageType DamageType { get; }
            public AbilityTemplate AbilityTemplate { get; }
            public string TriggeredEffectId { get; }
            public bool HasShockWave { get; }
            public bool OriginatesFromAbility { get; }
            public int CastId { get; }
            public int ResolutionId { get; }
            public bool BookSpellResult { get; }
        }

        public void BookSpellHealing(int castId, Agent target, int healingDone)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.BookHealing(target, healingDone);
                return;
            }

            var pendingSession = _pendingCollectSessions.Find(s => s.CastID == castId);
            pendingSession?.BookHealing(target, healingDone);
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
                return;
            }

            var pendingSession = _pendingCollectSessions.Find(s => s.CastID == castId);
            pendingSession?.BookStatusEffect(target);
        }

        /// <summary>
        /// Extends the session collect time to wait for status effects to expire.
        /// Called from TriggeredEffect when status effects are applied.
        /// </summary>
        public void ExtendSessionCollectTime(int castId, float duration)
        {
            if (_activeSpellSessions.TryGetValue(castId, out var session))
            {
                session.TrackAppliedStatusEffectDuration(duration);
                session.ExtendCollectTime(duration);
                return;
            }

            var pendingSession = _pendingCollectSessions.Find(s => s.CastID == castId);
            if (pendingSession != null)
            {
                pendingSession.TrackAppliedStatusEffectDuration(duration);
                pendingSession.ExtendCollectTime(duration);
            }
        }

        /// <summary>
        /// Called when an ability ends. If status effects are still pending, queues for later collection.
        /// </summary>
        public void CollectSpellSession(int castId)
        {
            if (!_activeSpellSessions.TryGetValue(castId, out var session))
                return;

            session.MarkAbilityEnded();
            _activeSpellSessions.Remove(castId);

            // If not ready to collect (status effects still pending), queue for later
            if (!session.IsReadyToCollect || HasQueuedAbilityCast(castId))
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
                if (session.IsReadyToCollect && !HasQueuedAbilityCast(session.CastID))
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
            _queuedSpellDamage.Clear();
            _queuedSpellDamageCountByCastId.Clear();
            _queuedSpellHealing.Clear();
            _queuedSpellHealingCountByCastId.Clear();
            _queuedStatusDotDamage.Clear();
            _queuedStatusDotDamageCountByCastId.Clear();
            _queuedStatusHealing.Clear();
            _queuedTriggeredStatusEffects.Clear();
            _triggeredStatusEffectsWaitingForPrimaryHit.Clear();
            _queuedTriggeredStatusEffectCountByCastId.Clear();
            _queuedTriggeredCosmetics.Clear();
            _pendingTriggeredEffectPrimaryWork.Clear();

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
        private static void LogSpellSession(SpellCastSession session)
        {
            if (!ENABLE_LOG_SPELLS || session == null)
            {
                return;
            }

            string spellName = session.AbilityTemplate?.Name?.ToString();
            if (string.IsNullOrWhiteSpace(spellName))
            {
                spellName = session.AbilityTemplate?.StringID ?? "unknown_spell";
            }

            string casterName = session.Caster?.Name?.ToString();
            if (string.IsNullOrWhiteSpace(casterName) && session.CasterHero != null)
            {
                casterName = session.CasterHero.Name?.ToString();
            }

            if (string.IsNullOrWhiteSpace(casterName))
            {
                casterName = "unknown_caster";
            }

            TORCommon.Log(
                $"spell session end | castId={session.CastID} | spell={spellName} | caster={casterName} | duration={session.EffectiveDurationSeconds:0.0}s | hasData={session.HasData} | kills={session.AgentsKilledCount} | friendlyKills={session.AgentsFriendlyKilledCount} | damaged={session.AgentsDamagedCount} | healed={session.AgentsHealedCount} | statusTargets={session.AgentsAffectedByStatusEffectsCount} | statusApplications={session.StatusEffectsApplied}",
                NLog.LogLevel.Info);
        }

        /// <summary>
        /// Finalizes a spell session - displays results and grants XP.
        /// </summary>
        private void FinalizeSession(SpellCastSession session)
        {
            session.MarkAbilityEnded();
            LogSpellSession(session);

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
                
                // Grant career ability charge once per session (instead of every tick)
                
                if (session.Caster != null && (session.AbilityTemplate.AbilityType == AbilityType.Spell || session.AbilityTemplate.AbilityType == AbilityType.Prayer))
                {
                    // Apply charge for damage dealt
                    if (session.TotalDamageDealt > 0)
                    {
                        CareerHelper.ApplyCareerAbilityCharge(session.TotalDamageDealt, ChargeType.DamageDone, AttackTypeMask.Spell, session.Caster);
                    }

                    // Apply charge for healing done
                    if (session.TotalHealingDone > 0)
                    {
                        CareerHelper.ApplyCareerAbilityCharge(session.TotalHealingDone, ChargeType.Healed, AttackTypeMask.Spell, session.Caster);
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