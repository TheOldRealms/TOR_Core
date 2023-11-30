using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.TwoDimension;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics
{
   
    public class TORCareerPerkCampaignBehavior : CampaignBehaviorBase
    {
        private List<Kingdom> _factionsAtWarWith;
        private List<Kingdom> _factionsRuleBretonnianSettlement;
        
        public override void RegisterEvents()
        {
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyCareerTickEvents);
            CampaignEvents.ItemsLooted.AddNonSerializedListener(this, RaidingPartyEvent);
            CampaignEvents.OnUnitRecruitedEvent.AddNonSerializedListener(this, PlayerRecruitmentEvent);
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this,EnvoyOfTheLadyDialogOptions);
        }

        private void EnvoyOfTheLadyDialogOptions(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddPlayerLine("convincelord0", "lord_talk_speak_diplomacy_2", "convincelord1", "The Lady demands you to stop your atrocious War against the People of Bretonnia!", () => FullfillsEnvoyOfTheLadyCondition() && CivilWarCondition(), null);

            campaignGameStarter.AddDialogLine("convincelord1", "convincelord1", "convincelord2", "Mylady, how dare you demand me... oh she does?", null,null,200,null);
            campaignGameStarter.AddDialogLine("convincelord2", "convincelord2", "convincelordplayerchoice", "I guess, I can't deny your biding. If the lady demands, I will obey.", null,null,200,null);
            
            campaignGameStarter.AddPlayerLine("convincelordwar0", "lord_talk_speak_diplomacy_2", "convincelordWar1", "The People of Bretonnia of need to unite against our enemies!", () =>FullfillsEnvoyOfTheLadyCondition()&& foreignForceRulesSettlementinbretonnia(), null, 200, null, null);
            campaignGameStarter.AddDialogLine("convincelordWar1", "convincelordWar1", "convincelordWar2", "Ah yeah so you know better, which thread bothers us most ?", null,null,200,null);
            campaignGameStarter.AddDialogLine("convincelordWar2", "convincelordWar2", "convincelordplayerchoicewar", "I guess, I can't deny you your biding. If the lady demands, I will obey.", null,null,200,null); 
            
            campaignGameStarter.AddPlayerLine("convincelordplayerchoice1", "convincelordplayerchoice","convincelord_end", "Stop war with {FACTION_NAME_1}.", condition_faction_war1, consequence_stop_war_faction1, 200,null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoice2", "convincelordplayerchoice","convincelord_end", "Stop war with {FACTION_NAME_2}.", condition_faction_war2, consequence_stop_war_faction2, 200,null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoice3", "convincelordplayerchoice","convincelord_end", "Stop war with {FACTION_NAME_3}.", condition_faction_war3, consequence_stop_war_faction3, 200,null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoice3", "convincelordplayerchoice","lord_pretalk", "Actually never mind",null , null, 200,null, null);

            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar1", "convincelordplayerchoicewar","convincelord_end", "We need to unite against {FACTION_NAME_1}.", condition_enemy1, consequence_declareWar1, 200,null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar2", "convincelordplayerchoicewar","convincelord_end", "We need to unite against {FACTION_NAME_2}.", condition_enemy2, consequence_declareWar2, 200,null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar3", "convincelordplayerchoicewar","convincelord_end", "We need to unite against {FACTION_NAME_3}.", condition_enemy3, consequence_declareWar3, 200,null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar3", "convincelordplayerchoicewar","lord_pretalk", "Actually never mind",null , null, 200,null, null);
            
            
            campaignGameStarter.AddDialogLine("convincelord_end", "convincelord_end", "lord_pretalk", "As you wish Mylady.", null,null,200,null);

            
            
        //    campaignGameStarter.AddPlayerLine("convincelordplayerchoice", "port_option_1", "convincelord_end.", "Stop war with {FACTION_NAME_1}.", condition_faction_war2, true);
         //   campaignGameStarter.AddPlayerLine("convincelordplayerchoice", "port_option_2", "convincelord_end.", "Stop war with {FACTION_NAME_1}.", condition_faction_war3, true);
            
        }
        
        
        

        private bool foreignForceRulesSettlementinbretonnia()
        {
            var settlements = Campaign.Current.Settlements;

            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            if (!character.IsKingdomLeader) return false;
            
            _factionsRuleBretonnianSettlement = new List<Kingdom>();
            foreach (var settlement in settlements)
            {
                if (settlement.IsBrettonianMayorSettlement() && settlement.Owner.Culture.StringId != "vlandia")
                {
                    if (settlement.Owner.Clan != null || settlement.Owner.Clan.Kingdom != null)
                    {
                        if(character.Clan.Kingdom.IsAtWarWith(settlement.Owner.Clan.Kingdom))
                        {
                            continue;
                        }
                        
                        if (!_factionsRuleBretonnianSettlement.Contains(settlement.Owner.Clan.Kingdom))
                        {
                            _factionsRuleBretonnianSettlement.Add(settlement.Owner.Clan.Kingdom);
                        }
                        
                    }
                    
                }
            }

            if (!_factionsRuleBretonnianSettlement.IsEmpty())
            {
                return true;
            }
            
            return false;
        }
        
        private bool condition_enemy1()
        {
            if (_factionsRuleBretonnianSettlement.Count >= 1)
            {
                GameTexts.SetVariable("FACTION_NAME_1",_factionsRuleBretonnianSettlement[0].Name);
                return true;
            }

            return false;
        }
        
        private bool condition_enemy2()
        {
            if (_factionsRuleBretonnianSettlement.Count >= 2)
            {
                GameTexts.SetVariable("FACTION_NAME_2",_factionsRuleBretonnianSettlement[1].Name);
                return true;
            }

            return false;
        }
        
        private bool condition_enemy3()
        {
            if (_factionsRuleBretonnianSettlement.Count >= 2)
            {
                GameTexts.SetVariable("FACTION_NAME_3",_factionsRuleBretonnianSettlement[2].Name);
                return true;
            }

            return false;
        }

        public void consequence_declareWar1()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            DeclareWarAction.ApplyByKingdomDecision(character.Clan.Kingdom,_factionsRuleBretonnianSettlement[0]);
        }
        
        public void consequence_declareWar2()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            DeclareWarAction.ApplyByKingdomDecision(character.Clan.Kingdom,_factionsRuleBretonnianSettlement[1]);
        }
        
        public void consequence_declareWar3()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            DeclareWarAction.ApplyByKingdomDecision(character.Clan.Kingdom,_factionsRuleBretonnianSettlement[2]);
        }
        
        private void consequence_stop_war_faction1()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            MakePeaceAction.Apply(character.Clan.Kingdom, _factionsAtWarWith[0]);
        }
        private void consequence_stop_war_faction2()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            MakePeaceAction.Apply(character.Clan.Kingdom, _factionsAtWarWith[1]);
        }
        
        private void consequence_stop_war_faction3()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            MakePeaceAction.Apply(character.Clan.Kingdom, _factionsAtWarWith[2]);
        }

       
        
        private bool condition_faction_war1()
        {
            if (_factionsAtWarWith.Count >= 1)
            {
                GameTexts.SetVariable("FACTION_NAME_1",_factionsAtWarWith[0].Name);
                return true;
            }

            return false;
        }
        
        private bool condition_faction_war2()
        {
            if (_factionsAtWarWith.Count >= 2)
            {
                GameTexts.SetVariable("FACTION_NAME_2",_factionsAtWarWith[2].Name);
                return true;
            }

            return false;
        }
        
        private bool condition_faction_war3()
        {
            if (_factionsAtWarWith.Count >= 3)
            {
                GameTexts.SetVariable("FACTION_NAME_3",_factionsAtWarWith[3].Name);
                return true;
            }

            return false;
        }

        public bool CivilWarCondition()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            
            if (!character.IsKingdomLeader) return false;

            _factionsAtWarWith = new List<Kingdom>();
            
            foreach (var faction in Campaign.Current.Kingdoms.Where(faction => faction.IsAtWarWith(character.Clan.Kingdom)&& faction.Culture== character.Culture))
            {
                _factionsAtWarWith.Add(faction);
            }


            return _factionsAtWarWith.Any();
        }

        private bool FullfillsEnvoyOfTheLadyCondition()
        {
            var choices = Hero.MainHero.GetAllCareerChoices();
                
            return choices.Contains("EnvoyOfTheLadyPassive4");
        }
        
        
        
        
        
        
        private void PlayerRecruitmentEvent(CharacterObject characterObject, int amount)
        {
            if (Hero.MainHero.HasAnyCareer())
            {
                var party = Hero.MainHero.PartyBelongedTo;
                var choices = Hero.MainHero.GetAllCareerChoices();
                
                if (choices.Contains("CommanderPassive4"))
                {
                    var choice = TORCareerChoices.GetChoice("CommanderPassive4");
                    if (choice != null)
                        AddExtraTroopsWithChanceIfPossible(characterObject, amount, party, choice.GetPassiveValue());
                }

                if (choices.Contains("InspirationOfTheLadyPassive1"))
                {
                    var choice = TORCareerChoices.GetChoice("InspirationOfTheLadyPassive1");
                    if (choice != null)
                        AddExtraTroopsWithChanceIfPossible(characterObject, amount, party, choice.GetPassiveValue());
                }

                if (choices.Contains("MonsterSlayerPassive3"))
                {
                    var choice = TORCareerChoices.GetChoice("MonsterSlayerPassive3");
                    if (choice != null)
                        AddExtraTroopsWithChanceIfPossible(characterObject, amount, party, choice.GetPassiveValue());
                }
            }
        }


        private void AddExtraTroopsWithChanceIfPossible(CharacterObject troop, int originalAmount, MobileParty party, float chance)
        {
            for (int i = 0; i < originalAmount; i++)
            {
                if (party.Party.PartySizeLimit - party.Party.NumberOfAllMembers + 1 < 0)
                {
                    break;
                }

                if (MBRandom.RandomFloatRanged(0, 1) < chance)
                {
                    party.AddElementToMemberRoster(troop, 1);
                }
            }
        }

        private void RaidingPartyEvent(MobileParty mobileParty, ItemRoster itemRoster)
        {
            if (mobileParty != MobileParty.MainParty && mobileParty.MapEvent.IsRaid) return;

            if (mobileParty == null || mobileParty.LeaderHero != Hero.MainHero) return;
            if (!MobileParty.MainParty.LeaderHero.HasAnyCareer()) return;

            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("BladeMasterPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("BladeMasterPassive4");
                if (choice == null) return;
                var memberList = mobileParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (member.Character.IsVampire() && !member.Character.IsHero)
                    {
                        mobileParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                    }
                    else if (member.Character.IsHero && member.Character.IsVampire() && member.Character.IsHero)
                    {
                        var character = member.Character.HeroObject;
                        var skills = new List<SkillObject> { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm, DefaultSkills.Riding, DefaultSkills.Tactics };
                        var targetSkill = skills.GetRandomElement();
                        character.AddSkillXp(targetSkill, choice.GetPassiveValue());
                    }
                }
            }
        }
        
        private void DailyCareerTickEvents(MobileParty mobileParty)
        {
            if (mobileParty != MobileParty.MainParty) return;

            if (!MobileParty.MainParty.LeaderHero.HasAnyCareer()) return;
            var choices = MobileParty.MainParty.LeaderHero.GetAllCareerChoices();

            if (choices.Contains("SurvivalistPassive4"))
            {
                LaunchHuntingEvent(mobileParty);
            }

            if (choices.Contains("PeerlessWarriorPassive4"))
            {
                var choice = TORCareerChoices.GetChoice("PeerlessWarriorPassive4");
                if (choice != null)
                {
                    var skills = new List<SkillObject> { DefaultSkills.OneHanded, DefaultSkills.TwoHanded, DefaultSkills.Polearm };
                    var targetSkill = skills.GetRandomElement();
                    Hero.MainHero.AddSkillXp(targetSkill, choice.GetPassiveValue());
                }
            }

            if (choices.Contains("ErrantryWarPassive4"))
            {
                var memberList = mobileParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (!member.Character.IsRanged && !member.Character.IsHero)
                    {
                        var choice = TORCareerChoices.GetChoice("ErrantryWarPassive4");
                        if (choice != null)
                        {
                            mobileParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                        }
                    }
                }
            }
            
            if (choices.Contains("JustCausePassive2"))
            {
                var memberList = mobileParty.MemberRoster.GetTroopRoster();
                for (var index = 0; index < memberList.Count; index++)
                {
                    var member = memberList[index];
                    if (!member.Character.IsKnightUnit())
                    {
                        var choice = TORCareerChoices.GetChoice("JustCausePassive2");
                        if (choice != null)
                        {
                            mobileParty.MemberRoster.AddXpToTroopAtIndex((int)choice.GetPassiveValue(), index);
                        }
                    }
                }
            }
        }
        
        private static void LaunchHuntingEvent(MobileParty mobileParty)
        {
            if (mobileParty.CurrentSettlement != null || mobileParty.Army != null) return;

            TerrainType faceTerrainType = Campaign.Current.MapSceneWrapper.GetFaceTerrainType(mobileParty.CurrentNavigationFace);
            var scoutingValue = mobileParty.LeaderHero.GetSkillValue(DefaultSkills.Scouting);
            var wieldedWeaponValue = 0;
            var weaponSkill = DefaultSkills.Bow;
            var weapons = mobileParty.LeaderHero.CharacterObject.GetCharacterEquipment(EquipmentIndex.WeaponItemBeginSlot, EquipmentIndex.Weapon3).Where(x => x.WeaponComponent.PrimaryWeapon.IsRangedWeapon || x.PrimaryWeapon.RelevantSkill == DefaultSkills.Polearm);
            if (weapons.IsEmpty()) return;
            var wielded = weapons.ToList().MaxBy(x => mobileParty.LeaderHero.GetSkillValue(x.RelevantSkill));
            if (wielded == null) return;

            weaponSkill = wielded.PrimaryWeapon.RelevantSkill;
            wieldedWeaponValue = mobileParty.LeaderHero.GetSkillValue(wielded.PrimaryWeapon.RelevantSkill);

            var PreyChance = (((float)scoutingValue) / 300) * 0.9f;
            if (faceTerrainType == TerrainType.Forest)
                PreyChance += 0.1f;

            var huntSucess = ((scoutingValue + wieldedWeaponValue) / 600f);
            if (mobileParty.HasBlessing("cult_of_taal"))
                huntSucess *= 1.1f;

            var PreySize = MBRandom.RandomInt(1, 3);
            if (MBRandom.RandomFloatRanged(0, 1) >= PreyChance) return;
            mobileParty.LeaderHero.AddSkillXp(DefaultSkills.Scouting, 50f * PreySize);
            var preySizeAnimalText = "";
            switch (PreySize)
            {
                case 1: preySizeAnimalText = new TextObject ("{=tor_hunt_perk_animal_large_str}large Animal").ToString();
                    break;
                case 2: preySizeAnimalText = new TextObject ("{=tor_hunt_perk_animal_medium_str}medium Animal").ToString();
                    break;
                case 3: preySizeAnimalText = new TextObject ("{=tor_hunt_perk_animal_small_str}small Animal").ToString();
                    break;
            }
            
            MBTextManager.SetTextVariable("PERK_HUNT_ANIMAL_SIZE", preySizeAnimalText);
            
            
            
            if (MBRandom.RandomFloatRanged(0, 1) >= huntSucess)
            {
                InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText ("tor_hunt_perk_result","Failed").ToString()));
                return;
            }
            
            var preyText = PreySize > 1 ? $"({PreySize} {DefaultItems.Meat.Name}, {PreySize}{DefaultItems.Hides.Name})" : $"{PreySize} {DefaultItems.Meat.Name}";
            MBTextManager.SetTextVariable("PERK_HUNT_PREY", preyText);

            mobileParty.LeaderHero.AddSkillXp(weaponSkill, 50f * PreySize);
            mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Meat, PreySize));
            if (PreySize > 1)
                mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Hides, PreySize));
            
            InformationManager.DisplayMessage(new InformationMessage(GameTexts.FindText ("tor_hunt_perk_result","Success").ToString()));
        }

        public override void SyncData(IDataStore dataStore)
        {
        }
    }
}