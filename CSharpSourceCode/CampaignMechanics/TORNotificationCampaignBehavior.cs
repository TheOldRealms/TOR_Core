using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Issues;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.CampaignMechanics.Diplomacy;
using TOR_Core.Extensions;
using TOR_Core.Models;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORNotificationCampaignBehavior : CampaignBehaviorBase
    {
        private List<Tuple<bool, float>> _foodNotificationList = new List<Tuple<bool, float>>();


        public override void RegisterEvents()
        {
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.HeroRelationChanged.AddNonSerializedListener(this, OnRelationChanged);
            CampaignEvents.HeroLevelledUp.AddNonSerializedListener(this, OnHeroLevelledUp);
            CampaignEvents.HeroGainedSkill.AddNonSerializedListener(this, OnHeroGainedSkill);
            CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedFaction);
            CampaignEvents.ArmyCreated.AddNonSerializedListener(this, OnArmyCreated);
            CampaignEvents.OnPlayerArmyLeaderChangedBehaviorEvent.AddNonSerializedListener(this, OnPlayerArmyLeaderChangedBehaviorEvent);
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.RenownGained.AddNonSerializedListener(this, OnRenownGained);
            CampaignEvents.BeforeHeroesMarried.AddNonSerializedListener(this, OnHeroesMarried);
            CampaignEvents.PartyRemovedFromArmyEvent.AddNonSerializedListener(this, OnPartyRemovedFromArmy);
            CampaignEvents.ArmyDispersed.AddNonSerializedListener(this, OnArmyDispersed);
            CampaignEvents.OnPartyJoinedArmyEvent.AddNonSerializedListener(this, OnPartyJoinedArmy);
            CampaignEvents.OnChildConceivedEvent.AddNonSerializedListener(this, OnChildConceived);
            CampaignEvents.OnGivenBirthEvent.AddNonSerializedListener(this, OnGivenBirth);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.HeroOrPartyTradedGold.AddNonSerializedListener(this, OnHeroOrPartyTradedGold);
            CampaignEvents.OnTroopsDesertedEvent.AddNonSerializedListener(this, OnTroopsDeserted);
            CampaignEvents.ClanTierIncrease.AddNonSerializedListener(this, OnClanTierIncreased);
            CampaignEvents.OnSiegeBombardmentHitEvent.AddNonSerializedListener(this, OnSiegeBombardmentHit);
            CampaignEvents.OnSiegeBombardmentWallHitEvent.AddNonSerializedListener(this, OnSiegeBombardmentWallHit);
            CampaignEvents.OnSiegeEngineDestroyedEvent.AddNonSerializedListener(this, OnSiegeEngineDestroyed);
            CampaignEvents.BattleStarted.AddNonSerializedListener(this, OnBattleStarted);
            CampaignEvents.OnSiegeEventStartedEvent.AddNonSerializedListener(this, OnSiegeEventStarted);
            CampaignEvents.ItemsLooted.AddNonSerializedListener(this, OnItemsLooted);
            CampaignEvents.OnHideoutSpottedEvent.AddNonSerializedListener(this, OnHideoutSpotted);
            CampaignEvents.OnHeroSharedFoodWithAnotherHeroEvent.AddNonSerializedListener(this, OnHeroSharedFoodWithAnotherHero);
            CampaignEvents.OnQuestStartedEvent.AddNonSerializedListener(this, OnQuestStarted);
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
            CampaignEvents.QuestLogAddedEvent.AddNonSerializedListener(this, OnQuestLogAdded);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, OnPrisonerTaken);
            CampaignEvents.CharacterBecameFugitiveEvent.AddNonSerializedListener(this, OnHeroBecameFugitive);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
            CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
            CampaignEvents.OnIssueUpdatedEvent.AddNonSerializedListener(this, OnIssueUpdated);
            CampaignEvents.HeroOrPartyGaveItem.AddNonSerializedListener(this, OnHeroOrPartyGaveItem);
            CampaignEvents.RebellionFinished.AddNonSerializedListener(this, OnRebellionFinished);
            CampaignEvents.TournamentFinished.AddNonSerializedListener(this, OnTournamentFinished);
            CampaignEvents.OnBuildingLevelChangedEvent.AddNonSerializedListener(this, OnBuildingLevelChanged);
            CampaignEvents.CompanionRemoved.AddNonSerializedListener(this, OnCompanionRemoved);
            CampaignEvents.OnHeroTeleportationRequestedEvent.AddNonSerializedListener(this, OnHeroTeleportationRequested);
            CampaignEvents.OnPartyAddedToMapEventEvent.AddNonSerializedListener(this, OnPartyAddedToMapEvent);
            CampaignEvents.OnCallToWarAgreementStartedEvent.AddNonSerializedListener(this, OnCallToWarAgreementStarted);
            CampaignEvents.OnCallToWarAgreementEndedEvent.AddNonSerializedListener(this, OnCallToWarAgreementEnded);
            CampaignEvents.OnAllianceStartedEvent.AddNonSerializedListener(this, OnAllianceStarted);
            CampaignEvents.OnAllianceEndedEvent.AddNonSerializedListener(this, OnAllianceEnded);
        }

        private void OnAllianceStarted(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (!TORNotificationHelper.AreKingdomsRelevantToPlayer(kingdom1, kingdom2)) return;
            TextObject message = new TextObject("{=hymvJALX}The {KINGDOM_1} and the {KINGDOM_2} have formed an alliance.");
            message.SetTextVariable("KINGDOM_1", kingdom1.Name);
            message.SetTextVariable("KINGDOM_2", kingdom2.Name);
            MBInformationManager.AddQuickInformation(message, 5000);
        }

        private void OnAllianceEnded(Kingdom kingdom1, Kingdom kingdom2)
        {
            if (!TORNotificationHelper.AreKingdomsRelevantToPlayer(kingdom1, kingdom2)) return;
            TextObject message = new TextObject("{=KC8CqquX}The alliance between the {KINGDOM_1} and the {KINGDOM_2} is dissolved.");
            message.SetTextVariable("KINGDOM_1", kingdom1.Name);
            message.SetTextVariable("KINGDOM_2", kingdom2.Name);
            MBInformationManager.AddQuickInformation(message, 5000);
        }

        private void OnCallToWarAgreementStarted(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
        {
            if (!TORNotificationHelper.AreKingdomsRelevantToPlayer(callingKingdom, calledKingdom, kingdomToCallToWarAgainst)) return;

            if (Clan.PlayerClan.IsUnderMercenaryService && callingKingdom.Clans.Contains(Clan.PlayerClan))
            {
                TextObject message = new TextObject("{=zVmDmLCW}Your realm called the {CALLED_KINGDOM} to war against the {KINGDOM_TO_CALL_TO_WAR_AGAINST}.");
                message.SetTextVariable("CALLED_KINGDOM", calledKingdom.Name);
                message.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
                MBInformationManager.AddQuickInformation(message, 5000);
            }
            if (Clan.PlayerClan.IsUnderMercenaryService && calledKingdom.Clans.Contains(Clan.PlayerClan))
            {
                TextObject message1 = new TextObject("{=2ihQeId5}The {CALLING_KINGDOM} has called your realm to war against the {KINGDOM_TO_CALL_TO_WAR_AGAINST}.");
                message1.SetTextVariable("CALLING_KINGDOM", callingKingdom.Name);
                message1.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
                MBInformationManager.AddQuickInformation(message1, 5000);
            }
        }

        private void OnCallToWarAgreementEnded(Kingdom callingKingdom, Kingdom calledKingdom, Kingdom kingdomToCallToWarAgainst)
        {
            if (!TORNotificationHelper.AreKingdomsRelevantToPlayer(callingKingdom, calledKingdom, kingdomToCallToWarAgainst)) return;

            if (callingKingdom.Clans.Contains(Clan.PlayerClan))
            {
                TextObject message = new TextObject("{=ocNWAQsu}The call that your realm issued to the {CALLED_KINGDOM}, to go to war against the {KINGDOM_TO_CALL_TO_WAR_AGAINST}, has ended.");
                message.SetTextVariable("CALLED_KINGDOM", calledKingdom.Name);
                message.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
                MBInformationManager.AddQuickInformation(message, 5000);
            }
            if (calledKingdom.Clans.Contains(Clan.PlayerClan))
            {
                TextObject message1 = new TextObject("{=6eHPy57Z}The call that the {CALLING_KINGDOM} issued to your realm, to go to war against the {KINGDOM_TO_CALL_TO_WAR_AGAINST}, has ended.");
                message1.SetTextVariable("CALLING_KINGDOM", callingKingdom.Name);
                message1.SetTextVariable("KINGDOM_TO_CALL_TO_WAR_AGAINST", kingdomToCallToWarAgainst.Name);
                MBInformationManager.AddQuickInformation(message1, 5000);
            }
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero hero)
        {
            if (mobileParty == null || !mobileParty.IsTargetingPort || settlement.SiegeEvent == null || settlement.SiegeEvent.IsBlockadeActive || !settlement.SiegeEvent.BesiegerCamp.IsBesiegerSideParty(MobileParty.MainParty))
                return;
            TextObject message = new TextObject("{=dPE4A7fO}{ENEMY_PARTY_NAME} has entered the town with {MAN_COUNT} men from the port.");
            message.SetTextVariable("ENEMY_PARTY_NAME", mobileParty.Name);
            message.SetTextVariable("MAN_COUNT", mobileParty.MemberRoster.TotalManCount);
            MBInformationManager.AddQuickInformation(message);
        }

        private void OnPartyAddedToMapEvent(PartyBase involvedParty)
        {
            if (involvedParty.LeaderHero == null || involvedParty.LeaderHero.Clan != Clan.PlayerClan || involvedParty == PartyBase.MainParty || involvedParty.Side != BattleSideEnum.Defender || involvedParty.MapEvent.AttackerSide.LeaderParty == null)
                return;
            bool flag = false;
            foreach (PartyBase involvedParty1 in involvedParty.MapEvent.InvolvedParties)
            {
                if (involvedParty1 == PartyBase.MainParty)
                {
                    flag = true;
                    break;
                }
            }
            if (flag)
                return;
            Settlement settlement = Hero.MainHero.HomeSettlement;
            float num1 = Campaign.MapDiagonalSquared;
            foreach (Settlement toSettlement in Settlement.All)
            {
                if (toSettlement.IsVillage || toSettlement.IsFortification)
                {
                    float num2 = involvedParty.IsMobile ? Campaign.Current.Models.MapDistanceModel.GetDistance(involvedParty.MobileParty, toSettlement, false, MobileParty.NavigationType.All, out float _) : Campaign.Current.Models.MapDistanceModel.GetDistance(involvedParty.Settlement, toSettlement, false, false, MobileParty.NavigationType.All);
                    if (num2 < num1)
                    {
                        num1 = num2;
                        settlement = toSettlement;
                    }
                }
            }
            if (settlement == null)
                return;
            TextObject text = GameTexts.FindText("str_party_attacked");
            text.SetTextVariable("CLAN_PARTY_NAME", involvedParty.Name);
            text.SetTextVariable("ENEMY_PARTY_NAME", involvedParty.MapEvent.AttackerSide.LeaderParty.Name);
            text.SetTextVariable("SETTLEMENT_NAME", settlement.Name);
            MBInformationManager.AddQuickInformation(text);
        }

        private void OnCompanionRemoved(Hero hero, RemoveCompanionAction.RemoveCompanionDetail detail)
        {
            if (detail == RemoveCompanionAction.RemoveCompanionDetail.ByTurningToLord)
            {
                TextObject textObject = new TextObject("{=2Lj0WkSF}{COMPANION.NAME} is now a {?COMPANION.GENDER}noblewoman{?}lord{\\?} of the {KINGDOM}.");
                textObject.SetCharacterProperties("COMPANION", hero.CharacterObject);
                textObject.SetTextVariable("KINGDOM", Clan.PlayerClan.Kingdom.Name);
                MBInformationManager.AddQuickInformation(textObject, soundEventPath: "event:/ui/notification/relation");
            }
            if (detail != RemoveCompanionAction.RemoveCompanionDetail.Fire)
                return;
            TextObject textObject1 = new TextObject("{=4zdyeTGn}{COMPANION.NAME} left your clan.");
            textObject1.SetCharacterProperties("COMPANION", hero.CharacterObject);
            MBInformationManager.AddQuickInformation(textObject1, soundEventPath: "event:/ui/notification/relation");
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_foodNotificationList", ref _foodNotificationList);
        }

        private void OnHourlyTick()
        {
            if (MobileParty.MainParty.Army == null)
                return;
            CheckFoodNotifications();
        }

        private void OnIssueUpdated(IssueBase issue, IssueBase.IssueUpdateDetails details, Hero issueSolver)
        {
            switch (details)
            {
                case IssueBase.IssueUpdateDetails.PlayerSentTroopsToQuest:
                    TextObject text1 = GameTexts.FindText("str_issue_updated", details.ToString());
                    text1.SetTextVariable("ISSUE_NAME", issue.Title);
                    MBInformationManager.AddQuickInformation(text1, soundEventPath: "event:/ui/notification/quest_start");
                    break;
                case IssueBase.IssueUpdateDetails.SentTroopsFinishedQuest:
                    TextObject text2 = GameTexts.FindText("str_issue_updated", details.ToString());
                    text2.SetTextVariable("ISSUE_NAME", issue.Title);
                    MBInformationManager.AddQuickInformation(text2, soundEventPath: "event:/ui/notification/quest_finished");
                    break;
                case IssueBase.IssueUpdateDetails.SentTroopsFailedQuest:
                    TextObject text3 = GameTexts.FindText("str_issue_updated", details.ToString());
                    text3.SetTextVariable("ISSUE_NAME", issue.Title);
                    MBInformationManager.AddQuickInformation(text3, soundEventPath: "event:/ui/notification/quest_fail");
                    break;
            }
        }

        private void OnQuestLogAdded(QuestBase quest, bool hideInformation)
        {
            if (hideInformation)
                return;
            TextObject text = GameTexts.FindText("str_quest_log_added");
            text.SetTextVariable("TITLE", quest.Title);
            MBInformationManager.AddQuickInformation(text, soundEventPath: "event:/ui/notification/quest_update");
        }

        private void OnQuestCompleted(QuestBase quest, QuestBase.QuestCompleteDetails detail)
        {
            switch (detail)
            {
                case QuestBase.QuestCompleteDetails.Success:
                    TextObject text1 = GameTexts.FindText("str_quest_completed", detail.ToString());
                    text1.SetTextVariable("QUEST_TITLE", quest.Title);
                    MBInformationManager.AddQuickInformation(text1, soundEventPath: "event:/ui/notification/quest_finished");
                    break;
                case QuestBase.QuestCompleteDetails.Cancel:
                case QuestBase.QuestCompleteDetails.Fail:
                case QuestBase.QuestCompleteDetails.Timeout:
                case QuestBase.QuestCompleteDetails.FailWithBetrayal:
                    TextObject text2 = GameTexts.FindText("str_quest_completed", detail.ToString());
                    text2.SetTextVariable("QUEST_TITLE", quest.Title);
                    MBInformationManager.AddQuickInformation(text2, soundEventPath: "event:/ui/notification/quest_fail");
                    break;
            }
        }

        private void OnQuestStarted(QuestBase quest)
        {
            TextObject text = GameTexts.FindText("str_quest_started");
            text.SetTextVariable("QUEST_TITLE", quest.Title);
            MBInformationManager.AddQuickInformation(text, soundEventPath: "event:/ui/notification/quest_start");
        }

        private void OnRenownGained(Hero hero, int gainedRenown, bool doNotNotifyPlayer)
        {
            if (hero.Clan != Clan.PlayerClan || doNotNotifyPlayer)
                return;
            TextObject text;
            if (hero.PartyBelongedTo != null)
            {
                text = GameTexts.FindText("str_party_gained_renown");
                text.SetTextVariable("PARTY", hero.PartyBelongedTo.Name);
            }
            else
                text = GameTexts.FindText("str_clan_gained_renown");
            text.SetTextVariable("NEW_RENOWN", $"{hero.Clan.Renown:0.#}");
            text.SetTextVariable("AMOUNT_TO_ADD", gainedRenown);
            text.SetTextVariable("CLAN", hero.Clan.Name);
            MBInformationManager.AddQuickInformation(text);
        }

        private void OnHideoutSpotted(PartyBase party, PartyBase hideoutParty)
        {
            if (party != PartyBase.MainParty)
                return;
            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_hideout_spotted").ToString(), new Color(1f, 0.0f, 0.0f)));
        }

        private void OnHeroBecameFugitive(Hero hero, bool showNotification)
        {
            if (!showNotification) return;
            if (!TORNotificationHelper.IsHeroRelevantToPlayer(hero)) return;
            TextObject text = GameTexts.FindText("str_fugitive_news");
            text.SetCharacterProperties("HERO", hero.CharacterObject);
            MBInformationManager.AddQuickInformation(text);
        }

        private void OnPrisonerTaken(PartyBase capturer, Hero prisoner)
        {
            if (!TORNotificationHelper.IsHeroRelevantToPlayer(prisoner)) return;
            TextObject text1 = GameTexts.FindText("str_on_prisoner_taken");
            if (capturer.IsSettlement && capturer.Settlement.IsTown)
            {
                TextObject text2 = GameTexts.FindText("str_garrison_party_name");
                text2.SetTextVariable("MAJOR_PARTY_LEADER", capturer.Settlement.Name);
                text1.SetTextVariable("CAPTOR_NAME", text2);
            }
            else
                text1.SetTextVariable("CAPTOR_NAME", capturer.Name);
            StringHelpers.SetCharacterProperties("PRISONER", prisoner.CharacterObject, text1);
            MBInformationManager.AddQuickInformation(text1);
        }

        private void OnHeroPrisonerReleased(Hero hero, PartyBase party, IFaction capturerFaction, EndCaptivityDetail detail, bool showNotification)
        {
            TextObject textObject = null;
            if (!showNotification)
                return;
            if (!TORNotificationHelper.IsHeroRelevantToPlayer(hero)) return;

            if (hero.Clan == Clan.PlayerClan)
            {
                switch (detail)
                {
                    case EndCaptivityDetail.Ransom:
                    case EndCaptivityDetail.ReleasedAfterPeace:
                    case EndCaptivityDetail.ReleasedAfterBattle:
                    case EndCaptivityDetail.ReleasedAfterEscape:
                        textObject = GameTexts.FindText("str_on_prisoner_released_main_clan", detail.ToString());
                        break;
                    default:
                        textObject = GameTexts.FindText("str_on_prisoner_released_main_clan_default");
                        break;
                }
            }
            else if (party != null && party.IsSettlement && party.Settlement.IsFortification && party.Settlement.OwnerClan == Clan.PlayerClan)
            {
                if (detail == EndCaptivityDetail.ReleasedAfterEscape)
                {
                    textObject = GameTexts.FindText("str_on_prisoner_released_escaped_from_settlement");
                    textObject.SetTextVariable("SETTLEMENT", party.Settlement.Name);
                }
            }
            else if (party != null && party.IsMobile && party.MobileParty == MobileParty.MainParty && detail == EndCaptivityDetail.ReleasedAfterEscape)
                textObject = GameTexts.FindText("str_on_prisoner_released_escaped_from_party");
            if (textObject == null)
                return;
            StringHelpers.SetCharacterProperties("PRISONER", hero.CharacterObject, textObject);
            MBInformationManager.AddQuickInformation(textObject);
        }

        private void OnBattleStarted(PartyBase attackerParty, PartyBase defenderParty, object subject, bool showNotification)
        {
            Settlement settlement;
            if (!showNotification || (settlement = subject as Settlement) == null || settlement.OwnerClan != Clan.PlayerClan || defenderParty.MapEvent == null || defenderParty.MapEvent.DefenderSide.Parties.FindIndexQ(p => p.Party == settlement.Party) < 0)
                return;
            MBTextManager.SetTextVariable("PARTY", attackerParty.MobileParty.Army != null ? attackerParty.MobileParty.ArmyName : attackerParty.Name);
            MBTextManager.SetTextVariable("FACTION", attackerParty.MapFaction.Name);
            MBTextManager.SetTextVariable("SETTLEMENT", settlement.EncyclopediaLinkWithName);
            MBInformationManager.AddQuickInformation(new TextObject("{=ASOW1MuQ}Your settlement {SETTLEMENT} is under attack by {PARTY} of {FACTION}!"));
        }

        private void OnSiegeEventStarted(SiegeEvent siegeEvent)
        {
            if (siegeEvent.BesiegedSettlement == null || siegeEvent.BesiegedSettlement.OwnerClan != Clan.PlayerClan || siegeEvent.BesiegerCamp.LeaderParty == null)
                return;
            MBTextManager.SetTextVariable("PARTY", siegeEvent.BesiegerCamp.LeaderParty.Army != null ? siegeEvent.BesiegerCamp.LeaderParty.ArmyName : siegeEvent.BesiegerCamp.LeaderParty.Name);
            MBTextManager.SetTextVariable("FACTION", siegeEvent.BesiegerCamp.MapFaction.Name);
            MBTextManager.SetTextVariable("SETTLEMENT", siegeEvent.BesiegedSettlement.EncyclopediaLinkWithName);
            MBInformationManager.AddQuickInformation(new TextObject("{=3FvGk8k6}Your settlement {SETTLEMENT} is besieged by {PARTY} of {FACTION}!"));
        }

        private void OnClanTierIncreased(Clan clan, bool shouldNotify = true)
        {
            if (!shouldNotify || clan != Clan.PlayerClan)
                return;
            MBTextManager.SetTextVariable("CLAN", clan.Name);
            MBTextManager.SetTextVariable("TIER_LEVEL", clan.Tier);
            MBInformationManager.AddQuickInformation(new TextObject("{=No04urXt}{CLAN} tier is increased to {TIER_LEVEL}"));
        }

        private void OnItemsLooted(MobileParty mobileParty, ItemRoster items)
        {
            if (mobileParty != MobileParty.MainParty)
                return;
            bool flag = true;
            for (int index = 0; index < items.Count; ++index)
            {
                ItemRosterElement elementCopyAtIndex = items.GetElementCopyAtIndex(index);
                MBTextManager.SetTextVariable("NUMBER_OF", items.GetElementNumber(index));
                MBTextManager.SetTextVariable("ITEM", elementCopyAtIndex.EquipmentElement.Item.Name);
                if (flag)
                {
                    MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_number_of_item").ToString());
                    flag = false;
                }
                else
                {
                    MBTextManager.SetTextVariable("RIGHT", GameTexts.FindText("str_number_of_item").ToString());
                    MBTextManager.SetTextVariable("LEFT", GameTexts.FindText("str_LEFT_comma_RIGHT").ToString());
                }
            }
            MBTextManager.SetTextVariable("PRODUCTS", GameTexts.FindText("str_LEFT_ONLY").ToString());
            InformationManager.DisplayMessage(new InformationMessage(new TextObject("{=GW8ITTMb}You plundered {PRODUCTS}.").ToString()));
        }

        private void OnRelationChanged(Hero effectiveHero, Hero effectiveHeroGainedRelationWith, int relationChange, bool showNotification, ChangeRelationAction.ChangeRelationDetail detail, Hero originalHero, Hero originalGainedRelationWith)
        {
            if (!showNotification || relationChange == 0 || effectiveHero != Hero.MainHero && effectiveHeroGainedRelationWith != Hero.MainHero)
                return;
            Hero hero = effectiveHero.IsHumanPlayerCharacter ? effectiveHeroGainedRelationWith : effectiveHero;
            TextObject textObject;
            if (hero.Clan == null || hero.Clan == Clan.PlayerClan)
            {
                textObject = relationChange > 0 ? GameTexts.FindText("str_your_relation_increased_with_notable") : GameTexts.FindText("str_your_relation_decreased_with_notable");
                StringHelpers.SetCharacterProperties("HERO", hero.CharacterObject, textObject);
            }
            else
            {
                textObject = relationChange > 0 ? GameTexts.FindText("str_your_relation_increased_with_clan") : GameTexts.FindText("str_your_relation_decreased_with_clan");
                textObject.SetTextVariable("CLAN_LEADER", hero.Clan.Name);
            }
            textObject.SetTextVariable("VALUE", hero.GetRelation(Hero.MainHero));
            textObject.SetTextVariable("MAGNITUDE", MathF.Abs(relationChange));
            MBInformationManager.AddQuickInformation(textObject, announcerCharacter: hero.IsNotable ? hero.CharacterObject : null, soundEventPath: "event:/ui/notification/relation");
        }

        private void OnHeroLevelledUp(Hero hero, bool shouldNotify)
        {
            if (!shouldNotify || hero != Hero.MainHero && hero.Clan != Clan.PlayerClan)
                return;
            TextObject textObject = new TextObject("{=3wzCrzEq}{HERO.NAME} gained a level.");
            StringHelpers.SetCharacterProperties("HERO", hero.CharacterObject, textObject);
            MBInformationManager.AddQuickInformation(textObject, soundEventPath: "event:/ui/notification/levelup");
        }

        private void OnHeroGainedSkill(Hero hero, SkillObject skill, int change = 1, bool shouldNotify = true)
        {
            if (!shouldNotify || !BannerlordConfig.ReportExperience || hero != Hero.MainHero && hero.Clan != Clan.PlayerClan && hero.PartyBelongedTo != MobileParty.MainParty && (hero.CompanionOf == null || hero.CompanionOf != Clan.PlayerClan))
                return;
            TextObject text = GameTexts.FindText("str_skill_gained_notification");
            StringHelpers.SetCharacterProperties("HERO", hero.CharacterObject, text);
            text.SetTextVariable("PLURAL", change > 1 ? 1 : 0);
            text.SetTextVariable("GAINED_POINTS", change);
            text.SetTextVariable("SKILL_NAME", skill.Name);
            text.SetTextVariable("UPDATED_SKILL_LEVEL", hero.GetSkillValue(skill));
            InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
        }

        private void OnTroopsDeserted(MobileParty mobileParty, TroopRoster desertedTroops)
        {
            if (mobileParty != MobileParty.MainParty && mobileParty.Party.Owner != Hero.MainHero)
                return;
            TextObject text = GameTexts.FindText("str_troops_deserting");
            text.SetTextVariable("PARTY", mobileParty.Name);
            text.SetTextVariable("DESERTER_COUNT", desertedTroops.TotalManCount);
            text.SetTextVariable("PLURAL", desertedTroops.TotalManCount == 1 ? 0 : 1);
            MBInformationManager.AddQuickInformation(text);
        }

        private void OnClanChangedFaction(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification = true)
        {
            if (clan != Clan.PlayerClan && Hero.MainHero.MapFaction != oldKingdom && Hero.MainHero.MapFaction != newKingdom)
                return;
            if (detail == ChangeKingdomAction.ChangeKingdomActionDetail.JoinAsMercenary || detail == ChangeKingdomAction.ChangeKingdomActionDetail.LeaveAsMercenary)
            {
                OnMercenaryClanChangedKingdom(clan, oldKingdom, newKingdom);
            }
            else
            {
                if (!showNotification)
                    return;
                OnRegularClanChangedKingdom(clan, oldKingdom, newKingdom);
            }
        }

        private void OnRegularClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
        {
            TextObject message = oldKingdom == null || newKingdom != null ? (oldKingdom != null || newKingdom == null ? (oldKingdom == null || newKingdom == null || oldKingdom == newKingdom ? (oldKingdom == null || oldKingdom != newKingdom || clan.IsUnderMercenaryService ? null : new TextObject("{=6f9Hs5zp}The {CLAN_NAME} ended its mercenary contract and became a vassal of the {NEW_FACTION_NAME}")) : new TextObject("{=HlrGpPkV}The {CLAN_NAME} changed from the {OLD_FACTION_NAME} to the {NEW_FACTION_NAME}.")) : new TextObject("{=qeiVFn9s}The {CLAN_NAME} joined the {NEW_FACTION_NAME}")) : new TextObject("{=WNKkdpN3}The {CLAN_NAME} left the {OLD_FACTION_NAME}.");
            if (TextObject.IsNullOrEmpty(message))
                return;
            message.SetTextVariable("CLAN_NAME", clan.AliveLords.Count == 1 ? clan.AliveLords[0].Name : clan.Name);
            if (oldKingdom != null)
                message.SetTextVariable("OLD_FACTION_NAME", oldKingdom.InformalName);
            if (newKingdom != null)
                message.SetTextVariable("NEW_FACTION_NAME", newKingdom.InformalName);
            MBInformationManager.AddQuickInformation(message);
        }

        private void OnMercenaryClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom)
        {
            if (clan != Clan.PlayerClan && Hero.MainHero.MapFaction != oldKingdom && Hero.MainHero.MapFaction != newKingdom)
                return;
            if (oldKingdom != null && (clan == Hero.MainHero.Clan || oldKingdom == Hero.MainHero.MapFaction))
            {
                TextObject message = clan.IsUnderMercenaryService ? new TextObject("{=a2AO5T1Q}The {CLAN_NAME} and the {KINGDOM_NAME} have ended their mercenary contract.") : new TextObject("{=g7qhnsnJ}The {CLAN_NAME} clan has left the {KINGDOM_NAME}.");
                message.SetTextVariable("CLAN_NAME", clan.Name);
                message.SetTextVariable("KINGDOM_NAME", oldKingdom.InformalName);
                MBInformationManager.AddQuickInformation(message);
            }
            if (newKingdom == null || clan != Hero.MainHero.Clan && newKingdom != Hero.MainHero.MapFaction || !clan.IsUnderMercenaryService)
                return;
            TextObject message1 = new TextObject("{=AozaGCru}The {CLAN_NAME} and the {KINGDOM_NAME} have signed a mercenary contract.");
            message1.SetTextVariable("CLAN_NAME", clan.Name);
            message1.SetTextVariable("KINGDOM_NAME", newKingdom.InformalName);
            MBInformationManager.AddQuickInformation(message1);
        }

        private void OnArmyCreated(Army army)
        {
            if (army.Kingdom != MobileParty.MainParty.MapFaction || MobileParty.MainParty.Army != null)
                return;
            TextObject textObject = new TextObject("{=VEHPTzhO}{LEADER.NAME} is gathering an army near {SETTLEMENT}.");
            textObject.SetTextVariable("SETTLEMENT", army.AiBehaviorObject.Name);
            StringHelpers.SetCharacterProperties("LEADER", army.LeaderParty.LeaderHero.CharacterObject, textObject);
            MBInformationManager.AddQuickInformation(textObject, announcerCharacter: army.LeaderParty.LeaderHero.CharacterObject);
        }

        private void OnPlayerArmyLeaderChangedBehaviorEvent()
        {
            MBInformationManager.AddQuickInformation(GameTexts.FindText("str_army_leader_think", "Unknown"), announcerCharacter: MobileParty.MainParty.Army.LeaderParty.LeaderHero.CharacterObject);
        }

        private void OnSiegeBombardmentHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, SiegeBombardTargets target)
        {
            if ((besiegerParty.Army == null || !besiegerParty.Army.Parties.Contains(MobileParty.MainParty)) && besiegerParty != MobileParty.MainParty && (MobileParty.MainParty.CurrentSettlement == null || MobileParty.MainParty.CurrentSettlement != besiegedSettlement))
                return;
            TextObject textObject;
            switch (target)
            {
                case SiegeBombardTargets.RangedEngines:
                    textObject = side == BattleSideEnum.Defender ? new TextObject("{=gqdsXVNi}{WEAPON} of {SETTLEMENT} hit ranged engines of {BESIEGER}!") : new TextObject("{=FnkYfyGa}the {WEAPON} of {BESIEGER} hit ranged engines of {SETTLEMENT}!");
                    break;
                case SiegeBombardTargets.People:
                    textObject = side == BattleSideEnum.Defender ? new TextObject("{=7WlQ0Twr}{WEAPON} of {SETTLEMENT} hit some soldiers of {BESIEGER}!") : new TextObject("{=ZrMeSyPu}The {WEAPON} of {BESIEGER} hit some soldiers in {SETTLEMENT}!");
                    break;
                default:
                    textObject = null;
                    break;
            }
            if (TextObject.IsNullOrEmpty(textObject))
                return;
            textObject.SetTextVariable("WEAPON", weapon.Name);
            textObject.SetTextVariable("BESIEGER", besiegerParty.Army != null ? besiegerParty.Army.Name : besiegerParty.Name);
            textObject.SetTextVariable("SETTLEMENT", besiegedSettlement.Name);
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
        }

        private void OnSiegeBombardmentWallHit(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType weapon, bool isWallCracked)
        {
            if ((besiegerParty.Army == null || !besiegerParty.Army.Parties.Contains(MobileParty.MainParty)) && besiegerParty != MobileParty.MainParty && (MobileParty.MainParty.CurrentSettlement == null || MobileParty.MainParty.CurrentSettlement != besiegedSettlement))
                return;
            TextObject textObject = new TextObject("{=8Wy1OCsr}The {WEAPON} of {BESIEGER} hit wall of {SETTLEMENT}!");
            textObject.SetTextVariable("WEAPON", weapon.Name);
            textObject.SetTextVariable("BESIEGER", besiegerParty.Army != null ? besiegerParty.Army.Name : besiegerParty.Name);
            textObject.SetTextVariable("SETTLEMENT", besiegedSettlement.Name);
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
            if (!isWallCracked)
                return;
            TextObject message = new TextObject("{=uJNvbag5}The walls of {SETTLEMENT} has been cracked.");
            message.SetTextVariable("SETTLEMENT", besiegedSettlement.Name);
            MBInformationManager.AddQuickInformation(message);
        }

        private void OnSiegeEngineDestroyed(MobileParty besiegerParty, Settlement besiegedSettlement, BattleSideEnum side, SiegeEngineType destroyedEngine)
        {
            if ((besiegerParty.Army == null || !besiegerParty.Army.Parties.Contains(MobileParty.MainParty)) && besiegerParty != MobileParty.MainParty && (MobileParty.MainParty.CurrentSettlement == null || MobileParty.MainParty.CurrentSettlement != besiegedSettlement))
                return;
            TextObject message = side == BattleSideEnum.Attacker ? new TextObject("{=fa8sla4i}The {SIEGE_ENGINE} of {BESIEGER_PARTY} has been destroyed.") : new TextObject("{=U9zFz8Et}The {SIEGE_ENGINE} of {SIEGED_SETTLEMENT_NAME} has been cracked.");
            message.SetTextVariable("SIEGED_SETTLEMENT_NAME", besiegedSettlement.Name);
            message.SetTextVariable("BESIEGER_PARTY", besiegerParty.Name);
            message.SetTextVariable("SIEGE_ENGINE", destroyedEngine.Name);
            MBInformationManager.AddQuickInformation(message);
        }

        private void OnHeroOrPartyTradedGold((Hero, PartyBase) giverSide, (Hero, PartyBase) recipientSide, (int, string) transactionAmountAndId, bool displayNotification)
        {
            if (!displayNotification)
                return;
            int f = transactionAmountAndId.Item1;
            MBTextManager.SetTextVariable("GOLD_AMOUNT", MathF.Abs(f));
            bool flag1 = giverSide.Item1 == Hero.MainHero || giverSide.Item2 == PartyBase.MainParty;
            bool flag2 = recipientSide.Item1 == Hero.MainHero || recipientSide.Item2 == PartyBase.MainParty;
            if (flag1 && f > 0 || flag2 && f < 0)
            {
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_gold_removed_with_icon").ToString(), "event:/ui/notification/coins_negative"));
            }
            else
            {
                if ((!flag1 || f >= 0) && (!flag2 || f <= 0))
                    return;
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText("str_you_received_gold_with_icon").ToString(), "event:/ui/notification/coins_positive"));
            }
        }

        private void OnPartyJoinedArmy(MobileParty party)
        {
            if (party.Army != MobileParty.MainParty.Army || party.LeaderHero == party.Army.LeaderParty.LeaderHero)
                return;
            TextObject textObject = new TextObject("{=wD1YDmmg}{PARTY_NAME} has enlisted in {ARMY_NAME}.");
            textObject.SetTextVariable("PARTY_NAME", party.Name);
            textObject.SetTextVariable("ARMY_NAME", party.Army.Name);
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
        }

        private void OnPartyRemovedFromArmy(MobileParty party)
        {
            if (party.Army == MobileParty.MainParty.Army)
            {
                TextObject textObject = new TextObject("{=ApG1xg7O}{PARTY_NAME} has left {ARMY_NAME}.");
                textObject.SetTextVariable("PARTY_NAME", party.Name);
                textObject.SetTextVariable("ARMY_NAME", party.Army.Name);
                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
            }
            if (party != MobileParty.MainParty)
                return;
            CheckFoodNotifications();
        }

        private void OnArmyDispersed(Army army, Army.ArmyDispersionReason reason, bool isPlayersArmy)
        {
            if (!isPlayersArmy)
                return;
            CheckFoodNotifications();
        }

        private void OnHeroesMarried(Hero firstHero, Hero secondHero, bool showNotification)
        {
            if (!showNotification) return;
            if (!TORNotificationHelper.IsHeroRelevantToPlayer(firstHero) && !TORNotificationHelper.IsHeroRelevantToPlayer(secondHero)) return;
            StringHelpers.SetCharacterProperties("MARRIED_TO", firstHero.CharacterObject);
            StringHelpers.SetCharacterProperties("MARRIED_HERO", secondHero.CharacterObject);
            MBInformationManager.AddQuickInformation(GameTexts.FindText("str_hero_married_hero"));
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool openToClaim, Hero newOwner, Hero previousOwner, Hero capturerHero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege || settlement.MapFaction != Hero.MainHero.MapFaction || !settlement.IsFortification)
                return;
            TextObject message = Hero.MainHero.MapFaction.IsKingdomFaction ? new TextObject("{=OiCCfAeC}{SETTLEMENT} is taken. Election is started.") : new TextObject("{=2VRTPyZY}{SETTLEMENT} is yours.");
            message.SetTextVariable("SETTLEMENT", settlement.Name);
            MBInformationManager.AddQuickInformation(message);
        }

        private void OnChildConceived(Hero mother)
        {
            if (mother == Hero.MainHero)
                MBInformationManager.AddQuickInformation(new TextObject("{=ZhpT2qVh}You have just learned that you are with child."));
            else if (mother == Hero.MainHero.Spouse)
            {
                TextObject message = new TextObject("{=7v2dMsW5}Your spouse {MOTHER} has just learned that she is with child.");
                message.SetTextVariable("MOTHER", mother.Name);
                MBInformationManager.AddQuickInformation(message);
            }
            else
            {
                if (mother.Clan != Clan.PlayerClan)
                    return;
                TextObject message = new TextObject("{=2AGIxoUN}Your clan member {MOTHER} has just learned that she is with child.");
                message.SetTextVariable("MOTHER", mother.Name);
                MBInformationManager.AddQuickInformation(message);
            }
        }

        private void OnGivenBirth(Hero mother, List<Hero> aliveOffsprings, int stillbornCount)
        {
            if (mother != Hero.MainHero && mother != Hero.MainHero.Spouse && mother.Clan != Clan.PlayerClan)
                return;
            TextObject textObject = mother != Hero.MainHero ? (mother != Hero.MainHero.Spouse ? new TextObject("{=LsDRCPp0}Your clan member {MOTHER.NAME} has given birth to {DELIVERED_CHILDREN}.") : new TextObject("{=TsbjAsxs}Your wife {MOTHER.NAME} has given birth to {DELIVERED_CHILDREN}.")) : new TextObject("{=oIA9lkpc}You have given birth to {DELIVERED_CHILDREN}.");
            if (stillbornCount == 2)
                textObject.SetTextVariable("DELIVERED_CHILDREN", new TextObject("{=Sn9a1Aba}two stillborn babies"));
            else if (stillbornCount == 1 && aliveOffsprings.Count == 0)
                textObject.SetTextVariable("DELIVERED_CHILDREN", new TextObject("{=qWLq2y84}a stillborn baby"));
            else if (stillbornCount == 1 && aliveOffsprings.Count == 1)
                textObject.SetTextVariable("DELIVERED_CHILDREN", new TextObject("{=vn13OyFV}one healthy and one stillborn baby"));
            else if (stillbornCount == 0 && aliveOffsprings.Count == 1)
                textObject.SetTextVariable("DELIVERED_CHILDREN", new TextObject("{=lbRMmZym}a healthy baby"));
            else if (stillbornCount == 0 && aliveOffsprings.Count == 2)
                textObject.SetTextVariable("DELIVERED_CHILDREN", new TextObject("{=EPbHr2DX}two healthy babies"));
            StringHelpers.SetCharacterProperties("MOTHER", mother.CharacterObject, textObject);
            MBInformationManager.AddQuickInformation(textObject);
        }

        private void OnHeroKilled(Hero victimHero, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
        {
            if (!showNotification || victimHero == null) return;
            if (!TORNotificationHelper.IsHeroRelevantToPlayer(victimHero)) return;
            MBInformationManager.AddQuickInformation(CharacterHelper.GetDeathNotification(victimHero, killer, detail));
        }

        private void OnHeroSharedFoodWithAnotherHero(Hero supporterHero, Hero supportedHero, float influence)
        {
            if (supporterHero == Hero.MainHero)
            {
                _foodNotificationList.Add(new Tuple<bool, float>(true, influence));
            }
            else
            {
                if (supportedHero != Hero.MainHero)
                    return;
                _foodNotificationList.Add(new Tuple<bool, float>(false, influence));
            }
        }

        private void CheckFoodNotifications()
        {
            float num1 = 0.0f;
            float num2 = 0.0f;
            bool flag1 = false;
            bool flag2 = false;
            foreach (Tuple<bool, float> foodNotification in _foodNotificationList)
            {
                if (foodNotification.Item1)
                {
                    num1 += foodNotification.Item2;
                    flag1 = true;
                }
                else
                {
                    num2 += foodNotification.Item2;
                    flag2 = true;
                }
            }
            if (flag1)
            {
                TextObject textObject = new TextObject("{=B0eBWPoO} You shared your food with starving soldiers of your army. You gained {INFLUENCE}{INFLUENCE_ICON}.");
                textObject.SetTextVariable("INFLUENCE", num1.ToString("0.00"));
                textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
            }
            if (flag2)
            {
                TextObject textObject = new TextObject("{=qQ71Ux7D} Your army shared their food with your starving soldiers. You spent {INFLUENCE}{INFLUENCE_ICON}.");
                textObject.SetTextVariable("INFLUENCE", num2.ToString("0.00"));
                textObject.SetTextVariable("INFLUENCE_ICON", "{=!}<img src=\"General\\Icons\\Influence@2x\" extend=\"7\">");
                InformationManager.DisplayMessage(new InformationMessage(textObject.ToString()));
            }
            _foodNotificationList.Clear();
        }

        private void OnClanDestroyed(Clan destroyedClan)
        {
            if (destroyedClan.Kingdom != null && !TORNotificationHelper.IsKingdomRelevantToPlayer(destroyedClan.Kingdom)) return;
            TextObject message = new TextObject("{=PBq1FyrJ}{CLAN_NAME} clan was destroyed.");
            message.SetTextVariable("CLAN_NAME", destroyedClan.Name);
            MBInformationManager.AddQuickInformation(message);
        }

        private void OnHeroOrPartyGaveItem((Hero, PartyBase) giver, (Hero, PartyBase) receiver, ItemRosterElement itemRosterElement, bool showNotification)
        {
            if (!showNotification || itemRosterElement.Amount <= 0)
                return;
            TextObject parent = null;
            if (giver.Item1 == Hero.MainHero || giver.Item2 == PartyBase.MainParty)
            {
                if (receiver.Item1 != null)
                {
                    parent = GameTexts.FindText("str_hero_gave_item_to_hero");
                    StringHelpers.SetCharacterProperties("HERO", receiver.Item1.CharacterObject, parent);
                }
                else
                {
                    parent = GameTexts.FindText("str_hero_gave_item_to_party");
                    parent.SetTextVariable("PARTY_NAME", receiver.Item2.Name);
                }
            }
            else if (receiver.Item1 == Hero.MainHero || receiver.Item2 == PartyBase.MainParty)
            {
                if (giver.Item1 != null)
                {
                    parent = GameTexts.FindText("str_hero_received_item_from_hero");
                    StringHelpers.SetCharacterProperties("HERO", giver.Item1.CharacterObject, parent);
                }
                else
                {
                    parent = GameTexts.FindText("str_hero_received_item_from_party");
                    parent.SetTextVariable("PARTY_NAME", giver.Item2.Name);
                }
            }
            if (parent == null)
                return;
            parent.SetTextVariable("ITEM", itemRosterElement.EquipmentElement.Item.Name);
            parent.SetTextVariable("COUNT", itemRosterElement.Amount);
            InformationManager.DisplayMessage(new InformationMessage(parent.ToString()));
        }

        private void OnRebellionFinished(Settlement settlement, Clan oldOwnerClan)
        {
            TextObject text = GameTexts.FindText("str_rebellion_finished");
            text.SetTextVariable("SETTLEMENT", settlement.Name);
            text.SetTextVariable("RULER", oldOwnerClan.Leader.Name);
            MBInformationManager.AddQuickInformation(text);
        }

        private void OnTournamentFinished(CharacterObject winner, MBReadOnlyList<CharacterObject> participants, Town town, ItemObject prize)
        {
            if (!winner.IsHero) return;
            if (!TORNotificationHelper.IsHeroRelevantToPlayer(winner.HeroObject)) return;
            TextObject text = GameTexts.FindText("str_tournament_companion_won_prize");
            text.SetTextVariable("ITEM_NAME", prize.Name);
            text.SetCharacterProperties("COMPANION", winner);
            MBInformationManager.AddQuickInformation(text);
        }

        private void OnBuildingLevelChanged(Town town, Building building, int levelChange)
        {
            if (levelChange <= 0 || town.OwnerClan != Clan.PlayerClan)
                return;
            TextObject textObject = building.CurrentLevel == 1 ? GameTexts.FindText("str_building_completed") : GameTexts.FindText("str_building_level_gained");
            textObject.SetTextVariable("SETTLEMENT_NAME", town.Name);
            textObject.SetTextVariable("BUILDING_NAME", building.Name);
            InformationManager.DisplayMessage(new InformationMessage(textObject.ToString(), new Color(0.0f, 1f, 0.0f)));
        }

        private void OnHeroTeleportationRequested(Hero hero, Settlement targetSettlement, MobileParty targetParty, TeleportHeroAction.TeleportationDetail detail)
        {
            if (detail == TeleportHeroAction.TeleportationDetail.ImmediateTeleportToParty && targetParty == MobileParty.MainParty && MobileParty.MainParty.IsActive)
            {
                TextObject textObject = new TextObject("{=abux36nq}{HERO.NAME} joined your party.");
                textObject.SetCharacterProperties("HERO", hero.CharacterObject);
                MBInformationManager.AddQuickInformation(textObject);
            }
            if (detail == TeleportHeroAction.TeleportationDetail.ImmediateTeleportToPartyAsPartyLeader && targetParty.ActualClan == Clan.PlayerClan && targetParty != MobileParty.MainParty)
            {
                TextObject textObject = new TextObject("{=xxSlIDCW}{HERO.NAME} has joined {?HERO.GENDER}her{?}his{\\?} party and assumed command.");
                textObject.SetCharacterProperties("HERO", hero.CharacterObject);
                MBInformationManager.AddQuickInformation(textObject);
            }
            if (detail != TeleportHeroAction.TeleportationDetail.ImmediateTeleportToSettlement || hero.Clan != Clan.PlayerClan || !targetSettlement.IsTown || targetSettlement.Town.Governor != hero || hero.HeroState != Hero.CharacterStates.Traveling)
                return;
            TextObject textObject1 = new TextObject("{=btynhBAn}The new governor of {SETTLEMENT}, {HERO.NAME}, has arrived and taken up the reins of office.");
            textObject1.SetCharacterProperties("HERO", hero.CharacterObject);
            textObject1.SetTextVariable("SETTLEMENT", targetSettlement.Name);
            MBInformationManager.AddQuickInformation(textObject1);
        }
    }
}
