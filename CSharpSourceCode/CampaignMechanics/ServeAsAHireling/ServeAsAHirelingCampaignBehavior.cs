using Helpers;
using System;
using System.Collections.Generic;
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

        private bool _hirelingWaitMenuShown;

        private float _entryServiceTimeStamp;

        private SkillObject _currentTrainedSkill;
        private int _currentActivityIndex;

        private bool _enlistInquiryDeclined;

        public float DurationInDays => _durationInDays;

        public int ManuallyFoughtBattles => _manuallyFoughtBattles;


        public bool IsEnlisted()
        {
            return _hirelingEnlisted;
        }

        public Hero EnlistingLord => _hirelingEnlistingLord;

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
            CampaignEvents.GameMenuOptionSelectedEvent.AddNonSerializedListener(this, ContinueTimeAfterLeftSettlementWhileEnlisted);
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
            if (component.IsPlayerMapEvent)
            {
                GameMenu.ActivateGameMenu("hireling_menu");
            }
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
                    _currentTrainedSkill = _activities.GetHirelingActivities(Hero.MainHero.GetCareer())[0];
                    _currentActivityIndex = 0;
                }

                if (_currentTrainedSkill != null && Hero.MainHero.IsHealthFull())
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

        private void ContinueTimeAfterLeftSettlementWhileEnlisted(GameMenu menu, GameMenuOption option)
        {
            if (_hirelingEnlisted && option.IdString == "town_leave")
            {
                GameMenu.ActivateGameMenu("hireling_menu");
                Campaign.Current.TimeControlMode = CampaignTimeControlMode.StoppableFastForward;
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

        private void MenuOpened(MenuCallbackArgs obj)
        {
            if (_startBattle && obj.MenuContext.GameMenu.StringId == "encounter" && !_debugSkipBattles)
            {
                _startBattle = false;

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
                var damage = new TextObject("This will harm your relations with the entire faction.");
                GameTexts.SetVariable("HIRELING_DESERT_TEXT", damage);
            }
            else
            {
                GameTexts.SetVariable("HIRELING_DESERT_TEXT", "");
            }

            var titleText = new TextObject("{=FLT0000044}Abandon Party");
            var text = new TextObject("{=FLT0000046}Are you sure you want to abandon the party? {HIRELING_DESERT_TEXT}");
            var affirmativeText = new TextObject("{=FLT0000047}Yes");
            var negativeText = new TextObject("{=FLT0000048}No");
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

            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword_quit_sure", "I would like to quit my service.", QuitCondition, null);
            campaignGameStarter.AddDialogLine("payedsword_quit_sure", "payedsword_quit_sure", "payedsword_quit_choice", "Are you sure?", null, null);
            campaignGameStarter.AddPlayerLine("payedsword_quit_choice", "payedsword_quit_choice", "payedsword_quit", "Yes i want to leave", null, null); //are these localized?
            campaignGameStarter.AddPlayerLine("payedsword_quit_choice", "payedsword_quit_choice", "lord_pretalk", "I have to think about this.", null, null);
            campaignGameStarter.AddDialogLine("payedsword_quit", "payedsword_quit", "end", quitText.Value, null, LeaveLordPartyAction);

            campaignGameStarter.AddPlayerLine("convincelord", "lord_talk_speak_diplomacy_2", "payedsword_explain", "I am hereby offering my sword.", () => SanityCheck() && !IsEnlisted() && ServeAsAHirelingHelpers.HirelingServiceConditions(), null);
            campaignGameStarter.AddDialogLine("payedsword_explain", "payedsword_explain", "hireling_decide_player", explainText.Value, null, null, 200);
            campaignGameStarter.AddPlayerLine("hireling_decide_player", "hireling_decide_player", "hireling_prompt", "I accept my Lord.", ServeAsAHirelingHelpers.HirelingServiceConditions, () => DisplayPrompt(EnlistPlayer));
            campaignGameStarter.AddPlayerLine("hireling_decide_player", "hireling_decide_player", "lord_pretalk", "I need to think about this", null, null);
            campaignGameStarter.AddDialogLine("hireling_prompt", "hireling_prompt", "hireling_decision", "...", null, null);
            campaignGameStarter.AddPlayerLine("hireling_decision", "hireling_decision", "lord_pretalk", "I need to think about this", () => _enlistInquiryDeclined, null);
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
            var explaination = GameTexts.FindText("tor_hireling", "prompttext");
            _enlistInquiryDeclined = false;
            var inquiry = new InquiryData(title.ToString(),
                explaination.ToString(),
                true,
                true,
                "Accept", "Decline",
                enlistPlayer,
                () => _enlistInquiryDeclined = true);
            InformationManager.ShowInquiry(inquiry);
        }

        private void SetupHirelingMenu(CampaignGameStarter campaignGameStarter)
        {
            var infotext = new TextObject("{ENLISTING_TEXT}");

            campaignGameStarter.AddGameMenuOption("town", "town_back_to_hireling", "Back", args =>
            {
                args.optionLeaveType = GameMenuOption.LeaveType.Leave;
                return IsEnlisted();
            }, args => GameMenu.SwitchToMenu("hireling_menu"), true);

            campaignGameStarter.AddWaitGameMenu("hireling_menu", infotext.Value, party_wait_talk_to_other_members_on_init, wait_on_condition,
                null, wait_on_tick, GameMenu.MenuAndOptionType.WaitMenuHideProgressAndHoursOption);

            var textObjectHirelingEnterSettlement = new TextObject("Enter the settlement");
            campaignGameStarter.AddGameMenuOption("hireling_menu", "enter_town", textObjectHirelingEnterSettlement.ToString(), args =>
            {
                if (!IsEnlisted())
                {
                    return false;
                }
                args.optionLeaveType = GameMenuOption.LeaveType.Continue;

                //Sly : this can return a null for PartyBelongedTo if the enlisted lord is part of a siege encampment and they lose with player being downed. (The original context was : besieger attacks town, I choose "Avoid Combat" to begin a simulation, an outside army attacks the encampment from behind, I then join join battle for a field one outside, we lose and the allied army retreats, I then had a native encounter menu, I choose "Attack" which creates another field battle, I down during the battle (enlisted lord was downed in one of the 2 battles), on battle end a NRE occurs here from null.CurrentSettlement.
                //I believe that the issue stems from the player and lord being downed and therefore part of the prisoner loot roster for the otherside, ie. the lord is now without party, and the game attempts to restore the last relevant menu (hireling menu) which then checks this condition.
                //This is probably intended to be addressed by the OnTick action which checks for a null enlisted party and ends enlistment if yes, but because the transition from mission end to restored menu occurs while the game is paused, no tick occurs and the _hirelingEnlistingLord field hasn't been cleared yet.
                //There is the OnMobilePartyDestroyed action which I'd expect to trigger and therefore handle ending enlistment, but I'm unsure if it didn't occur because the lord being taken prisoner makes destroyedParty.Leader null and therefore the condition isn't true, or if the crash here predates the emprisoning of the lord and therefore the destruction of the party.
                //this probably shouldn't even be hit now that it uses ExitToLast on event end

                return _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement != null &&
                _hirelingEnlistingLord.PartyBelongedTo.CurrentSettlement == PlayerEncounter.EncounterSettlement &&
                PlayerEncounter.EncounterSettlement.IsTown;
            }, args =>
            {
                GameMenu.SwitchToMenu("town");
            }, true);

            var text = new TextObject("{PAUSE_ONOFF_TEXT}");
            campaignGameStarter.AddGameMenuOption("hireling_menu", "pause_time_option", text.Value, null, PauseModeToggle);
            var pauseText = GameTexts.FindText("tor_hireling", "pausetime");
            pauseText.SetTextVariable("PAUSE_ONOFF", "off");
            GameTexts.SetVariable("PAUSE_ONOFF_TEXT", pauseText);

            var lordTalkText = GameTexts.FindText("tor_hireling", "talktolord");
            campaignGameStarter.AddGameMenuOption("hireling_menu", "activity0_option", lordTalkText.Value, null, args => StartDialog());

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

            campaignGameStarter.AddGameMenuOption("hireling_menu", "empty", "", args =>
            {
                args.IsEnabled = false; return true;
            }, null);

            campaignGameStarter.AddGameMenuOption("hireling_menu", "party_wait_leave", "Desert", args =>
            {
                var infoText = new TextObject("{=FLT0000045}This will damage your reputation with the {FACTION}. Serve for {MINIMUMSERVEDAYS} days, and speak to your enlisting Lord to avoid consequences");
                string factionName = (_hirelingEnlistingLord != null) ? _hirelingEnlistingLord.MapFaction.Name.ToString() : "DATA CORRUPTION ERROR";
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
            if (_hirelingEnlistingLord?.PartyBelongedTo?.MapEvent != null)
            {
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

        private void SetupBattleMenu(CampaignGameStarter campaignGameStarter)
        {
            TextObject hirelingBattleTextMenu = new("Your Lord engages in a battle.");
            campaignGameStarter.AddGameMenu("hireling_battle_menu", hirelingBattleTextMenu.Value, party_wait_talk_to_other_members_on_init, GameMenu.MenuOverlayType.Encounter);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_join_battle", "Join battle",
                hireling_battle_menu_join_battle_on_condition,
                delegate
                {
                    while (Campaign.Current.CurrentMenuContext != null)
                    {
                        GameMenu.ExitToLast();
                    }
                    if (_hirelingEnlistingLord.PartyBelongedTo.MapEvent != null)
                    {
                        var mapEvent = _hirelingEnlistingLord.PartyBelongedTo.MapEvent;
                        var eventAlliedLeaderParty = _hirelingEnlistingLord.PartyBelongedTo.MapEventSide.LeaderParty.MobileParty;

                        if (eventAlliedLeaderParty == null)
                        {
                            TORCommon.Log("hireling_join_battle consequence : eventAlliedLeaderParty is null", NLog.LogLevel.Error);
                        }

                        var enemyLeaderBase = eventAlliedLeaderParty.MapEventSide.OtherSide.LeaderParty;
                        if (enemyLeaderBase == null)
                        {
                            TORCommon.Log("hireling_join_battle consequence : enemyLeaderBase is null", NLog.LogLevel.Error);
                        }

                        //the crash is from attempting to join a siege that the enlisting lord is in (siege leader or follower unknown) - is the issue the player not being part of the siege event? or maybe the besieger camp?
                        var playerParty = MobileParty.MainParty;
                        playerParty.MapEventSide = eventAlliedLeaderParty.MapEventSide;
                        playerParty.BesiegerCamp = eventAlliedLeaderParty.BesiegerCamp;
                        playerParty.CurrentSettlement = eventAlliedLeaderParty.CurrentSettlement;

                        var forceOutOfSettlement = eventAlliedLeaderParty.SiegeEvent == null;

                        if (mapEvent.IsSiegeAssault) //a siege event is SiegeOutside when the player is not present - is the PlayerEncounter.Init patch in EncounterPatches meant to solve this issue and it had a side effect of causing an issue when not enlisted?
                        //SiegeAssault doesn't know whether it's the attacker or defender, it's just that the map event is in a Siege state
                        {//Sly : why is this doing the same thing as the StartBattleAction call above, but behind further conditionals?
                            //Likely can be removed as I think it never gets past the conditionals inside and the map event won't be a siege assault unless it has already started a player-involved map event
                            Game.Current.AfterTick += InitializeSiegeBattle;
                            _siegeBattleMissionStarted = true;
                        }
                        else
                        {
                            StartBattleAction.Apply(PartyBase.MainParty, enemyLeaderBase); //(Zerca's prior comment?) : changing the direction fixed the sole defender bug for the player.
                            //It seems the defense has in joining no meaning
                        }
                        _startBattle = true;
                        _hirelingLordIsFightingWithoutPlayer = false;
                    }
                }
                , false, 4);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_avoid_combat", "Avoid Combat",
               hireling_battle_menu_avoid_combat_on_condition,
               delegate (MenuCallbackArgs args)
               {
                   _hirelingLordIsFightingWithoutPlayer = true;
                   _startBattle = false;
                   args.MenuContext.GameMenu.StartWait();
               }
               , false, 4);

            campaignGameStarter.AddGameMenuOption("hireling_battle_menu", "hireling_flee", "Flee",
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

            var onOffText = "Off";
            if (_pauseModeToggle)
            {
                onOffText = "On";
            }

            TextObject text2 = GameTexts.FindText("tor_hireling", "pausetime");
            text2.SetTextVariable("PAUSE_ONOFF", onOffText);

            GameTexts.SetVariable("PAUSE_ONOFF_TEXT", text2);
            args.Text = text2;
            args.MenuContext.Refresh();
        }

        public void LeaveLordPartyAction()
        {
            _hirelingEnlisted = false;
            _hirelingEnlistingLord = null;
            _hirelingWaitMenuShown = false;
            //I am not sure why this was needed? Putting it in makes it crash if you leave service while in a town for example.
            //This makes PlayerEncounter.EncounterSettlement null which is accessed via vanilla gamemenu init methods
            //crash does not occur, and finishing encounter is important to not end in a invalid state, where parties try to engange with player but can't
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
            var maxHitPointsHero = Hero.MainHero.MaxHitPoints;
            var hitPointsHero = Hero.MainHero.HitPoints;
            return hitPointsHero > maxHitPointsHero * 0.2;
        }

        private bool hireling_battle_menu_desert_on_condition(MenuCallbackArgs args)
        {
            return _hirelingEnlistingLord.CurrentSettlement == null;
        }

        private bool hireling_battle_menu_avoid_combat_on_condition(MenuCallbackArgs args)
        {
            if (_hirelingEnlistingLord == null) return false;

            var lordParty = _hirelingEnlistingLord.PartyBelongedTo;

            if (lordParty?.MapEvent == null) return false;

            if (Hero.MainHero.IsWounded)
                return true;

            var partyStrength = GetEnlistingLordEventStrengthRatio(lordParty); // (ally strength / enemy) => allies stronger when value > 1
            var combatStrengthThreshold = partyStrength > RatioPartyAgainstEnemyStrength;//allies' strength at least 3x greater than enemy to simulate

            return combatStrengthThreshold;
        }

        private bool wait_on_condition(MenuCallbackArgs args)
        {
            return true;
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

                var armyInfo = "";
                if (_hirelingEnlistingLord.PartyBelongedTo.Army != null)
                {
                    armyInfo += "{newLine}";
                    armyInfo += $"is Part of {_hirelingEnlistingLord.PartyBelongedTo.Army.Name}";
                }
                text2.SetTextVariable("ENLISTING_ARMY", armyInfo);

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
        }

        private void party_wait_talk_to_other_members_on_init(MenuCallbackArgs args) { }

        /// <remarks>
        /// This event triggers before MapEventEnded.
        /// </remarks>
        private void ControlPlayerLoot(MapEvent mapEvent)
        {
            if (IsEnlisted() && mapEvent.PlayerSide == mapEvent.WinningSide)
            {
                if (!_hirelingLordIsFightingWithoutPlayer)
                {
                    _manuallyFoughtBattles++;
                }

                PlayerEncounter.Current.RosterToReceiveLootItems.Clear();
                PlayerEncounter.Current.RosterToReceiveLootMembers.Clear();
                PlayerEncounter.Current.RosterToReceiveLootPrisoners.Clear();
            }

            _hirelingWaitMenuShown = false;
        }

        private void OnPartyLeavesSettlement(MobileParty mobileParty, Settlement settlement)
        {
            if (!_hirelingEnlisted || _hirelingEnlistingLord == null) return;

            if (_hirelingEnlistingLord.PartyBelongedTo == mobileParty || (MobileParty.MainParty == mobileParty && mobileParty.CurrentSettlement == null))
            {
                while (Campaign.Current.CurrentMenuContext != null)
                    GameMenu.ExitToLast();
                if (PlayerEncounter.Current != null && PlayerEncounter.Current.EncounterState == PlayerEncounterState.End)
                    PlayerEncounter.Finish();
                if (PartyBase.MainParty.MobileParty.CurrentSettlement != null)
                    PlayerEncounter.LeaveSettlement();
                if (PlayerEncounter.LocationEncounter != null)
                    PlayerEncounter.LocationEncounter = null;
                PartyBase.MainParty.SetVisualAsDirty();
                GameMenu.ActivateGameMenu("hireling_menu");
            }
        }

        private void EnlistingLordPartyEntersSettlement(MobileParty mobileParty, Settlement settlement, Hero partyHero)
        {
            if (!_hirelingEnlisted || !settlement.IsTown) return;
            if (MobileParty.MainParty.CurrentSettlement == settlement && PlayerEncounter.EncounterSettlement == settlement) return;
            if (_hirelingEnlistingLord != null && _hirelingEnlistingLord.PartyBelongedTo == mobileParty)
            {
                EnterSettlementAction.ApplyForParty(MobileParty.MainParty, _hirelingEnlistingLord.CurrentSettlement);
                EncounterManager.StartSettlementEncounter(MobileParty.MainParty, settlement);
                GameMenu.SwitchToMenu("hireling_menu");

                if (_pauseModeToggle)
                {
                    Campaign.Current.TimeControlMode = CampaignTimeControlMode.Stop;
                }
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
                _hirelingLordIsFightingWithoutPlayer = false;
                GameMenu.ActivateGameMenu("hireling_menu");
                _hirelingWaitMenuShown = true;
            }
        }

        private void OnTick(float dt)
        {
            if (_hirelingEnlisted && _hirelingEnlistingLord?.PartyBelongedTo != null)
            {
                var menu = Campaign.Current.GameMenuManager.GetGameMenu("hireling_menu");
                var timeModel = Campaign.Current.Models.CampaignTimeModel;
                _durationInDays = timeModel.CampaignStartTime.ElapsedDaysUntilNow - _entryServiceTimeStamp;//could be in an hourly or daily tick instead
                menu.RunOnTick(Campaign.Current.CurrentMenuContext, dt);

                if (!_hirelingWaitMenuShown)
                {
                    GameMenu.ActivateGameMenu("hireling_menu");
                    _hirelingWaitMenuShown = true;
                    SetActivities();
                    Campaign.Current.CurrentMenuContext.Refresh();
                }

                HidePlayerParty();
                PartyBase.MainParty.MobileParty.Position = _hirelingEnlistingLord.PartyBelongedTo.Position;
                if (_hirelingEnlistingLord.PartyBelongedTo.MapEvent != null)
                {
                    var mapEvent = _hirelingEnlistingLord.PartyBelongedTo.MapEvent;

                    if (!_hirelingLordIsFightingWithoutPlayer && !mapEvent.HasWinner)
                    {
                        GameMenu.ActivateGameMenu("hireling_battle_menu");
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
            _hirelingEnlisted = true;

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

            if (_hirelingEnlistingLord?.PartyBelongedTo?.MapEvent == mapEvent) return true;

            return false;
        }
    }
}