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
using NLog;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.HarmonyPatches;

namespace TOR_Core.AbilitySystem
{
    public class AbilityManagerMissionLogic : MissionLogic
    {
        private bool _shouldSheathWeapon;
        private bool _shouldWieldWeapon;
        private bool _hasInitializedForMainAgent;
        private bool _careerAbilitySelected;
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
        private readonly float DamagePortionForChargingCareerAbility = 1f;
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
                foreach (var agent in team.TeamAgents)
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
        
        public override void OnBehaviorInitialize()
        {
            base.OnBehaviorInitialize();
            Mission.OnItemPickUp += OnItemPickup;
        }

        protected override void OnEndMission()
        {
            base.OnEndMission();
            BindWeaponKeys();
            Mission.OnItemPickUp -= OnItemPickup;
        }

        private bool ActionFullFillsBasicCareerChargeRequirements(Agent affectorAgent , Agent affectedAgent)
        {
            if (!Hero.MainHero.HasAnyCareer()) return false;
            if(Agent.Main==null)return false;
            
            if(affectorAgent==null)return false;
            if(affectorAgent.IsMount||affectedAgent.IsMount) return false;
            
            var affectorParty = affectorAgent.GetOriginMobileParty();
            var affectedParty = affectedAgent.GetOriginMobileParty();
            if(!affectorParty.IsMainParty&&!affectedParty.IsMainParty ) return false;
            
            
            return true;
        }

        public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
        {

            if (ActionFullFillsBasicCareerChargeRequirements(affectorAgent, affectedAgent))
            {
                var attackMask = DamagePatch.DetermineMask(blow);
                
                CareerHelper.ApplyCareerAbilityCharge(1,ChargeType.NumberOfKills,attackMask,affectorAgent,affectedAgent);
            }
            
        }

        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon, in Blow blow, in AttackCollisionData attackCollisionData)
        {
            if(!ActionFullFillsBasicCareerChargeRequirements(affectedAgent, affectorAgent))return;
            
            
            
            var comp = Agent.Main.GetComponent<AbilityComponent>();
            var attackMask = DamagePatch.DetermineMask(blow);
            
            CareerHelper.ApplyCareerAbilityCharge(blow.InflictedDamage,ChargeType.DamageDone, attackMask,affectorAgent,affectedAgent, attackCollisionData);
            
            if (affectorAgent.IsHero && affectorAgent.GetHero() == Hero.MainHero)
            {
                return;
            }

            if (affectedAgent.IsHero && affectorAgent.GetHero() == Hero.MainHero)
            {
                CareerHelper.ApplyCareerAbilityCharge(blow.InflictedDamage,ChargeType.DamageTaken,attackMask,affectorAgent,affectedAgent, attackCollisionData);
                return;
            }

            if (affectorAgent.GetOriginMobileParty() != null && affectorAgent.GetOriginMobileParty().IsMainParty)
            {
                if (affectorAgent.IsHero)
                {
                    var choices = Agent.Main.GetHero().GetAllCareerChoices();
                    if (choices.Contains("InspirationOfTheLadyKeystone"))
                    {
                        if (!affectorAgent.IsSpellCaster()) return;
                    
                        AttackTypeMask mask= DamagePatch.DetermineMask(blow);
                        if (mask == AttackTypeMask.Spell)
                        {
                            CareerHelper.ApplyCareerAbilityCharge(blow.InflictedDamage,ChargeType.DamageDone, mask);
                            return;
                        }
                    }
                }
                
                if (affectorAgent.IsUndead())
                {
                    if (comp.CareerAbility.IsActive)
                    {
                        return;
                    }
                    CareerHelper.ApplyCareerAbilityCharge(blow.InflictedDamage,ChargeType.DamageDone,attackMask);
                }
            }
            
            
        }

        public override void OnMissionTick(float dt)
        {
            if (!_hasInitializedForMainAgent)
            {
                if (Agent.Main != null)
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
            if (CurrentState != AbilityModeState.Off)
            {
                var action = Agent.Main.GetCurrentAction(1);
                if (CurrentState == AbilityModeState.Idle && action != _idleAnimation)
                {
                    Agent.Main.SetActionChannel(1, _idleAnimation);
                }
            }
        }

        internal void OnCastComplete(Ability ability, Agent agent)
        {
            if (ability is ItemBoundAbility && ability.Template.AbilityEffectType == AbilityEffectType.ArtilleryPlacement)
            {
                if (_artillerySlots.ContainsKey(agent.Team))
                {
                    _artillerySlots[agent.Team]--;
                }
            }

            if (agent == Agent.Main)
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
                var model = Campaign.Current.Models.GetAbilityModel();
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
            if (agent == Agent.Main)
            {
                if (CurrentState == AbilityModeState.Idle) _currentState = AbilityModeState.Casting;
            }
            
            if (agent.GetHero().HasAnyCareer())
            {
                var playerHero = agent.GetHero();
                var choices = playerHero.GetAllCareerChoices();

                if (choices.Contains("SecretsOFTheGrailPassive3"))
                {
                    if (ability.Template.AbilityType == AbilityType.Prayer)
                    {
                        var choice = TORCareerChoices.GetChoice("SecretsOFTheGrailPassive3");
                        if (choice != null)
                        {
                            float random = MBRandom.RandomFloatRanged(0, 1);
                            if (random < choice.GetPassiveValue())
                            {
                                playerHero.AddWindsOfMagic(15);
                            }
                        }
                                
                    }
                }
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

            if (Input.IsKeyDown(InputKey.Tab))
                return;

            if (Input.IsKeyPressed(_specialMoveKey.KeyboardKey.InputKey) ||
                Input.IsKeyPressed(_specialMoveKey.ControllerKey.InputKey))
            {
                if (_abilityComponent != null && _abilityComponent.CareerAbility != null)
                    if (_currentState == AbilityModeState.Off &&
                        IsCurrentCrossHairCompatible())
                    {
                        if (_abilityComponent.CareerAbility.RequiresSpellTargeting())
                        {
                            ShiftToCareerAbility();
                        }
                        else
                        {
                            _abilityComponent.CareerAbility.TryCast(Agent.Main);
                        }
                    }
                    else if (_currentState == AbilityModeState.Idle)
                    {
                        if (_careerAbilitySelected)
                        {
                            ShiftBackFromCareerAbility();
                        }
                        else
                        {
                            ShiftToCareerAbility();
                        }
                        
                    }
                
            }

            if (Input.IsKeyPressed(_nextAbilitySelection.KeyboardKey.InputKey) || Input.IsKeyPressed(_nextAbilitySelection.ControllerKey.InputKey))
            {
                Agent.Main.SelectNextAbility();
                _careerAbilitySelected = false;
            }


            if (Input.IsKeyPressed(_previousAbilitySelection.KeyboardKey.InputKey) || Input.IsKeyPressed(_previousAbilitySelection.ControllerKey.InputKey))
            {
                Agent.Main.SelectPreviousAbility();
                _careerAbilitySelected = false;
            }
                

            if (Input.IsKeyPressed(_quickCast.KeyboardKey.InputKey) || Input.IsKeyPressed(_quickCast.ControllerKey.InputKey))
            {
                if (_abilityComponent != null && _abilityComponent.CurrentAbility.AbilityEffectType != AbilityEffectType.SeekerMissile)
                    Agent.Main.CastCurrentAbility();
            }


            if (Input.IsKeyPressed(_spellcastingModeKey.KeyboardKey.InputKey) || Input.IsKeyPressed(_spellcastingModeKey.ControllerKey.InputKey))
            {
                if (_abilityComponent.KnownAbilitySystem.Count > 1 || _abilityComponent.CurrentAbility.Template.AbilityTargetType != AbilityTargetType.Self)
                {
                    switch (_currentState)
                    {
                        case AbilityModeState.Off:
                            ShiftBackFromCareerAbility();
                            EnableAbilityMode();
                            break;
                        case AbilityModeState.Idle:
                            ShiftBackFromCareerAbility();
                            DisableAbilityMode(false);
                            break;
                        default:
                            break;
                    }
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
                    if (_careerAbilitySelected)
                    {
                        ShiftBackFromCareerAbility();
                    }
                }

                if (_abilityComponent.CareerAbility != null && _abilityComponent.CareerAbility.IsActive) _abilityComponent.OnInterrupt();
            }
            else if (Input.IsKeyPressed(InputKey.RightMouseButton))
            {
                if (_abilityComponent.CareerAbility != null && _abilityComponent.CareerAbility.IsActive) _abilityComponent.OnInterrupt();
            }
            else if (Input.IsKeyPressed(InputKey.MouseScrollUp) && _currentState != AbilityModeState.Off)
            {
                if(_abilityComponent.KnownAbilitySystem.Count > 1) Agent.Main.SelectNextAbility();
            }
            else if (Input.IsKeyPressed(InputKey.MouseScrollDown) && _currentState != AbilityModeState.Off)
            {
                if (_abilityComponent.KnownAbilitySystem.Count > 1) Agent.Main.SelectPreviousAbility();
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
        
        private void ShiftToCareerAbility()
        {
           if(!(_abilityComponent.CareerAbility.RequiresSpellTargeting() && _abilityComponent.CareerAbility.IsCharged))
               return;
            
            _abilityComponent.SelectAbility(_abilityComponent.CareerAbility);
            EnableAbilityMode();
            _careerAbilitySelected = true;

        }

        private void ShiftBackFromCareerAbility()
        {
            _abilityComponent.SelectAbility(_abilityComponent.GetCurrentAbilityIndex());
            
            _careerAbilitySelected = false;
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
            _spellcastingModeKey = HotKeyManager.GetCategory(nameof(TORGameKeyContext)).GetGameKey("Spellcasting");
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
            if (_psys != null)
            {
                foreach (var psys in _psys)
                {
                    if (psys != null)
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
        
        public override void OnMissionResultReady(MissionResult missionResult)
        {
            if (missionResult.PlayerDefeated || missionResult.PlayerVictory)
            {
                var agents = Mission.Current.Agents;
                foreach (var agent in agents)
                {
                    if (agent.IsMainAgent&&agent.IsActive())
                    {
                        DisableAbilityMode(true);
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
                    if (comp != null)
                    {
                        comp.Dispose();
                    }
                }
            }
        }

        private void OnItemPickup(Agent agent, SpawnedItemEntity item)
        {
            if (agent == Agent.Main) DisableAbilityMode(true);
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
            if(!IsCastingMission()) return;
            var hero = Agent.Main?.GetHero();
            if (hero != null)
            {
                var info = hero.GetExtendedInfo();
                if (info != null)
                {
                    if (hero.GetPerkValue(TORPerks.SpellCraft.Improvision) && info.GetCustomResourceValue("WindsOfMagic") < TORPerks.SpellCraft.Improvision.PrimaryBonus)
                    {
                        info.SetCustomResourceValue("WindsOfMagic", TORPerks.SpellCraft.Improvision.PrimaryBonus);
                    }

                    if (hero.GetPerkValue(TORPerks.SpellCraft.Catalyst))
                    {
                        int magicItemCount = 0;
                        for (int i = 0; i < (int)EquipmentIndex.NumEquipmentSetSlots; i++)
                        {
                            var equipmentElement = hero.BattleEquipment.GetEquipmentFromSlot((EquipmentIndex)i);
                            var equippedItem = equipmentElement.Item;
                            if (equippedItem != null && equippedItem.IsMagicalItem()) magicItemCount++;
                        }

                        if (magicItemCount > 0)
                        {
                            info.AddCustomResource("WindsOfMagic", magicItemCount * TORPerks.SpellCraft.Catalyst.PrimaryBonus);
                        }
                    }
                }

                if (!(Game.Current.GameType is Campaign)) return;
                if (hero.HasAnyCareer() && hero.HasCareerChoice("ArchLectorPassive1")) return;
                Agent.Main.GetComponent<AbilityComponent>().SetIntialPrayerCoolDown();
            }
        }

        public void ActivateSpellcasterMode()
        {
            if (IsAbilityModeAvailableForMainAgent())
            {
                EnableAbilityMode();
            }
        }

        public void DeactivateSpellcasterMode()
        {
            if (IsAbilityModeAvailableForMainAgent())
            {
                DisableAbilityMode(true);
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