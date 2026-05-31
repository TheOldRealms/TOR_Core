using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.PostBattleLoot;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.ServeAsAHireling
{
    public class ServeAsAHirelingCampaignBehavior : CampaignBehaviorBase
    {
        private ServeAsAHirelingActivities _activities;
        private const float MinimumServeDays = 25;
        private const float RatioPartyAgainstEnemyStrength = 3f;

        private float _durationInDays;
        private bool _hirelingEnlisted;
        private Hero _hirelingEnlistingLord;
        private bool _hirelingEnlistingLordIsAttacking => _hirelingEnlistingLord?.PartyBelongedTo?.MapEventSide?.MissionSide == BattleSideEnum.Attacker;
        private bool _hirelingLordIsFightingWithoutPlayer;

        private readonly bool _debugSkipBattles = false;
        private bool _pauseModeToggle;
        private int _manuallyFoughtBattles;

        private bool _startBattle;
        private bool _siegeBattleMissionStarted;

        // Static accessor for Harmony patches to check if battle is being started
        public static bool IsStartingBattle => Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>()?._startBattle ?? false;

        // Tracks when we're in a post-battle transition where AI ticks might cause issues
        private bool _inPostBattleTransition;
        public static bool InPostBattleTransition => Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>()?._inPostBattleTransition ?? false;

        private bool _hirelingWaitMenuShown;
        private bool _suppressMainPartySettlementLeaveHandling;

        private int _deadJoinedEncounterCleanupTicks;
        private MapEvent _joinedHirelingCleanupBattle;
        private MapEvent _lastCountedHirelingBattle;
        private const int STRICT_JOINED_CLEANUP_DELAY_TICKS = 120;

        private float _entryServiceTimeStamp;

        private SkillObject _currentTrainedSkill;
        private int _currentActivityIndex;

        private bool _enlistInquiryDeclined;

        public float DurationInDays => _durationInDays;

        public int ManuallyFoughtBattles => _manuallyFoughtBattles;
        internal static void MarkHirelingWaitMenuShown()
        {
            Campaign.Current.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>()._hirelingWaitMenuShown = true;
        }

        public bool IsEnlisted()
        {
            return _hirelingEnlisted;
        }

        public Hero EnlistingLord => _hirelingEnlistingLord;

        internal static bool TryLeaveSettlementToHirelingMenu(bool continueTime)
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (behavior == null || !behavior._hirelingEnlisted)
            {
                return false;
            }

            behavior.LeaveSettlementAndOpenHirelingMenu(continueTime);
            return true;
        }



        internal static bool ShouldSuppressHirelingBattleInfluence(Hero hero, float value)
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            return behavior != null
                && behavior._hirelingEnlisted
                && !behavior._hirelingLordIsFightingWithoutPlayer
                && hero?.Clan == Clan.PlayerClan
                && value > 0f;
        }

        private void TryRegisterWinningHirelingBattle(MapEvent mapEvent)
        {
            if (!_hirelingEnlisted
                || _hirelingLordIsFightingWithoutPlayer
                || mapEvent == null
                || !mapEvent.IsPlayerMapEvent
                || !mapEvent.HasWinner
                || mapEvent.PlayerSide != mapEvent.WinningSide
                || _lastCountedHirelingBattle == mapEvent)
            {
                return;
            }

            _manuallyFoughtBattles++;
            _lastCountedHirelingBattle = mapEvent;
        }

        private bool HasTrackedWinningFieldHirelingBattle()
        {
            var trackedBattle = _joinedHirelingCleanupBattle;

            return _hirelingEnlisted
                && !_hirelingLordIsFightingWithoutPlayer
                && trackedBattle != null
                && trackedBattle.IsPlayerMapEvent
                && !trackedBattle.IsSiegeAssault
                && trackedBattle.HasWinner
                && trackedBattle.PlayerSide == trackedBattle.WinningSide;
        }
        private bool HasTrackedCapturePhaseHirelingVictory()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;
            var trackedBattle = _joinedHirelingCleanupBattle;

            return _hirelingEnlisted
                && !_hirelingLordIsFightingWithoutPlayer
                && trackedBattle != null
                && !trackedBattle.IsSiegeAssault
                && trackedBattle.HasWinner
                && currentEncounter != null
                && PlayerEncounter.EncounterSettlement == null
                && currentEncounter.EncounterState == PlayerEncounterState.CaptureHeroes
                && currentBattle == trackedBattle;
        }

        private static bool FinalizeTrackedHirelingVictory(ServeAsAHirelingCampaignBehavior behavior)
        {
            behavior.TryRegisterWinningHirelingBattle(behavior._joinedHirelingCleanupBattle ?? PlayerEncounter.Battle);

            ClearCurrentHirelingLoot();

            if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement == null)
            {
                PlayerEncounter.Finish(false);
            }

            var mainParty = MobileParty.MainParty;
            if (mainParty != null)
            {
                mainParty.MapEventSide = null;
                mainParty.BesiegerCamp = null;
                mainParty.CurrentSettlement = null;
            }

            behavior._startBattle = false;
            behavior._inPostBattleTransition = false;
            behavior._joinedHirelingCleanupBattle = null;
            behavior._deadJoinedEncounterCleanupTicks = 0;
            behavior.ActivateAndRefreshHirelingMenu();

            return true;
        }

        internal static bool TryFinalizeTrackedHirelingVictory()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (behavior == null || !behavior.HasTrackedWinningFieldHirelingBattle())
            {
                return false;
            }

            return FinalizeTrackedHirelingVictory(behavior);
        }

        internal static bool TryFinalizeTrackedHirelingVictoryFromCaptureHeroes()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (behavior == null || !behavior.HasTrackedCapturePhaseHirelingVictory())
            {
                return false;
            }

            return FinalizeTrackedHirelingVictory(behavior);
        }
        private bool HasCurrentWinningFieldHirelingBattle()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;

            return _hirelingEnlisted
                && !_hirelingLordIsFightingWithoutPlayer
                && currentEncounter != null
                && currentBattle != null
                && PlayerEncounter.EncounterSettlement == null
                && currentBattle.IsPlayerMapEvent
                && !currentBattle.IsSiegeAssault
                && currentBattle.HasWinner
                && currentBattle.PlayerSide == currentBattle.WinningSide
                && GetEnlistingLordIsInMapEvent(currentBattle);
        }

        internal static bool TryFinalizeCurrentWinningFieldHirelingBattle()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (behavior == null || !behavior.HasCurrentWinningFieldHirelingBattle())
            {
                return false;
            }

            return FinalizeTrackedHirelingVictory(behavior);
        }

        private bool HasTrackedResolvedHirelingSiegeBattle()
        {
            var trackedBattle = _joinedHirelingCleanupBattle;

            return _hirelingEnlisted
                && !_hirelingLordIsFightingWithoutPlayer
                && trackedBattle != null
                && trackedBattle.IsPlayerMapEvent
                && (trackedBattle.IsSiegeAssault || trackedBattle.IsSiegeOutside)
                && trackedBattle.HasWinner;
        }
        internal static bool TryFinalizeTrackedHirelingSiegeBattle()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (behavior == null || !behavior.HasTrackedResolvedHirelingSiegeBattle())
            {
                return false;
            }
            behavior.TryRegisterWinningHirelingBattle(behavior._joinedHirelingCleanupBattle ?? PlayerEncounter.Battle);
            ClearCurrentHirelingLoot();

            if (PlayerEncounter.LocationEncounter != null)
            {
                PlayerEncounter.LocationEncounter = null;
            }

            var mainParty = MobileParty.MainParty;
            var lordParty = behavior._hirelingEnlistingLord?.PartyBelongedTo;
            var lordSettlement = lordParty?.CurrentSettlement;

            var canReturnIntoLordSettlement =
                mainParty != null
                && lordSettlement != null
                && lordSettlement.SiegeEvent == null
                && lordSettlement.Party.MapEvent == null;

            if (canReturnIntoLordSettlement)
            {
                if (PlayerEncounter.Current != null
                    && PlayerEncounter.EncounterSettlement != lordSettlement)
                {
                    PlayerEncounter.Finish(false);
                }

                mainParty.MapEventSide = null;
                mainParty.BesiegerCamp = null;

                behavior._startBattle = false;
                behavior._siegeBattleMissionStarted = false;
                behavior._inPostBattleTransition = false;
                behavior._joinedHirelingCleanupBattle = null;
                behavior._deadJoinedEncounterCleanupTicks = 0;
                behavior._hirelingWaitMenuShown = false;
                behavior._hirelingLordIsFightingWithoutPlayer = false;

                behavior.EnsureHirelingSettlementEncounter(lordSettlement);

                behavior.ActivateAndRefreshHirelingMenu();

                return true;
            }

            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(false);
            }

            if (mainParty != null)
            {
                mainParty.MapEventSide = null;
                mainParty.BesiegerCamp = null;
                mainParty.CurrentSettlement = null;
            }

            behavior._startBattle = false;
            behavior._siegeBattleMissionStarted = false;
            behavior._inPostBattleTransition = false;
            behavior._joinedHirelingCleanupBattle = null;
            behavior._deadJoinedEncounterCleanupTicks = 0;
            behavior._hirelingWaitMenuShown = false;
            behavior._hirelingLordIsFightingWithoutPlayer = false;

            behavior.ActivateAndRefreshHirelingMenu();

            return true;
        }
        internal static bool ShouldSuppressHirelingInfluenceGain(Clan clan, float influenceChange)
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            return behavior != null
                && behavior._hirelingEnlisted
                && clan == Clan.PlayerClan
                && influenceChange > 0f;
        }

        //Sly : I just realized that almost all of these events can be unregistered for the majority of the game, and they're only registered while the player is enlisted, before unregistering them again when enlistment ends.
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Initialize);
            CampaignEvents.TickEvent.AddNonSerializedListener(this, OnTick);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, EnlistingLordPartyEntersSettlement);
            CampaignEvents.OnSettlementLeftEvent.AddNonSerializedListener(this, OnPartyLeavesSettlement);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, ControlPlayerLoot);                //Those events are never executed when the player lose a battle!
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, MapEventEnded);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, MenuOpened);
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, WeeklyRenownGain);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, SkillGain);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, LeaveKingdomEvent);
            //CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.RaidCompletedEvent.AddNonSerializedListener(this, OnRaidCompleted);
            CampaignEvents.OnQuarterDailyPartyTick.AddNonSerializedListener(this, IgnoreHirelingPartyRefresh);
        }

        /// <summary>
        /// Sets the player party to be untargetable by AI parties.
        /// </summary>
        /// <remarks>
        /// <para>Refreshes the MainParty being ignored so that it can't have battles against it initiated; this is set initially in <see cref="EnlistPlayer"/>.</para>
        /// <para>The player party is sometimes targeted by an enemy party while they are hired which leads to the player being the LeaderHero for their map event side despite supposedly being "just a merc hired by the noble". This leads to the player having a conversation with the enemy LeaderHero as well as being able to choose if their side surrenders and having the default encounterAttack menu displayed. There is also a rare occurence where the followedNoble's party is attacked and engaged in a map event while the player's party is attacked and placed in a separate map event.</para>
        /// <para>The MainParty can instead be set to ignored periodically while in service which will prevent the AI parties from considering them a valid attack target and consequently only targetting the followedNoble. This is already used in OnTick to prevent the player party from being attacked while they are avoiding battle, but this can instead be expanded to apply generally while enlisted and at a lower frequency than every game tick.</para>
        /// <para>party.ShouldBeIgnored is used in 2 places :</para>
        /// <br>- MobilePartyAi.GetBestInitiativeBehavior which prevents the AI from targetting the ignored party (wanted behaviour)</br>
        /// <br>- PlayerEncounter.FindNonAttachedNpcPartiesWhoWillJoinEvent which searches among parties around the player encounter location for allies of the player/enemies of the player's enemy; when it checks for !ShouldBeIgnored for parties on the player side, it also checks for !MainParty which means any nearby npc party can join the player encounter regardless of the ShouldBeIgnored state of the MainParty</br>
        ///<para>So, no predictable impact on which parties participate in the battle when the StartBattleAction is used to create a map event that includes the player, and avoids any bugs related to the AI and the player party.
        ///<br>Should have no persisting issue because it's set for limited duration at a time; if it does persist, that's a relatively easy-to-notice issue that indicates that enlistment is incorrectly being ended (which could be possible on 1.2.11 since there have been some reports about parties not recruiting despite the player having since left enlistment).</br>
        ///<br>If the visual for the player party is also fixed at some point, it would avoid the incongruency of the player being attacked when they have no visual on the map and are "just part of the noble's party"</br>
        ///</para>
        ///<para>Tested with : army v army, army v siege camp, siege camp v army, bandits (cultist, outlaws, ungors), army v party, party v army, party v party, and instances where parties had allied armies in proximity.
        ///<br>In all cases, the hireling player choosing to join the battle would include the noble's party, any nearby parties, and any parties attached to an army.</br>
        ///</para>
        /// </remarks>
        private void IgnoreHirelingPartyRefresh(MobileParty mobileParty)
        {
            //Sly : this doesn't work as expected, I still see parties attempting to target the player party
            //the main party does use this tick as it's used to perform healing ticks, so is a party being flagged as "Ignored" not doing what it seems that it should?
            if (MobileParty.MainParty == mobileParty && _hirelingEnlisted)
            {
                //the moment at which this runs can be between 5-7 hours apart, therefore using 8 to cover all possibilties
                //despite being set to ignore, I still see parties targeting me; what does this imply?
                MobileParty.MainParty.IgnoreForHours(8f);
            }
        }

        private void OnRaidCompleted(BattleSideEnum side, RaidEventComponent component)
        {
            if (!_hirelingEnlisted || !component.IsPlayerMapEvent)
                return;

            _hirelingWaitMenuShown = false;

            if (HasPendingNativeHirelingEncounterCleanup())
                return;

            var mainParty = MobileParty.MainParty;
            var partySettlement = mainParty.CurrentSettlement;
            var encounterSettlement = PlayerEncounter.EncounterSettlement;

            if (partySettlement == null && encounterSettlement == null)
                return;

            while (Campaign.Current.CurrentMenuContext != null)
                GameMenu.ExitToLast();

            if (partySettlement != null)
            {
                if (encounterSettlement != null)
                    PlayerEncounter.LeaveSettlement();
                else
                {
                    LeaveSettlementAction.ApplyForParty(mainParty);
                    PartyBase.MainParty.SetVisualAsDirty();
                }
            }

            if (PlayerEncounter.Current != null
                && PlayerEncounter.EncounterSettlement == null
                && PlayerEncounter.Current.EncounterState == PlayerEncounterState.End)
                PlayerEncounter.Finish(false);

            if (PlayerEncounter.LocationEncounter != null)
                PlayerEncounter.LocationEncounter = null;

            GameMenu.ActivateGameMenu("hireling_menu");
        }


        //private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase attackingParty)
        //{
        //    if (_hirelingEnlisted) //lord prisoners are captured before their party is destroyed so party.LeaderHero is always null - this will probably never do anything
        //    {
        //        if (destroyedParty.LeaderHero == _hirelingEnlistingLord || destroyedParty == MobileParty.MainParty)
        //        {
        //            LeaveLordPartyAction();
        //        }
        //    }
        //}

        private void LeaveKingdomEvent(Clan clan, Kingdom kingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail arg4, bool arg5)
        {
            if (clan == Clan.PlayerClan && IsEnlisted())
            {
                LeaveLordPartyAction();
            }
        }

        private void SkillGain()
        {
            if (_hirelingEnlisted)
            {
                if (_currentTrainedSkill == null)
                {
                    var activities = _activities.GetHirelingActivities(Hero.MainHero.GetCareer());
                    if (activities.Count > 0)
                    {
                        if (_currentActivityIndex < 0 || _currentActivityIndex >= activities.Count)
                        {
                            _currentActivityIndex = 0;
                        }
                        _currentTrainedSkill = activities[_currentActivityIndex];
                    }
                }

                if (_currentTrainedSkill != null && Hero.MainHero.HitPoints >= Hero.MainHero.MaxHitPoints * 0.5f)
                {
                    Hero.MainHero.AddSkillXp(_currentTrainedSkill, 25);
                }
            }
        }

        private void WeeklyRenownGain()
        {
            if (Hero.MainHero.IsEnlisted())
            {
                var gain = 5;
                var clanTier = Hero.MainHero.Clan.Tier;
                gain += clanTier;

                Hero.MainHero.Clan.AddRenown(gain);
            }
        }

        private float GetEnlistingLordEventStrengthRatio(MobileParty lordParty)
        {
            BattleSideEnum side = lordParty.Party.Side;
            lordParty.MapEvent.GetStrengthsRelativeToParty(side, out float enlistingLordStrength, out float enemyStrength);

            if (enemyStrength > 0) //always true, the minimum value is 0.1
            {
                return enlistingLordStrength / enemyStrength;
            }

            return 1;
        }

        private bool IsHirelingSettlementEncounterSafe(Settlement settlement)
        {
            return settlement != null
                && !_inPostBattleTransition
                && settlement.SiegeEvent == null
                && settlement.Party.MapEvent == null;
        }

        private void EnsureHirelingSettlementEncounter(Settlement settlement)
        {
            if (settlement == null)
            {
                return;
            }

            if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement != settlement)
            {
                // old encounter junk
                _inPostBattleTransition = false;
                PlayerEncounter.Finish(false);
            }

            if (PlayerEncounter.Current != null
                && PlayerEncounter.EncounterSettlement == settlement
                && PlayerEncounter.LocationEncounter == null)
            {
                // same settlement, but location encounter got cleared by hireling flow
                PlayerEncounter.EnterSettlement();
                return;
            }

            EnterSettlementAction.ApplyForParty(MobileParty.MainParty, settlement);

            if (PlayerEncounter.Current == null || PlayerEncounter.EncounterSettlement != settlement)
            {
                // re open on the right place
                EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
            }
        }
        private void LeaveSettlementAndOpenHirelingMenu(bool continueTime)
        {
            var mainParty = MobileParty.MainParty;
            var currentSettlement = mainParty.CurrentSettlement;
            var currentMenuId = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;

            if (currentMenuId == "town_wait_menus" || currentMenuId == "village_wait_menus")
            {
                GameMenu.ExitToLast();
            }

            if (currentSettlement != null)
            {
                mainParty.Position = currentSettlement.GatePosition;
            }

            _suppressMainPartySettlementLeaveHandling = true;
            try
            {
                if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement != null)
                {
                    if (mainParty.CurrentSettlement != null)
                    {
                        PlayerEncounter.LeaveSettlement();
                    }

                    if (PlayerEncounter.Current != null)
                    {
                        PlayerEncounter.Finish(false);
                    }
                }
                else if (mainParty.CurrentSettlement != null)
                {
                    LeaveSettlementAction.ApplyForParty(mainParty);
                    PartyBase.MainParty.SetVisualAsDirty();
                }

                if (PlayerEncounter.LocationEncounter != null)
                {
                    PlayerEncounter.LocationEncounter = null;
                }
            }
            finally
            {
                _suppressMainPartySettlementLeaveHandling = false;
            }

            _hirelingWaitMenuShown = false;
            GameMenu.ActivateGameMenu("hireling_menu");
            _hirelingWaitMenuShown = true;

            if (continueTime)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
            }
        }
        private void CleanupStaleEncounterBeforeStartingHirelingFieldBattle()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;

            var hasStaleEncounter =
                currentEncounter != null
                && (PlayerEncounter.EncounterSettlement != null
                    || currentBattle == null
                    || currentBattle.HasWinner
                    || currentBattle.State == MapEventState.WaitingRemoval
                    || currentEncounter.EncounterState == PlayerEncounterState.CaptureHeroes
                    || currentEncounter.EncounterState == PlayerEncounterState.End);

            if (!hasStaleEncounter)
            {
                return;
            }

            ClearCurrentHirelingLoot();

            if (PlayerEncounter.LocationEncounter != null)
            {
                PlayerEncounter.LocationEncounter = null;
            }

            PlayerEncounter.Finish(false);

            var mainParty = MobileParty.MainParty;
            if (mainParty != null)
            {
                mainParty.MapEventSide = null;
                mainParty.BesiegerCamp = null;
                mainParty.CurrentSettlement = null;
            }

            _joinedHirelingCleanupBattle = null;
            _deadJoinedEncounterCleanupTicks = 0;
            _inPostBattleTransition = false;
        }
        private bool HasActiveNativeSettlementEncounter()
        {
            return PlayerEncounter.Current != null
                && PlayerEncounter.EncounterSettlement != null;
        }

        private bool TryCleanupUnsafeHirelingSettlementState()
        {
            var lordParty = _hirelingEnlistingLord?.PartyBelongedTo;
            var settlement = lordParty?.CurrentSettlement;
            var mainParty = MobileParty.MainParty;
            var currentMenuId = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;

            if (_startBattle
                || _siegeBattleMissionStarted
                || settlement?.SiegeEvent == null
                || mainParty == null
                || currentMenuId != "hireling_menu")
            {
                return false;
            }

            if (mainParty.CurrentSettlement != settlement)
            {
                return false;
            }

            if (PlayerEncounter.Current != null
                && PlayerEncounter.EncounterSettlement != null
                && PlayerEncounter.EncounterSettlement != settlement)
            {
                return false;
            }

            if (PlayerEncounter.Battle != null)
            {
                return false;
            }

            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(false);
            }

            mainParty.MapEventSide = null;
            mainParty.BesiegerCamp = null;

            if (mainParty.CurrentSettlement == settlement)
            {
                LeaveSettlementAction.ApplyForParty(mainParty);
                PartyBase.MainParty.SetVisualAsDirty();
            }

            _hirelingWaitMenuShown = false;

            return true;
        }

        private MobileParty GetCurrentHirelingBattleParty()
        {
            var lordParty = _hirelingEnlistingLord?.PartyBelongedTo;
            if (lordParty == null)
            {
                return null;
            }

            if (lordParty.MapEvent != null)
            {
                var lordMapEventSide = lordParty.MapEventSide?.MissionSide;
                if (IsJoinableHirelingMapEvent(lordParty.MapEvent, lordMapEventSide))
                {
                    return lordParty;
                }

                return null;
            }

            var armyOwnerParty = lordParty.Army?.ArmyOwner?.PartyBelongedTo;
            if (armyOwnerParty?.MapEvent == null
                || (!armyOwnerParty.MapEvent.IsSiegeAssault && !armyOwnerParty.MapEvent.IsSiegeOutside))
            {
                return null;
            }

            var sameBesiegerCamp = armyOwnerParty.BesiegerCamp != null
                && (lordParty.BesiegerCamp == armyOwnerParty.BesiegerCamp
                    || (lordParty.Army != null && lordParty.Army == armyOwnerParty.Army));

            var sameDefendingSettlement = lordParty.CurrentSettlement != null
                && lordParty.CurrentSettlement == armyOwnerParty.CurrentSettlement
                && lordParty.CurrentSettlement == armyOwnerParty.MapEvent.MapEventSettlement;

            if ((sameBesiegerCamp || sameDefendingSettlement)
                && IsJoinableHirelingMapEvent(armyOwnerParty.MapEvent, armyOwnerParty.MapEventSide?.MissionSide))
            {
                return armyOwnerParty;
            }

            return null;
        }
        private bool HasExternalRaidInterrupter(MapEvent mapEvent)
        {
            var defaultSettlementDefenders = mapEvent.MapEventSettlement.GetInvolvedPartiesForEventType(mapEvent.EventType);
            return mapEvent.DefenderSide.Parties.Any(defenderParty => !defaultSettlementDefenders.Contains(defenderParty.Party));
        }

        internal static bool IsJoinableHirelingMapEvent(MapEvent mapEvent, BattleSideEnum? alliedSide)
        {
            if (mapEvent == null
                || alliedSide == null
                || mapEvent.HasWinner
                || mapEvent.State == MapEventState.WaitingRemoval)
            {
                return false;
            }

            var alliedMapEventSide = mapEvent.GetMapEventSide(alliedSide.Value);
            var enemyMapEventSide = alliedMapEventSide?.OtherSide;
            if (alliedMapEventSide == null || enemyMapEventSide?.LeaderParty == null)
            {
                return false;
            }

            var alliedHasHealthyTroops = alliedMapEventSide.Parties.Any(x => x.Party.NumberOfHealthyMembers > 0);
            var enemyHasHealthyTroops = enemyMapEventSide.Parties.Any(x => x.Party.NumberOfHealthyMembers > 0);

            if (!alliedHasHealthyTroops || !enemyHasHealthyTroops)
            {
                return false;
            }

            return mapEvent.HasTroopsOnBothSides() || mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside;
        }

        internal static bool IsOngoingHirelingMapEvent(MapEvent mapEvent)
        {
            return mapEvent != null
                && !mapEvent.HasWinner
                && mapEvent.State != MapEventState.WaitingRemoval;
        }

        private bool HasJoinableHirelingBattle()
        {
            var enlistingLordParty = _hirelingEnlistingLord?.PartyBelongedTo;
            var playerMapEvent = Hero.MainHero.PartyBelongedTo?.MapEvent;
            var playerMapEventSide = Hero.MainHero.PartyBelongedTo?.MapEventSide?.MissionSide;
            var enlistingLordMapEventSide = enlistingLordParty?.MapEventSide?.MissionSide;

            return IsJoinableHirelingMapEvent(playerMapEvent, playerMapEventSide)
                || IsJoinableHirelingMapEvent(enlistingLordParty?.MapEvent, enlistingLordMapEventSide);
        }
        private bool HasPendingNativeHirelingEncounterCleanup()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;

            return _inPostBattleTransition
                && currentEncounter != null
                && PlayerEncounter.EncounterSettlement == null
                && (currentEncounter.EncounterState == PlayerEncounterState.End
                    || (currentBattle != null && currentBattle.HasWinner));
        }

        internal static bool HasPendingNativeEncounterCleanup()
        {
            return Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>()?.HasPendingNativeHirelingEncounterCleanup() ?? false;
        }
        private bool HasActiveNativeHirelingJoinedBattleEncounter()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;

            return currentEncounter?.IsJoinedBattle == true
                && currentBattle != null
                && !currentBattle.HasWinner
                && PlayerEncounter.EncounterSettlement == null
                && currentEncounter.EncounterState != PlayerEncounterState.End;
        }

        private bool HasAnyNativeFieldEncounter()
        {
            return PlayerEncounter.Current != null
                && PlayerEncounter.EncounterSettlement == null;
        }

        private bool HasDeadNativeFieldEncounter()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;

            return currentEncounter != null
                && PlayerEncounter.EncounterSettlement == null
                && (currentEncounter.EncounterState == PlayerEncounterState.End
                    || (currentBattle != null && currentBattle.HasWinner));
        }

        private bool HasTrackedDeadHirelingResultEncounter()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;

            return _joinedHirelingCleanupBattle != null
                && currentEncounter != null
                && PlayerEncounter.EncounterSettlement == null
                && currentBattle == _joinedHirelingCleanupBattle
                && !currentBattle.IsSiegeAssault
                && currentBattle.HasWinner
                && currentBattle.PlayerSide == currentBattle.WinningSide
                && currentEncounter.EncounterState == PlayerEncounterState.End;
        }

        internal static bool ShouldSuppressTrackedHirelingBattleLoot(MapEvent mapEvent)
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            return behavior != null
                && behavior._hirelingEnlisted
                && !behavior._hirelingLordIsFightingWithoutPlayer
                && behavior._joinedHirelingCleanupBattle != null
                && mapEvent == behavior._joinedHirelingCleanupBattle
                && mapEvent.IsPlayerMapEvent
                && !mapEvent.IsSiegeAssault;
        }

        internal static bool CleanupTrackedDeadHirelingResultEncounter()
        {
            var behavior = Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>();
            if (behavior == null || !behavior.HasTrackedDeadHirelingResultEncounter())
            {
                return false;
            }

            behavior.TryRegisterWinningHirelingBattle(behavior._joinedHirelingCleanupBattle ?? PlayerEncounter.Battle);

            PlayerEncounter.Current?.RosterToReceiveLootItems?.Clear();
            PendingLootedTroopManager.ResetAllPendingState();
            PlayerEncounter.Finish(false);

            var mainParty = MobileParty.MainParty;
            if (mainParty != null)
            {
                mainParty.MapEventSide = null;
                mainParty.BesiegerCamp = null;
                mainParty.CurrentSettlement = null;
            }

            behavior._joinedHirelingCleanupBattle = null;
            behavior._deadJoinedEncounterCleanupTicks = 0;
            behavior._hirelingWaitMenuShown = false;

            return true;
        }
        private void ResetFailedHirelingSiegeJoin(Settlement settlement)
        {
            var mainParty = MobileParty.MainParty;
            if (mainParty == null)
            {
                return;
            }

            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(false);
            }

            if (PlayerEncounter.LocationEncounter != null)
            {
                PlayerEncounter.LocationEncounter = null;
            }

            mainParty.MapEventSide = null;
            mainParty.BesiegerCamp = null;

            if (settlement != null && mainParty.CurrentSettlement == settlement)
            {
                LeaveSettlementAction.ApplyForParty(mainParty);
                PartyBase.MainParty.SetVisualAsDirty();
            }

            mainParty.CurrentSettlement = null;

            _joinedHirelingCleanupBattle = null;
            _deadJoinedEncounterCleanupTicks = 0;
            _startBattle = false;
            _siegeBattleMissionStarted = false;
            _hirelingLordIsFightingWithoutPlayer = false;
            _hirelingWaitMenuShown = false;
        }
        private bool EnsureHirelingSiegeSettlementEncounter(Settlement settlement, BattleSideEnum joinedSide, MapEvent siegeMapEvent)
        {
            if (settlement == null
                || siegeMapEvent == null
                || (!siegeMapEvent.IsSiegeAssault && !siegeMapEvent.IsSiegeOutside)
                || siegeMapEvent.MapEventSettlement != settlement)
            {
                return false;
            }

            var mainParty = MobileParty.MainParty;
            if (mainParty == null)
            {
                return false;
            }

            if (PlayerEncounter.Current != null)
            {
                PlayerEncounter.Finish(false);
            }

            if (PlayerEncounter.LocationEncounter != null)
            {
                PlayerEncounter.LocationEncounter = null;
            }

            mainParty.MapEventSide = null;
            mainParty.BesiegerCamp = null;

            if (joinedSide == BattleSideEnum.Defender)
            {
                if (mainParty.CurrentSettlement != settlement)
                {
                    EnterSettlementAction.ApplyForParty(mainParty, settlement);
                }

                PlayerEncounter.RestartPlayerEncounter(settlement.Party, siegeMapEvent.AttackerSide.LeaderParty, false);
            }
            else
            {
                if (mainParty.CurrentSettlement != null)
                {
                    LeaveSettlementAction.ApplyForParty(mainParty);
                    PartyBase.MainParty.SetVisualAsDirty();
                }

                EncounterManager.StartSettlementEncounter(mainParty, settlement);
            }

            if (PlayerEncounter.Current == null || PlayerEncounter.EncounteredParty == null)
            {
                return false;
            }

            var encounteredBattle = PlayerEncounter.EncounteredBattle;
            if (encounteredBattle == null)
            {
                return false;
            }

            return PlayerEncounter.EncounterSettlement == settlement
                && encounteredBattle == siegeMapEvent
                && (encounteredBattle.IsSiegeAssault || encounteredBattle.IsSiegeOutside)
                && encounteredBattle.MapEventSettlement == settlement
                && encounteredBattle.CanPartyJoinBattle(PartyBase.MainParty, joinedSide);
        }
        private bool TryGetCurrentHirelingSiegeBattle(out Settlement siegeSettlement, out BattleSideEnum joinedSide, out MapEvent siegeMapEvent)
        {
            siegeSettlement = null;
            joinedSide = BattleSideEnum.Attacker;
            siegeMapEvent = null;

            var lordParty = _hirelingEnlistingLord?.PartyBelongedTo;
            if (lordParty == null)
            {
                return false;
            }

            var lordMapEvent = lordParty.MapEvent;
            var lordJoinedSide = lordParty.MapEventSide?.MissionSide;
            if (lordMapEvent != null
                && (lordMapEvent.IsSiegeAssault || lordMapEvent.IsSiegeOutside)
                && lordJoinedSide != null
                && !lordMapEvent.HasWinner
                && lordMapEvent.State != MapEventState.WaitingRemoval
                && lordMapEvent.MapEventSettlement != null
                && lordMapEvent.InvolvedParties.Any(x => x == lordParty.Party)
                && lordMapEvent.CanPartyJoinBattle(PartyBase.MainParty, lordJoinedSide.Value))
            {
                siegeSettlement = lordMapEvent.MapEventSettlement;
                joinedSide = lordJoinedSide.Value;
                siegeMapEvent = lordMapEvent;
                return true;
            }

            var armyOwnerParty = lordParty.Army?.ArmyOwner?.PartyBelongedTo;
            var armyOwnerMapEvent = armyOwnerParty?.MapEvent;
            var armyOwnerJoinedSide = armyOwnerParty?.MapEventSide?.MissionSide;
            if (armyOwnerMapEvent != null
                && (armyOwnerMapEvent.IsSiegeAssault || armyOwnerMapEvent.IsSiegeOutside)
                && armyOwnerJoinedSide != null
                && !armyOwnerMapEvent.HasWinner
                && armyOwnerMapEvent.State != MapEventState.WaitingRemoval
                && armyOwnerMapEvent.MapEventSettlement != null
                && (armyOwnerMapEvent.InvolvedParties.Any(x => x == lordParty.Party)
                    || (lordParty.Army != null && armyOwnerParty != null && lordParty.Army == armyOwnerParty.Army))
                && armyOwnerMapEvent.CanPartyJoinBattle(PartyBase.MainParty, armyOwnerJoinedSide.Value))
            {
                siegeSettlement = armyOwnerMapEvent.MapEventSettlement;
                joinedSide = armyOwnerJoinedSide.Value;
                siegeMapEvent = armyOwnerMapEvent;
                return true;
            }

            var defendingSettlement = lordParty.CurrentSettlement;
            var defendingMapEvent = defendingSettlement?.Party?.MapEvent;
            if (defendingSettlement != null
                && defendingSettlement.IsFortification
                && defendingMapEvent != null
                && (defendingMapEvent.IsSiegeAssault || defendingMapEvent.IsSiegeOutside)
                && !defendingMapEvent.HasWinner
                && defendingMapEvent.State != MapEventState.WaitingRemoval
                && defendingMapEvent.DefenderSide.Parties.Any(x => x.Party == lordParty.Party)
                && defendingMapEvent.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Defender))
            {
                siegeSettlement = defendingSettlement;
                joinedSide = BattleSideEnum.Defender;
                siegeMapEvent = defendingMapEvent;
                return true;
            }

            var attackerParty = armyOwnerParty ?? lordParty;
            var besiegerCamp = lordParty.BesiegerCamp ?? attackerParty.BesiegerCamp;
            var besiegedSettlement = besiegerCamp?.SiegeEvent?.BesiegedSettlement;
            var attackingMapEvent = besiegedSettlement?.Party?.MapEvent;
            if (besiegedSettlement != null
                && besiegedSettlement.IsFortification
                && attackingMapEvent != null
                && (attackingMapEvent.IsSiegeAssault || attackingMapEvent.IsSiegeOutside)
                && !attackingMapEvent.HasWinner
                && attackingMapEvent.State != MapEventState.WaitingRemoval
                && attackingMapEvent.AttackerSide.Parties.Any(x => x.Party == attackerParty.Party)
                && attackingMapEvent.CanPartyJoinBattle(PartyBase.MainParty, BattleSideEnum.Attacker))
            {
                siegeSettlement = besiegedSettlement;
                joinedSide = BattleSideEnum.Attacker;
                siegeMapEvent = attackingMapEvent;
                return true;
            }

            return false;
        }

        internal static bool HasActiveNativeJoinedBattleEncounter()
        {
            return Campaign.Current?.GetCampaignBehavior<ServeAsAHirelingCampaignBehavior>()?.HasActiveNativeHirelingJoinedBattleEncounter() ?? false;
        }
        private bool IsCurrentHirelingBattleJoinable(MobileParty battleParty)
        {
            return battleParty?.MapEventSide != null
                && IsJoinableHirelingMapEvent(battleParty.MapEvent, battleParty.MapEventSide.MissionSide);
        }

        private void MenuOpened(MenuCallbackArgs obj)
        {
            var menuId = obj.MenuContext.GameMenu.StringId;
            if (IsEnlisted() && menuId == "hireling_battle_menu" && !_hirelingLordIsFightingWithoutPlayer)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
            if (IsEnlisted() && (menuId == "town_wait_menus" || menuId == "village_wait_menus"))
            {
                LeaveSettlementAndOpenHirelingMenu(true);
                return;
            }
            if (IsEnlisted() && menuId == "join_siege_event" && !_startBattle)
            {
                var currentEncounter = PlayerEncounter.Current;
                var currentBattle = PlayerEncounter.Battle;

                var shouldSuppressResolvedHirelingSiegeMenu =
                    _joinedHirelingCleanupBattle != null
                    && currentEncounter?.IsJoinedBattle == true
                    && currentBattle != null
                    && currentBattle == _joinedHirelingCleanupBattle
                    && (currentBattle.IsSiegeAssault || currentBattle.IsSiegeOutside)
                    && currentBattle.HasWinner;

                if (shouldSuppressResolvedHirelingSiegeMenu)
                {
                    if (TryFinalizeTrackedHirelingSiegeBattle())
                    {
                        return;
                    }
                }
            }

            // Intercept encounter menu when enlisted to prevent crashes from bad PlayerEncounter state
            if (IsEnlisted() && menuId == "encounter" && !_startBattle)
            {
                if (HasTrackedDeadHirelingResultEncounter())
                {
                    TryRegisterWinningHirelingBattle(_joinedHirelingCleanupBattle ?? PlayerEncounter.Battle);

                    PlayerEncounter.Current?.RosterToReceiveLootItems?.Clear();
                    PendingLootedTroopManager.ResetAllPendingState();
                    PlayerEncounter.Finish(false);

                    var mainParty = MobileParty.MainParty;
                    mainParty.MapEventSide = null;
                    mainParty.BesiegerCamp = null;
                    mainParty.CurrentSettlement = null;

                    _joinedHirelingCleanupBattle = null;
                    _deadJoinedEncounterCleanupTicks = 0;
                    _hirelingWaitMenuShown = false;

                    SwitchToHirelingMenu();
                    return;
                }

                var activeEncounter = PlayerEncounter.Current;
                var activeBattle = PlayerEncounter.Battle;

                var shouldReturnRetreatedDetachedSiegeOutsideToHirelingBattleMenu =
                    activeEncounter?.IsJoinedBattle == true
                    && activeBattle != null
                    && activeBattle == _joinedHirelingCleanupBattle
                    && !activeBattle.HasWinner
                    && activeBattle.State != MapEventState.WaitingRemoval
                    && PlayerEncounter.EncounterSettlement == null
                    && activeBattle.IsSiegeOutside
                    && activeBattle.MapEventSettlement == null
                    && GetEnlistingLordIsInMapEvent(activeBattle);

                if (shouldReturnRetreatedDetachedSiegeOutsideToHirelingBattleMenu)
                {
                    _hirelingWaitMenuShown = true;
                    GameMenu.SwitchToMenu("hireling_battle_menu");
                    return;
                }

                if (HasAnyNativeFieldEncounter() || HasActiveNativeSettlementEncounter())
                {
                    return;
                }

                SwitchToHirelingMenu();
                return;
            }
            if (_startBattle && menuId == "join_encounter" && !_debugSkipBattles)
            {
                PlayerEncounter.JoinBattle(GetCurrentHirelingBattleParty().MapEventSide.MissionSide);
                _joinedHirelingCleanupBattle = PlayerEncounter.Battle ?? GetCurrentHirelingBattleParty()?.MapEvent;
                _deadJoinedEncounterCleanupTicks = 0;
                GameMenu.SwitchToMenu("encounter");
                return;
            }

            if (_startBattle && menuId == "encounter" && !_debugSkipBattles)
            {
                _startBattle = false;
                var trackedEncounterBattle = PlayerEncounter.Battle ?? Hero.MainHero.PartyBelongedTo.MapEvent;
                if (trackedEncounterBattle != null)
                {
                    _joinedHirelingCleanupBattle = trackedEncounterBattle;
                    _deadJoinedEncounterCleanupTicks = 0;
                }

                if (Hero.MainHero.PartyBelongedTo.MapEvent != null)
                {
                    MenuHelper.EncounterAttackConsequence(obj); //Sly : this likely triggers the creation of a player encounter, and therefore if the player retreats from the battle triggered by "Join Battle", they will end up with a native encounter menu (which can consequently lead to their capture and immediate release after that type of battle whereas the lord losing while the player "Avoids Combat" leads to them being dropped on the campaign map.
                }
                else
                {
                    _startBattle = true; //this will cause it to keep re-opening every time a menu is opened, I guess it's to keep re-catching the encounter menu if the player opts to fast forward while the hireling battle menu is opened?
                }
            }
            if (_debugSkipBattles && _hirelingEnlistingLordIsAttacking)
            {
                _startBattle = false;
            }
        }

        private void LeaveEnlistingParty(string menuToReturn, bool desertion = false)
        {
            if (!desertion)
            {
                desertion = _durationInDays < MinimumServeDays;
            }

            if (desertion)
            {
                var damage = TORTextHelper.GetTextObject("tor_hireling_desert_warning_text", "This will harm your relations with the entire faction.");
                GameTexts.SetVariable("HIRELING_DESERT_TEXT", damage);
            }
            else
            {
                GameTexts.SetVariable("HIRELING_DESERT_TEXT", "");
            }

            var titleText = TORTextHelper.GetTextObject("tor_hireling_abandon_party_title", "Abandon Party");
            var text = TORTextHelper.GetTextObject("tor_hireling_abandon_party_text", "Are you sure you want to abandon the party? {HIRELING_DESERT_TEXT}");
            var affirmativeText = TORTextHelper.GetTextObject("tor_hireling_yes_text", "Yes");
            var negativeText = TORTextHelper.GetTextObject("tor_hireling_no_text", "No");
            InformationManager.ShowInquiry(new InquiryData(titleText.ToString(), text.ToString(), true, true, affirmativeText.ToString(), negativeText.ToString(), delegate ()
            {
                if (desertion)
                {
                    ChangeCrimeRatingAction.Apply(_hirelingEnlistingLord.MapFaction, 55f);
                    foreach (Clan clan in _hirelingEnlistingLord.Clan.Kingdom.Clans)
                    {
                        if (!clan.IsUnderMercenaryService)
                        {
                            ChangeRelationAction.ApplyPlayerRelation(clan.Leader, -10);
                        }
                    }
                }
                GameMenu.ExitToLast();
                LeaveLordPartyAction();
            }, delegate
            {
                GameMenu.ActivateGameMenu(menuToReturn);
            }));
        }

        private void InitializeDialogs(CampaignGameStarter campaignGameStarter)
        {
            var quitText = new TextObject("{HIRELING_QUIT_TEXT}");
            var explainText = new TextObject("{HIRELING_EXPLAIN_TEXT}");
            var positiveDecisionText = new TextObject("{HIRELING_DECISION_TEXT}");

            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword_quit_sure", TORTextHelper.GetText("tor_hireling_quit_service_text", "I would like to quit my service."), QuitCondition, null);
            campaignGameStarter.AddDialogLine("payedsword_quit_sure", "payedsword_quit_sure", "payedsword_quit_choice", TORTextHelper.GetText("tor_hireling_are_you_sure_text", "Are you sure?"), null, null);
            campaignGameStarter.AddPlayerLine("payedsword_quit_choice", "payedsword_quit_choice", "payedsword_quit", TORTextHelper.GetText("tor_hireling_yes_leave_text", "Yes i want to leave"), null, null);
            campaignGameStarter.AddPlayerLine("payedsword_quit_choice", "payedsword_quit_choice", "lord_pretalk", TORTextHelper.GetText("tor_hireling_think_about_it_text", "I have to think about this."), null, null);
            campaignGameStarter.AddDialogLine("payedsword_quit", "payedsword_quit", "end", quitText.Value, null, LeaveLordPartyAction);

            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword_explain", TORTextHelper.GetText("tor_hireling_offer_sword_text", "I am hereby offering my sword."), () => SanityCheck() && !IsEnlisted() && ServeAsAHirelingHelpers.HirelingServiceConditions(), null);
            campaignGameStarter.AddDialogLine("payedsword_explain", "payedsword_explain", "hireling_decide_player", explainText.Value, null, null, 200);
            campaignGameStarter.AddPlayerLine("hireling_decide_player", "hireling_decide_player", "hireling_prompt", TORTextHelper.GetText("tor_hireling_accept_text", "I accept my Lord."), ServeAsAHirelingHelpers.HirelingServiceConditions, () => DisplayPrompt(EnlistPlayer));
            campaignGameStarter.AddPlayerLine("hireling_decide_player", "hireling_decide_player", "lord_pretalk", TORTextHelper.GetText("tor_hireling_think_about_it_text", "I have to think about this."), null, null);
            campaignGameStarter.AddDialogLine("hireling_prompt", "hireling_prompt", "hireling_decision", TORTextHelper.GetText("tor_hireling_ellipsis_text", "..."), null, null);
            campaignGameStarter.AddPlayerLine("hireling_decision", "hireling_decision", "lord_pretalk", TORTextHelper.GetText("tor_hireling_think_about_it_text", "I have to think about this."), () => _enlistInquiryDeclined, null);
            campaignGameStarter.AddDialogLine("hireling_decision", "hireling_decision", "end", positiveDecisionText.Value, null, null);
        }

        private bool SanityCheck()
        {
            var dialogPartner = Campaign.Current.ConversationManager.OneToOneConversationHero;
            return Clan.PlayerClan.MapFaction == Clan.PlayerClan &&
                MobileParty.MainParty.Army == null && dialogPartner.IsPartyLeader; //for the rare case where a noble is in a settlement but hasn't yet formed a new party, eg. released from being a prisoner
        }

        private bool QuitCondition()
        {
            if (Campaign.Current.ConversationManager.OneToOneConversationHero != _hirelingEnlistingLord)
                return false;
            var culture = Campaign.Current.ConversationManager.OneToOneConversationCharacter.Culture.StringId;
            if (GameTexts.TryGetText("tor_hirelinglordquit", out var text, culture))
            {
                GameTexts.SetVariable("HIRELING_QUIT_TEXT", text.Value);
            }
            else
            {
                if (GameTexts.TryGetText("tor_hirelinglordquit", out var defaultText))
                {
                    GameTexts.SetVariable("HIRELING_QUIT_TEXT", defaultText.Value);
                }
            }
            return IsEnlisted() && _durationInDays > MinimumServeDays;
        }

        private void DisplayPrompt(Action enlistPlayer)
        {
            var title = GameTexts.FindText("tor_hireling", "prompttitle");
            var explanation = GameTexts.FindText("tor_hireling", "prompttext");
            _enlistInquiryDeclined = false;
            var inquiry = new InquiryData(title.ToString(),
                explanation.ToString(),
                true,
                true,
                TORTextHelper.GetText("tor_inquiry_accept_text", "Accept"), TORTextHelper.GetText("tor_inquiry_decline_text", "Decline"),
                enlistPlayer,
                () => _enlistInquiryDeclined = true);
            InformationManager.ShowInquiry(inquiry);
        }

        private void ActivateHirelingMenu()
        {
            _hirelingWaitMenuShown = false;
            GameMenu.ActivateGameMenu("hireling_menu");
            _hirelingWaitMenuShown = true;
        }

        private void ActivateAndRefreshHirelingMenu()
        {
            ActivateHirelingMenu();
            SetActivities();
            Campaign.Current.CurrentMenuContext?.Refresh();
        }

        private void SwitchToHirelingMenu()
        {
            GameMenu.SwitchToMenu("hireling_menu");
            _hirelingWaitMenuShown = true;
        }

        private void SetupHirelingMenu(CampaignGameStarter campaignGameStarter)
        {
            var infotext = new TextObject("{ENLISTING_TEXT}");

            campaignGameStarter.AddGameMenuOption("town", "town_back_to_hireling", TORTextHelper.GetText("tor_hireling_back_text", "Back"), args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return IsEnlisted();
            }, args => LeaveSettlementAndOpenHirelingMenu(false), true);

            campaignGameStarter.AddGameMenuOption("castle", "castle_back_to_hireling", TORTextHelper.GetText("tor_hireling_back_text", "Back"), args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return IsEnlisted();
            }, args => LeaveSettlementAndOpenHirelingMenu(false), true);

            campaignGameStarter.AddGameMenuOption("village", "village_back_to_hireling", TORTextHelper.GetText("tor_hireling_back_text", "Back"), args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return IsEnlisted();
            }, args => LeaveSettlementAndOpenHirelingMenu(false), true);

            campaignGameStarter.AddWaitGameMenu(
                "hireling_menu",
                infotext.Value,
                party_wait_talk_to_other_members_on_init,
                wait_on_condition,
                null,
                wait_on_tick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            var enterSettlementText = TORTextHelper.GetTextObject("tor_hireling_enter_settlement_text", "Enter the settlement");
            campaignGameStarter.AddGameMenuOption("hireling_menu", "enter_town", enterSettlementText.ToString(), args =>
            {
                if (!IsEnlisted())
                {
                    return false;
                }

                args.optionLeaveType = GameMenuOption.LeaveType.Continue;
                return _hirelingEnlistingLord?.PartyBelongedTo?.CurrentSettlement != null;
            }, args =>
            {
                var settlement = _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement;

                if (settlement.SiegeEvent != null)
                {
                    if (TryGetCurrentHirelingSiegeBattle(out _, out _, out _))
                    {
                        _hirelingWaitMenuShown = true;
                        GameMenu.ActivateGameMenu("hireling_battle_menu");
                        return;
                    }

                    ResetFailedHirelingSiegeJoin(settlement);
                    ActivateHirelingMenu();
                    return;
                }

                EnsureHirelingSettlementEncounter(settlement);

                if (settlement.IsTown)
                {
                    GameMenu.SwitchToMenu("town");
                }
                else if (settlement.IsVillage)
                {
                    GameMenu.SwitchToMenu("village");
                }
                else if (settlement.IsCastle)
                {
                    GameMenu.SwitchToMenu("castle");
                }
            }, true);

            var pauseText = new TextObject("{PAUSE_ONOFF_TEXT}");
            campaignGameStarter.AddGameMenuOption("hireling_menu", "pause_time_option", pauseText.Value, null, PauseModeToggle);

            var pauseStateText = GameTexts.FindText("tor_hireling", "pausetime");
            pauseStateText.SetTextVariable("PAUSE_ONOFF", "off");
            GameTexts.SetVariable("PAUSE_ONOFF_TEXT", pauseStateText);

            var lordTalkText = GameTexts.FindText("tor_hireling", "talktolord");
            campaignGameStarter.AddGameMenuOption("hireling_menu", "talk_to_lord_option", lordTalkText.Value, null, args => StartDialog());

            campaignGameStarter.AddGameMenuOption("hireling_menu", "empty", "", args =>
            {
                args.IsEnabled = false;
                return true;
            }, null);

            var activity0 = new TextObject("{HIRELINGACTIVITYTEXT0}");
            var activity1 = new TextObject("{HIRELINGACTIVITYTEXT1}");
            var activity2 = new TextObject("{HIRELINGACTIVITYTEXT2}");
            var activity3 = new TextObject("{HIRELINGACTIVITYTEXT3}");
            var activity4 = new TextObject("{HIRELINGACTIVITYTEXT4}");

            campaignGameStarter.AddGameMenuOption("hireling_menu", "activity0_option", activity0.Value, args => HoverActiviy(0, args), args => ToggleActivity(0, args));
            campaignGameStarter.AddGameMenuOption("hireling_menu", "activity1_option", activity1.Value, args => HoverActiviy(1, args), args => ToggleActivity(1, args));
            campaignGameStarter.AddGameMenuOption("hireling_menu", "activity2_option", activity2.Value, args => HoverActiviy(2, args), args => ToggleActivity(2, args));
            campaignGameStarter.AddGameMenuOption("hireling_menu", "activity3_option", activity3.Value, args => HoverActiviy(3, args), args => ToggleActivity(3, args));
            campaignGameStarter.AddGameMenuOption("hireling_menu", "activity4_option", activity4.Value, args => HoverActiviy(4, args), args => ToggleActivity(4, args));

            campaignGameStarter.AddGameMenuOption("hireling_menu", "empty_2", "", args =>
            {
                args.IsEnabled = false;
                return true;
            }, null);

            campaignGameStarter.AddGameMenuOption("hireling_menu", "party_wait_leave", TORTextHelper.GetText("tor_hireling_desert_text", "Desert"), args =>
            {
                var infoText = TORTextHelper.GetTextObject("tor_hireling_desert_warning", "This will damage your reputation with the {FACTION}. Serve for {MINIMUMSERVEDAYS} days, and speak to your enlisting Lord to avoid consequences.");
                var factionName = _hirelingEnlistingLord != null
                    ? _hirelingEnlistingLord.MapFaction.Name.ToString()
                    : "DATA CORRUPTION ERROR";

                infoText.SetTextVariable("FACTION", factionName);
                infoText.SetTextVariable("MINIMUMSERVEDAYS", MinimumServeDays);

                args.Tooltip = infoText;
                args.optionLeaveType = GameMenuOption.LeaveType.Escape;
                return true;
            }, args =>
            {
                LeaveEnlistingParty("hireling_menu");
            }, true);
        }
        private void StartDialog()
        {
            ConversationCharacterData characterData = new(_hirelingEnlistingLord.CharacterObject, _hirelingEnlistingLord.PartyBelongedTo.Party);
            ConversationCharacterData playerData = new(Hero.MainHero.CharacterObject, Hero.MainHero.PartyBelongedTo.Party);
            Campaign.Current.CurrentConversationContext = ConversationContext.Default;
            Campaign.Current.ConversationManager.OpenMapConversation(playerData, characterData);
        }

        private void SetActivities()
        {
            var career = Hero.MainHero.GetCareer();
            for (var i = 0; i < 5; i++)
            {
                if (GameTexts.TryGetText("tor_hirelingactivity" + i, out var text, career.StringId))
                {
                    if (_currentActivityIndex == i)
                    {
                        text = new TextObject($"[{text.Value}]");
                    }
                    GameTexts.SetVariable("HIRELINGACTIVITYTEXT" + i, text);
                }
            }
        }

        private bool HoverActiviy(int i, MenuCallbackArgs args)
        {
            var career = Hero.MainHero.GetCareer();
            var activities = _activities.GetHirelingActivities(career);

            args.Tooltip = activities[i].Name;

            return true;
        }


        private void ToggleActivity(int i, MenuCallbackArgs args)
        {

            var career = Hero.MainHero.GetCareer();
            _currentActivityIndex = i;
            SetActivities();

            var activities = _activities.GetHirelingActivities(career);

            args.Tooltip = activities[i].Name;

            _currentTrainedSkill = activities[i];
            args.MenuContext.Refresh();
            var currentHirelingBattleParty = GetCurrentHirelingBattleParty();
            if (TryGetCurrentHirelingSiegeBattle(out _, out _, out _)
                || IsCurrentHirelingBattleJoinable(currentHirelingBattleParty))
            {
                _hirelingWaitMenuShown = true;
                GameMenu.ActivateGameMenu("hireling_battle_menu");
            }
        }

        private void Initialize(CampaignGameStarter campaignGameStarter)
        {
            _activities = new ServeAsAHirelingActivities();

            InitializeDialogs(campaignGameStarter);
            SetupHirelingMenu(campaignGameStarter);
            SetupBattleMenu(campaignGameStarter);
        }
        private bool TryOpenJoinedHirelingSiegeEncounter(Settlement siegeSettlement, BattleSideEnum siegeJoinedSide, MapEvent siegeMapEvent, MenuCallbackArgs args)
        {
            if (!EnsureHirelingSiegeSettlementEncounter(siegeSettlement, siegeJoinedSide, siegeMapEvent))
            {
                ResetFailedHirelingSiegeJoin(siegeSettlement);

                ActivateHirelingMenu();
                Campaign.Current.CurrentMenuContext?.Refresh();

                return false;
            }

            var encounteredBattle = PlayerEncounter.EncounteredBattle;
            if (encounteredBattle == null
                || encounteredBattle != siegeMapEvent
                || (!encounteredBattle.IsSiegeAssault && !encounteredBattle.IsSiegeOutside)
                || encounteredBattle.MapEventSettlement != siegeSettlement
                || !encounteredBattle.CanPartyJoinBattle(PartyBase.MainParty, siegeJoinedSide))
            {
                ResetFailedHirelingSiegeJoin(siegeSettlement);

                GameMenu.ActivateGameMenu("hireling_menu");
                _hirelingWaitMenuShown = true;
                Campaign.Current.CurrentMenuContext?.Refresh();

                return false;
            }

            _joinedHirelingCleanupBattle = null;
            _deadJoinedEncounterCleanupTicks = 0;
            _startBattle = false;
            _siegeBattleMissionStarted = false;
            _hirelingLordIsFightingWithoutPlayer = false;
            _hirelingWaitMenuShown = false;

            PlayerEncounter.JoinBattle(siegeJoinedSide);
            var currentBattle = PlayerEncounter.Battle ?? encounteredBattle;

            if (currentBattle == null
                || currentBattle != siegeMapEvent
                || (!currentBattle.IsSiegeAssault && !currentBattle.IsSiegeOutside)
                || currentBattle.MapEventSettlement != siegeSettlement)
            {
                ResetFailedHirelingSiegeJoin(siegeSettlement);

                GameMenu.ActivateGameMenu("hireling_menu");
                _hirelingWaitMenuShown = true;
                Campaign.Current.CurrentMenuContext?.Refresh();

                return false;
            }

            _joinedHirelingCleanupBattle = currentBattle;
            _deadJoinedEncounterCleanupTicks = 0;
            _startBattle = false;
            _siegeBattleMissionStarted = false;
            _hirelingLordIsFightingWithoutPlayer = false;

            MenuHelper.EncounterAttackConsequence(args);
            return true;
        }

        private void SetupBattleMenu(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddWaitGameMenu(
                "hireling_battle_menu",
                "{BATTLE_INFO}",
                hireling_battle_menu_on_init,
                hireling_battle_menu_wait_on_condition,
                null,
                hireling_battle_menu_wait_on_tick,
                GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_join_battle", TORTextHelper.GetText("tor_hireling_join_battle_text", "Join battle"),
                hireling_battle_menu_join_battle_on_condition,
                delegate (MenuCallbackArgs args)
                {
                    var currentEncounter = PlayerEncounter.Current;
                    var currentBattle = PlayerEncounter.Battle;

                    var shouldReopenTrackedDetachedSiegeOutsideBattle =
                        currentEncounter?.IsJoinedBattle == true
                        && currentBattle != null
                        && currentBattle == _joinedHirelingCleanupBattle
                        && !currentBattle.HasWinner
                        && currentBattle.State != MapEventState.WaitingRemoval
                        && PlayerEncounter.EncounterSettlement == null
                        && currentBattle.IsSiegeOutside
                        && currentBattle.MapEventSettlement == null
                        && GetEnlistingLordIsInMapEvent(currentBattle);

                    if (shouldReopenTrackedDetachedSiegeOutsideBattle)
                    {
                        _hirelingLordIsFightingWithoutPlayer = false;
                        _startBattle = false;
                        _deadJoinedEncounterCleanupTicks = 0;

                        while (Campaign.Current.CurrentMenuContext != null)
                        {
                            GameMenu.ExitToLast();
                        }

                        MenuHelper.EncounterAttackConsequence(args);
                        return;
                    }

                    if (TryGetCurrentHirelingSiegeBattle(out var siegeSettlement, out var siegeJoinedSide, out var siegeMapEvent))
                    {
                        _hirelingLordIsFightingWithoutPlayer = false;
                        _startBattle = false;

                        while (Campaign.Current.CurrentMenuContext != null)
                        {
                            GameMenu.ExitToLast();
                        }

                        TryOpenJoinedHirelingSiegeEncounter(siegeSettlement, siegeJoinedSide, siegeMapEvent, args);
                        return;
                    }

                    var eventAlliedLeaderParty = GetCurrentHirelingBattleParty();
                    if (eventAlliedLeaderParty == null)
                    {
                        ActivateAndRefreshHirelingMenu();
                        return;
                    }

                    var mapEvent = eventAlliedLeaderParty.MapEvent;
                    var enemyLeaderBase = eventAlliedLeaderParty.MapEventSide.OtherSide.LeaderParty;
                    if (enemyLeaderBase == null)
                    {
                        TORCommon.Log("hireling_join_battle consequence : enemyLeaderBase is null", NLog.LogLevel.Error);
                        ActivateAndRefreshHirelingMenu();
                        return;
                    }

                    // join owns the flow again before native encounter menus can open
                    _hirelingLordIsFightingWithoutPlayer = false;
                    _startBattle = false;

                    while (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }

                    var playerParty = MobileParty.MainParty;
                    if ((mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside)
                        && mapEvent.MapEventSettlement != null)
                    {
                        var joinedSide = eventAlliedLeaderParty.MapEventSide.MissionSide;
                        var siegeEncounterSettlement = mapEvent.MapEventSettlement;

                        TryOpenJoinedHirelingSiegeEncounter(siegeEncounterSettlement, joinedSide, mapEvent, args);
                        return;
                    }

                    CleanupStaleEncounterBeforeStartingHirelingFieldBattle();

                    playerParty.MapEventSide = null;
                    playerParty.BesiegerCamp = null;
                    playerParty.CurrentSettlement = null;

                    _startBattle = true;
                    EncounterManager.StartPartyEncounter(PartyBase.MainParty, enemyLeaderBase);
                }
                , false, 4);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_avoid_combat", TORTextHelper.GetText("tor_hireling_avoid_combat_text", "Avoid Combat"),
                hireling_battle_menu_avoid_combat_on_condition,
                delegate (MenuCallbackArgs args)
                {
                    var currentBattle = PlayerEncounter.Battle;

                    var shouldDetachTrackedDetachedSiegeOutsideBattle =
                        PlayerEncounter.Current?.IsJoinedBattle == true
                        && currentBattle != null
                        && currentBattle == _joinedHirelingCleanupBattle
                        && !currentBattle.HasWinner
                        && currentBattle.State != MapEventState.WaitingRemoval
                        && PlayerEncounter.EncounterSettlement == null
                        && currentBattle.IsSiegeOutside
                        && currentBattle.MapEventSettlement == null
                        && GetEnlistingLordIsInMapEvent(currentBattle);

                    _hirelingLordIsFightingWithoutPlayer = true;
                    _startBattle = false;
                    _deadJoinedEncounterCleanupTicks = 0;

                    if (shouldDetachTrackedDetachedSiegeOutsideBattle)
                    {
                        ClearCurrentHirelingLoot();
                        PlayerEncounter.Finish(false);
                    }

                    _joinedHirelingCleanupBattle = null;

                    var playerParty = MobileParty.MainParty;
                    playerParty.MapEventSide = null;
                    playerParty.BesiegerCamp = null;
                    playerParty.CurrentSettlement = null;

                    var currentMenuContext = Campaign.Current.CurrentMenuContext;
                    if (currentMenuContext?.GameMenu?.StringId != "hireling_battle_menu")
                    {
                        GameMenu.ActivateGameMenu("hireling_battle_menu");
                        currentMenuContext = Campaign.Current.CurrentMenuContext;
                    }

                    _hirelingWaitMenuShown = true;

                    currentMenuContext?.GameMenu?.StartWait();
                    currentMenuContext?.Refresh();
                },
                false,
                4);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_flee", TORTextHelper.GetText("tor_hireling_flee_text", "Flee"),
               hireling_battle_menu_desert_on_condition,
               delegate
               {
                   LeaveEnlistingParty("hireling_battle_menu", true);
               }
               , false, 4);
        }

        private void PauseModeToggle(MenuCallbackArgs args)
        {
            _pauseModeToggle = !_pauseModeToggle;

            var onOffText = TORTextHelper.GetText("tor_toggle_off_text", "Off");
            if (_pauseModeToggle)
            {
                onOffText = TORTextHelper.GetText("tor_toggle_on_text", "On");
            }

            TextObject text2 = GameTexts.FindText("tor_hireling", "pausetime");
            text2.SetTextVariable("PAUSE_ONOFF", onOffText);

            GameTexts.SetVariable("PAUSE_ONOFF_TEXT", text2);
            args.Text = text2;
            args.MenuContext.Refresh();
        }

        public void LeaveLordPartyAction()
        {
            _startBattle = false;
            _siegeBattleMissionStarted = false;
            _inPostBattleTransition = false;
            _hirelingLordIsFightingWithoutPlayer = false;
            _hirelingWaitMenuShown = false;
            _joinedHirelingCleanupBattle = null;
            _deadJoinedEncounterCleanupTicks = 0;
            _lastCountedHirelingBattle = null;
            Game.Current.AfterTick -= InitializeSiegeBattle;
            _hirelingEnlisted = false;
            _hirelingEnlistingLord = null;
            Hero.MainHero.RemoveAttribute("enlisted");
            _hirelingWaitMenuShown = false;
            //I am not sure why this was needed? Putting it in makes it crash if you leave service while in a town for example.
            //This makes PlayerEncounter.EncounterSettlement null which is accessed via vanilla gamemenu init methods
            //crash does not occur, and finishing encounter is important to not end in a invalid state, where parties try to engange with player but can't
            ClearCurrentHirelingLoot();
            PlayerEncounter.Finish();
            if (Settlement.CurrentSettlement != null)
            {
                if (PlayerEncounter.EncounterSettlement != null) PlayerEncounter.LeaveSettlement();
                if (MobileParty.MainParty.CurrentSettlement != null) LeaveSettlementAction.ApplyForParty(MobileParty.MainParty);
            }
            UndoDiplomacy();
            ShowPlayerParty();

            _durationInDays = 0;
            _manuallyFoughtBattles = 0;
            _currentTrainedSkill = null;
            _currentActivityIndex = 0;
        }

        private void InitializeSiegeBattle(float tick)
        {
            if (!_hirelingEnlisted) return;
            if (!_siegeBattleMissionStarted) return;
            if (MobileParty.MainParty == null) return;
            var mainPartyMapEvent = MobileParty.MainParty.MapEvent;
            //Sly : this will never go past this conditional afaict, StringId is always null - this is likely now deprecated because MainParty.MapEvent is set prior to this when MainParty.MapEventSide is set to the same side as the leading party for their enlisting lord
            if (mainPartyMapEvent == null || mainPartyMapEvent.StringId == null) return; //wait until the main party event is assigned correctly

            StartBattleAction.Apply(PartyBase.MainParty, mainPartyMapEvent.DefenderSide.LeaderParty);
            _siegeBattleMissionStarted = false;
            Game.Current.AfterTick -= InitializeSiegeBattle;    //cleanup,  method is afterwards rendered harmless and will not affect performance 
        }

        private bool hireling_battle_menu_join_battle_on_condition(MenuCallbackArgs args)
        {
            if (Hero.MainHero.IsWounded)
            {
                return false;
            }

            if (TryGetCurrentHirelingSiegeBattle(out _, out _, out _))
            {
                return true;
            }

            return IsCurrentHirelingBattleJoinable(GetCurrentHirelingBattleParty());
        }

        private bool hireling_battle_menu_desert_on_condition(MenuCallbackArgs args)
        {
            return _hirelingEnlistingLord.CurrentSettlement == null;
        }

        private bool hireling_battle_menu_avoid_combat_on_condition(MenuCallbackArgs args)
        {
            if (_hirelingEnlistingLord == null)
            {
                return false;
            }

            if (TryGetCurrentHirelingSiegeBattle(out _, out _, out _))
            {
                return Hero.MainHero.IsWounded;
            }

            var lordParty = GetCurrentHirelingBattleParty();
            if (lordParty?.MapEvent == null || !IsCurrentHirelingBattleJoinable(lordParty))
            {
                return false;
            }

            if (Hero.MainHero.IsWounded)
            {
                return true;
            }

            var partyStrength = GetEnlistingLordEventStrengthRatio(lordParty); // (ally strength / enemy) => allies stronger when value > 1
            var combatStrengthThreshold = partyStrength > RatioPartyAgainstEnemyStrength; // allies' strength at least 3x greater than enemy to simulate

            return combatStrengthThreshold;
        }

        private bool wait_on_condition(MenuCallbackArgs args)
        {
            return true;
        }
        private bool hireling_battle_menu_wait_on_condition(MenuCallbackArgs args)
        {
            return true;
        }

        private void hireling_battle_menu_wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
            hireling_battle_menu_on_init(args);
        }

        private void wait_on_tick(MenuCallbackArgs args, CampaignTime time)
        {
            if (!_hirelingEnlisted || _hirelingEnlistingLord?.PartyBelongedTo == null)
            {
                while (Campaign.Current.CurrentMenuContext != null)
                {
                    GameMenu.ExitToLast();
                }
            }
            else
            {
                if (args.MenuContext?.GameMenu == null) return;
                TextObject text1 = args.MenuContext.GameMenu.GetText();
                TextObject text2 = GameTexts.FindText("tor_hireling", "maintext");
                text2.SetTextVariable("ENLISTING_LORD", _hirelingEnlistingLord.Name);

                var days = $"{_durationInDays:0.0}";
                text2.SetTextVariable("ENLISTING_DURATION", days);
                text2.SetTextVariable("HIRELING_BATTLE_COUNT", _manuallyFoughtBattles);

                var statusInfo = "";

                // Army status
                if (_hirelingEnlistingLord.PartyBelongedTo.Army != null)
                {
                    statusInfo += "{newLine}";
                    var armyText = TORTextHelper.GetTextObject("tor_hireling_serving_in_army_text", "Serving in {ARMY_NAME}");
                    armyText.SetTextVariable("ARMY_NAME", _hirelingEnlistingLord.PartyBelongedTo.Army.Name);
                    statusInfo += armyText.ToString();
                }

                // Siege status
                var lordParty = _hirelingEnlistingLord.PartyBelongedTo;
                var siegeEvent = lordParty.BesiegerCamp?.SiegeEvent ?? lordParty.Army?.ArmyOwner?.PartyBelongedTo?.BesiegerCamp?.SiegeEvent;
                if (siegeEvent != null)
                {
                    statusInfo += "{newLine}";
                    var siegeText = TORTextHelper.GetTextObject("tor_hireling_besieging_text", "Besieging {SETTLEMENT_NAME}");
                    siegeText.SetTextVariable("SETTLEMENT_NAME", siegeEvent.BesiegedSettlement.Name);
                    statusInfo += siegeText.ToString();

                    // Siege preparations progress
                    var besiegerEngines = siegeEvent.BesiegerCamp?.SiegeEngines;
                    if (besiegerEngines != null)
                    {
                        var preparations = besiegerEngines.SiegePreparations;
                        if (preparations != null && !preparations.IsConstructed)
                        {
                            statusInfo += "{newLine}";
                            var prepProgress = (int)(preparations.Progress * 100);
                            var prepText = TORTextHelper.GetTextObject("tor_hireling_siege_preparations_text", "Siege preparations: {PROGRESS}%");
                            prepText.SetTextVariable("PROGRESS", prepProgress);
                            statusInfo += prepText.ToString();
                        }
                        else
                        {
                            // Show active siege engines count
                            var activeEngines = besiegerEngines.DeployedSiegeEngines.Count(e => e.IsConstructed);
                            if (activeEngines > 0)
                            {
                                statusInfo += "{newLine}";
                                var enginesText = TORTextHelper.GetTextObject("tor_hireling_siege_engines_text", "Siege engines ready: {COUNT}");
                                enginesText.SetTextVariable("COUNT", activeEngines);
                                statusInfo += enginesText.ToString();
                            }
                        }
                    }
                }

                // Raid status
                var lordMapEvent = lordParty.MapEvent;
                if (lordMapEvent != null && (lordMapEvent.IsRaid || lordMapEvent.IsForcingSupplies || lordMapEvent.IsForcingVolunteers))
                {
                    statusInfo += "{newLine}";
                    var raidSettlement = lordMapEvent.MapEventSettlement;
                    if (raidSettlement != null)
                    {
                        var raidText = TORTextHelper.GetTextObject("tor_hireling_raiding_text", "Raiding {VILLAGE_NAME}");
                        raidText.SetTextVariable("VILLAGE_NAME", raidSettlement.Name);
                        statusInfo += raidText.ToString();
                    }
                }

                text2.SetTextVariable("ENLISTING_ARMY", statusInfo);

                TextObject variable = text2;
                text1.SetTextVariable("ENLISTING_TEXT", variable);

                args.MenuContext.SetBackgroundMeshName(_hirelingEnlistingLord.MapFaction.Culture.EncounterBackgroundMesh);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_enlisted", ref _hirelingEnlisted);
            dataStore.SyncData("_enlistingLord", ref _hirelingEnlistingLord);
            dataStore.SyncData("_entryServiceTimeStamp", ref _entryServiceTimeStamp);
            dataStore.SyncData("_manuallyFoughtBattles", ref _manuallyFoughtBattles);
            dataStore.SyncData("_durationInDays", ref _durationInDays);
            dataStore.SyncData("_currentActivityIndex", ref _currentActivityIndex);
            dataStore.SyncData("_inPostBattleTransition", ref _inPostBattleTransition);
            dataStore.SyncData("_joinedHirelingCleanupBattle", ref _joinedHirelingCleanupBattle);
            dataStore.SyncData("_lastCountedHirelingBattle", ref _lastCountedHirelingBattle);
            dataStore.SyncData("_deadJoinedEncounterCleanupTicks", ref _deadJoinedEncounterCleanupTicks);
        }

        private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args)
        {
            if (!_hirelingEnlisted)
            {
                return;
            }
            SetActivities();
        }

        private void hireling_battle_menu_on_init(MenuCallbackArgs args)
        {
            var battleParty = GetCurrentHirelingBattleParty();
            MapEvent mapEvent;
            MapEventSide lordSide;

            if (TryGetCurrentHirelingSiegeBattle(out _, out var siegeJoinedSide, out var siegeMapEvent))
            {
                mapEvent = siegeMapEvent;
                lordSide = siegeMapEvent.GetMapEventSide(siegeJoinedSide);
            }
            else
            {
                if (battleParty?.MapEvent == null)
                {
                    MBTextManager.SetTextVariable("BATTLE_INFO", TORTextHelper.GetTextObject("tor_hireling_battle_fallback", "Your Lord engages in a battle."));
                    return;
                }

                mapEvent = battleParty.MapEvent;
                lordSide = battleParty.MapEventSide;
            }

            var enemySide = lordSide?.OtherSide;

            // Battle type
            var battleTypeText = TORTextHelper.GetTextObject("tor_hireling_battle_type_battle", "Battle");
            if (mapEvent.IsSiegeAssault || mapEvent.IsSiegeOutside)
            {
                var isDefender = lordSide?.MissionSide == TaleWorlds.Core.BattleSideEnum.Defender;
                battleTypeText = isDefender
                    ? TORTextHelper.GetTextObject("tor_hireling_battle_type_siege_defense", "Siege Defense")
                    : TORTextHelper.GetTextObject("tor_hireling_battle_type_siege_assault", "Siege Assault");
            }

            // Enemy info
            var enemyLeader = enemySide?.LeaderParty?.LeaderHero?.Name?.ToString() ?? enemySide?.LeaderParty?.Name?.ToString() ?? "Unknown";

            // Strength calculation
            var allyStrength = lordSide?.RecalculateMemberCountOfSide() ?? 0;
            var enemyStrength = enemySide?.RecalculateMemberCountOfSide() ?? 0;

            // Odds calculation
            TextObject oddsText;
            if (enemyStrength == 0)
                oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_certain_victory", "Certain Victory");
            else
            {
                var ratio = (float)allyStrength / enemyStrength;
                if (ratio >= 2.0f) oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_overwhelming_advantage", "Overwhelming Advantage");
                else if (ratio >= 1.5f) oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_strong_advantage", "Strong Advantage");
                else if (ratio >= 1.1f) oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_slight_advantage", "Slight Advantage");
                else if (ratio >= 0.9f) oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_even", "Even Odds");
                else if (ratio >= 0.66f) oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_slight_disadvantage", "Slight Disadvantage");
                else if (ratio >= 0.5f) oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_strong_disadvantage", "Strong Disadvantage");
                else oddsText = TORTextHelper.GetTextObject("tor_hireling_odds_overwhelming_disadvantage", "Overwhelming Disadvantage");
            }

            var enemyText = TORTextHelper.GetTextObject("tor_hireling_battle_enemy", "Enemy: {ENEMY_LEADER}");
            enemyText.SetTextVariable("ENEMY_LEADER", enemyLeader);

            var oddsLineText = TORTextHelper.GetTextObject("tor_hireling_battle_odds", "Odds: {ODDS}");
            oddsLineText.SetTextVariable("ODDS", oddsText);

            var battleInfo = $"{battleTypeText}\n{enemyText}\n{oddsLineText}";

            MBTextManager.SetTextVariable("BATTLE_INFO", battleInfo);
        }

        /// <remarks>
        /// This event triggers before MapEventEnded (via OnPlayerBattleEndEvent).
        /// At this point, prisoners don't exist yet in the actual loot rosters.
        /// We use PendingLootManager to clear them when DoLootParty runs.
        /// </remarks>
        private void ControlPlayerLoot(MapEvent mapEvent)
        {
            if (!IsEnlisted())
            {
                return;
            }

            TryRegisterWinningHirelingBattle(mapEvent);

            PlayerEncounter.Current?.RosterToReceiveLootItems?.Clear();
            PendingLootedTroopManager.ResetAllPendingState();

            _hirelingWaitMenuShown = false;
        }

        internal static void ClearCurrentHirelingLoot()
        {
            var currentEncounter = PlayerEncounter.Current;
            if (currentEncounter != null)
            {
                currentEncounter.RosterToReceiveLootItems.Clear();
                currentEncounter.RosterToReceiveLootMembers.Clear();
                currentEncounter.RosterToReceiveLootPrisoners.Clear();
            }

            PendingLootedTroopManager.ResetAllPendingState();
        }

        private void OnPartyLeavesSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (!_hirelingEnlisted || _hirelingEnlistingLord == null)
            {
                return;
            }

            var enlistingLordLeftSettlement = _hirelingEnlistingLord.PartyBelongedTo == mobileParty;
            var mainPartyLeftSettlement = MobileParty.MainParty == mobileParty && mobileParty.CurrentSettlement == null;
            if (mainPartyLeftSettlement && _suppressMainPartySettlementLeaveHandling)
            {
                return;
            }

            if (!enlistingLordLeftSettlement && !mainPartyLeftSettlement)
            {
                return;
            }

            var mainParty = MobileParty.MainParty;
            var playerIsStillInsideSameSettlement =
                mainParty.CurrentSettlement == settlement ||
                PlayerEncounter.EncounterSettlement == settlement;

            if (enlistingLordLeftSettlement)
            {
                if (playerIsStillInsideSameSettlement)
                {
                    LeaveSettlementAndOpenHirelingMenu(false);
                    return;
                }

                if (PlayerEncounter.LocationEncounter != null)
                {
                    PlayerEncounter.LocationEncounter = null;
                }

                PartyBase.MainParty.SetVisualAsDirty();
                ActivateHirelingMenu();
                return;
            }

            if (PlayerEncounter.Current != null
                && PlayerEncounter.EncounterSettlement == null
                && PlayerEncounter.Current.EncounterState == PlayerEncounterState.End)
            {
                PlayerEncounter.Finish(false);
            }

            if (PlayerEncounter.LocationEncounter != null)
            {
                PlayerEncounter.LocationEncounter = null;
            }

            PartyBase.MainParty.SetVisualAsDirty();
            ActivateHirelingMenu();
        }

        private void EnlistingLordPartyEntersSettlement(MobileParty mobileParty, Settlement settlement, Hero partyHero)
        {
            if (!_hirelingEnlisted || _hirelingEnlistingLord == null)
            {
                return;
            }

            if (_hirelingEnlistingLord.PartyBelongedTo != mobileParty)
            {
                return;
            }

            if (!settlement.IsTown)
            {
                ActivateHirelingMenu();
                return;
            }

            if (MobileParty.MainParty.CurrentSettlement == settlement && PlayerEncounter.EncounterSettlement == settlement)
            {
                return;
            }

            if (!IsHirelingSettlementEncounterSafe(settlement))
            {
                GameMenu.SwitchToMenu("hireling_menu");

                if (_pauseModeToggle)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }

                return;
            }

            _inPostBattleTransition = false;
            ActivateHirelingMenu();

            if (_pauseModeToggle)
            {
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
            }
        }

        /// <remarks>
        /// Occurs after a OnPlayerBattleEndEvent.
        /// </remarks>
        private void MapEventEnded(MapEvent mapEvent)
        {
            if (_hirelingEnlistingLord == null || !IsEnlisted()) return;

            if (GetEnlistingLordIsInMapEvent(mapEvent) || mapEvent.IsPlayerMapEvent) //Sly : 2nd condition probably redunant, but why not
            {
                if (_hirelingEnlistingLord.PartyBelongedTo == null) //lord lost the battle and was captured/died, removing them from their party and therefore invalidating enlistment. Ending enlistment will be handled later by the OnTick.
                {
                    return;
                }

                // Mark that we're in a post-battle transition to prevent AI tick crashes
                // This is especially important after sieges when the lord immediately enters the settlement
                _inPostBattleTransition = true;
                PlayerEncounter.Current?.RosterToReceiveLootItems?.Clear();
                PendingLootedTroopManager.ResetAllPendingState();

                _hirelingLordIsFightingWithoutPlayer = false;

                var endedPlayerEncounterForThisBattle =
                    PlayerEncounter.Current != null &&
                    PlayerEncounter.EncounterSettlement == null &&
                    PlayerEncounter.Battle == mapEvent;

                if (endedPlayerEncounterForThisBattle && PlayerEncounter.Current.IsJoinedBattle != true)
                {
                    PlayerEncounter.Finish(false);
                    _hirelingWaitMenuShown = false;
                    return;
                }

                if (mapEvent.IsPlayerMapEvent)
                {
                    // native result / ally_thanks / encounter cleanup owns the flow here
                    _hirelingWaitMenuShown = false;
                    return;
                }
                ActivateHirelingMenu();
            }
        }

        private bool TryCleanupDeadJoinedHirelingEncounter()
        {
            var currentEncounter = PlayerEncounter.Current;
            var currentBattle = PlayerEncounter.Battle;
            var currentMenuId = Campaign.Current.CurrentMenuContext?.GameMenu?.StringId;
            var lordParty = _hirelingEnlistingLord?.PartyBelongedTo;

            var hasTrackedNativeFieldEncounter =
                !_startBattle &&
                currentEncounter != null &&
                PlayerEncounter.EncounterSettlement == null &&
                (_joinedHirelingCleanupBattle != null || _inPostBattleTransition);

            if (!hasTrackedNativeFieldEncounter)
            {
                if (currentEncounter == null)
                {
                    _joinedHirelingCleanupBattle = null;
                }

                _deadJoinedEncounterCleanupTicks = 0;
                return false;
            }

            var trackedBattleStillRunning =
                currentBattle != null &&
                _joinedHirelingCleanupBattle != null &&
                currentBattle == _joinedHirelingCleanupBattle &&
                currentEncounter.EncounterState != PlayerEncounterState.End &&
                !currentBattle.HasWinner;

            if (trackedBattleStillRunning)
            {
                _deadJoinedEncounterCleanupTicks = 0;
                return false;
            }

            var lordHasDeadBattleState =
                lordParty?.MapEvent != null &&
                !IsJoinableHirelingMapEvent(lordParty.MapEvent, lordParty.MapEventSide?.MissionSide);

            var deadNativeEncounter =
                HasDeadNativeFieldEncounter() || lordHasDeadBattleState;

            if (!deadNativeEncounter)
            {
                _deadJoinedEncounterCleanupTicks = 0;
                return false;
            }

            if (currentMenuId != "hireling_menu" && currentMenuId != "encounter")
            {
                _deadJoinedEncounterCleanupTicks = 0;
                return false;
            }

            _deadJoinedEncounterCleanupTicks++;

            if (_deadJoinedEncounterCleanupTicks < STRICT_JOINED_CLEANUP_DELAY_TICKS)
            {
                return false;
            }

            TORCommon.Log("hireling dead native encounter cleanup. report this to developers", NLog.LogLevel.Warn);
            InformationManager.DisplayMessage(new InformationMessage("hireling dead native encounter cleanup. report this to developers"));

            PlayerEncounter.Finish(false);
            _hirelingWaitMenuShown = false;
            _joinedHirelingCleanupBattle = null;
            _deadJoinedEncounterCleanupTicks = 0;

            return true;
        }

        private void OnTick(float dt)
        {
            if (_hirelingEnlisted && _hirelingEnlistingLord?.PartyBelongedTo != null)
            {
                if (TryCleanupUnsafeHirelingSettlementState())
                {
                    GameMenu.ActivateGameMenu("hireling_menu");
                    _hirelingWaitMenuShown = true;

                    if (_pauseModeToggle)
                    {
                        Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                    }

                    return;
                }
                // Clear post-battle transition flag once we're stable (no active battle, menu shown)
                var playerParty = MobileParty.MainParty;
                var hasDetachedPersistedBattleState =
                    PlayerEncounter.Current == null
                    && !HasPendingNativeHirelingEncounterCleanup()
                    && !HasJoinableHirelingBattle()
                    && (playerParty.MapEventSide != null || playerParty.BesiegerCamp != null);

                if ((_inPostBattleTransition || hasDetachedPersistedBattleState)
                    && !HasPendingNativeHirelingEncounterCleanup()
                    && !HasJoinableHirelingBattle()
                    && PlayerEncounter.Current == null)
                {
                    playerParty.MapEventSide = null;
                    playerParty.BesiegerCamp = null;
                    playerParty.CurrentSettlement = null;
                    _inPostBattleTransition = false;
                }
                var timeModel = Campaign.Current.Models.CampaignTimeModel;
                _durationInDays = timeModel.CampaignStartTime.ElapsedDaysUntilNow - _entryServiceTimeStamp;

                var currentMenuContext = Campaign.Current.CurrentMenuContext;
                if (currentMenuContext?.GameMenu?.StringId == "hireling_battle_menu"
                    && !_hirelingLordIsFightingWithoutPlayer)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }

                var hasTrackedDeadHirelingResultEncounter = HasTrackedDeadHirelingResultEncounter();
                if (currentMenuContext == null && hasTrackedDeadHirelingResultEncounter)
                {
                    CleanupTrackedDeadHirelingResultEncounter();
                    ActivateAndRefreshHirelingMenu();
                    return;
                }

                if (currentMenuContext?.GameMenu?.StringId == "hireling_menu")
                {
                    var hirelingMenu = Campaign.Current.GameMenuManager.GetGameMenu("hireling_menu");
                    hirelingMenu.RunOnTick(currentMenuContext, dt);
                }

                if (TryCleanupDeadJoinedHirelingEncounter())
                {
                    return;
                }
                if (TryFinalizeTrackedHirelingSiegeBattle())
                {
                    return;
                }

                var currentEncounter = PlayerEncounter.Current;
                var currentEncounterBattle = PlayerEncounter.Battle;
                var currentEncounterSettlement = PlayerEncounter.EncounterSettlement;

                var hirelingLordParty = _hirelingEnlistingLord.PartyBelongedTo;
                var hirelingArmyOwnerParty = hirelingLordParty.Army?.ArmyOwner?.PartyBelongedTo;
                var activeBesiegedSettlement = hirelingLordParty.BesiegerCamp?.SiegeEvent?.BesiegedSettlement
                    ?? hirelingArmyOwnerParty?.BesiegerCamp?.SiegeEvent?.BesiegedSettlement;

                var hasStaleSettlementEncounterBlockingBattleMenu =
                    currentMenuContext?.GameMenu?.StringId == "hireling_menu"
                    && currentEncounter != null
                    && currentEncounterSettlement != null
                    && MobileParty.MainParty.CurrentSettlement == null
                    && currentEncounterSettlement != hirelingLordParty.CurrentSettlement
                    && currentEncounterSettlement != activeBesiegedSettlement
                    && (currentEncounterBattle == null
                        || currentEncounterBattle.State == MapEventState.WaitingRemoval
                        || currentEncounterBattle.HasWinner);

                if (hasStaleSettlementEncounterBlockingBattleMenu)
                {
                    if (PlayerEncounter.LocationEncounter != null)
                    {
                        PlayerEncounter.LocationEncounter = null;
                    }

                    PlayerEncounter.Finish(false);
                    _inPostBattleTransition = false;
                    _hirelingWaitMenuShown = false;

                    currentMenuContext = Campaign.Current.CurrentMenuContext;
                }

                var waitingForNativeEncounterCleanup =
                    HasAnyNativeFieldEncounter()
                    || HasActiveNativeSettlementEncounter()
                    || HasPendingNativeHirelingEncounterCleanup();

                if (!_hirelingWaitMenuShown
                    && !waitingForNativeEncounterCleanup
                    && !hasTrackedDeadHirelingResultEncounter)
                {
                    ActivateAndRefreshHirelingMenu();
                }
                HidePlayerParty();

                var enlistingLordParty = _hirelingEnlistingLord.PartyBelongedTo;
                if (enlistingLordParty.CurrentSettlement == null)
                {
                    PartyBase.MainParty.MobileParty.Position = enlistingLordParty.Position;
                }

                var battleParty = GetCurrentHirelingBattleParty();
                var hasJoinableHirelingSiegeBattle = TryGetCurrentHirelingSiegeBattle(out _, out _, out var siegeMapEvent);

                if (!HasAnyNativeFieldEncounter()
                    && !HasActiveNativeSettlementEncounter()
                    && (battleParty?.MapEvent != null || hasJoinableHirelingSiegeBattle))
                {
                    var mapEvent = hasJoinableHirelingSiegeBattle ? siegeMapEvent : battleParty.MapEvent;
                    var currentHirelingBattleIsJoinable = hasJoinableHirelingSiegeBattle || IsCurrentHirelingBattleJoinable(battleParty);

                    if (!currentHirelingBattleIsJoinable)
                    {
                        if (currentMenuContext?.GameMenu?.StringId == "hireling_battle_menu")
                        {
                            GameMenu.ActivateGameMenu("hireling_menu");
                            _hirelingWaitMenuShown = true;
                        }
                    }
                    else
                    {
                        if (mapEvent.IsRaid || mapEvent.IsForcingSupplies || mapEvent.IsForcingVolunteers)
                        {
                            var lordIsOnRaidSide = mapEvent.AttackerSide.Parties.Any(x => x.Party == _hirelingEnlistingLord.PartyBelongedTo.Party);
                            if (lordIsOnRaidSide && !HasExternalRaidInterrupter(mapEvent))
                            {
                                return;
                            }
                        }

                        if (!_hirelingLordIsFightingWithoutPlayer
                            && currentMenuContext?.GameMenu?.StringId != "hireling_battle_menu")
                        {
                            _hirelingWaitMenuShown = true;
                            GameMenu.ActivateGameMenu("hireling_battle_menu");
                        }
                    }
                }

            }
            else if (_hirelingEnlisted && _hirelingEnlistingLord?.PartyBelongedTo == null)
            {
                LeaveLordPartyAction();
            }
        }

        private void UndoDiplomacy()
        {
            ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(Hero.MainHero.Clan, false);
        }

        private void EnlistPlayer()
        {
            _hirelingEnlistingLord = CharacterObject.OneToOneConversationCharacter.HeroObject;
            HidePlayerParty();
            DisbandParty();
            //sets the player party to be ignored so it can't be targeted; refreshed with the IgnoreHirelingPartyRefresh event
            MobileParty.MainParty.IgnoreForHours(8f); //may have to set the player party to notActive maybe?
            Hero.MainHero.AddAttribute("enlisted");

            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(Hero.MainHero.Clan, _hirelingEnlistingLord.Clan.Kingdom, default, 25, false);
            MBTextManager.SetTextVariable("ENLISTINGLORDNAME", _hirelingEnlistingLord.EncyclopediaLinkWithName);

            while (Campaign.Current.CurrentMenuContext != null)
                GameMenu.ExitToLast();

            // Finish the PlayerEncounter from the conversation with the lord
            // This prevents EncounteredMobileParty from blocking the lord's siege initiation
            if (PlayerEncounter.Current != null && PlayerEncounter.EncounterSettlement == null)
                PlayerEncounter.Finish(false);
            PendingLootedTroopManager.ResetAllPendingState();

            _hirelingEnlisted = true;

            if (Clan.PlayerClan.Influence != 0f)
            {
                ChangeClanInfluenceAction.Apply(Clan.PlayerClan, -Clan.PlayerClan.Influence);
            }

            _currentTrainedSkill = null;
            _currentActivityIndex = 0;
            _lastCountedHirelingBattle = null;

            SetActivities();
            var timeModel = Campaign.Current.Models.CampaignTimeModel;
            _entryServiceTimeStamp = timeModel.CampaignStartTime.ElapsedDaysUntilNow;
            GameMenu.ActivateGameMenu("hireling_menu");
        }

        private void ShowPlayerParty()
        {
            // Currently not working ??
            PartyBase.MainParty.MobileParty.IsVisible = true;
        }

        private void HidePlayerParty()
        {
            PartyBase.MainParty.MobileParty.IsVisible = false;
        }

        private void DisbandParty()
        {
            if (MobileParty.MainParty.MemberRoster.TotalManCount <= 1)
                return;
            List<TroopRosterElement> troopRosterElementList = [];
            foreach (TroopRosterElement troopRosterElement in MobileParty.MainParty.MemberRoster.GetTroopRoster())
            {
                if (troopRosterElement.Character != Hero.MainHero.CharacterObject && troopRosterElement.Character.HeroObject == null)
                    troopRosterElementList.Add(troopRosterElement);
            }
            if (troopRosterElementList.Count == 0)
                return;
            foreach (TroopRosterElement troopRosterElement in troopRosterElementList)
            {
                MobileParty.MainParty.MemberRoster.AddToCounts(troopRosterElement.Character, -1 * troopRosterElement.Number);
                EnlistingLord.PartyBelongedTo.MemberRoster.AddToCounts(troopRosterElement.Character, 1 * troopRosterElement.Number);

            }
        }

        /// <remarks>
        /// In *the* map event passed as an argument, not *a* map event. Use MobileParty.MapEvent for the latter.
        /// </remarks>
        private bool GetEnlistingLordIsInMapEvent(MapEvent mapEvent)
        {
            if (mapEvent == null) return false;

            var lordParty = _hirelingEnlistingLord?.PartyBelongedTo?.Party;
            if (lordParty == null)
            {
                return false;
            }

            foreach (var involvedParty in mapEvent.InvolvedParties)
            {
                if (involvedParty == lordParty)
                {
                    return true;
                }
            }
            return false;
        }
    }
}