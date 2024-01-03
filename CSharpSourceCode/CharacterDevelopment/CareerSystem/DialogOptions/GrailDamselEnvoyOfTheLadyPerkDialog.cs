using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.Careers
{
    public class GrailDamselEnvoyOfTheLadyPerkDialog
    {
        public GrailDamselEnvoyOfTheLadyPerkDialog(CampaignGameStarter campaignGameStarter)
        {
            EnvoyOfTheLadyDialogOptions(campaignGameStarter);
        }

        private List<Kingdom> _factionsAtWarWith;
        private List<Kingdom> _factionsRuleBretonnianSettlement;
        
        private void EnvoyOfTheLadyDialogOptions(CampaignGameStarter campaignGameStarter)
        {
            campaignGameStarter.AddPlayerLine("convincelord0", "lord_talk_speak_diplomacy_2", "convincelord1", "The Lady demands that you stop slaughtering your fellow Bretonnians!", () => FullfillsEnvoyOfTheLadyCondition() && CivilWarCondition(), null);

            campaignGameStarter.AddDialogLine("convincelord1", "convincelord1", "convincelord2", "Mylady, how dare you demand me... oh she does?", null, null, 200, null);
            campaignGameStarter.AddDialogLine("convincelord2", "convincelord2", "convincelordplayerchoice", "I guess, I can't deny your biding. If the lady demands, I will obey.", null, null, 200, null);

            campaignGameStarter.AddPlayerLine("convincelordwar0", "lord_talk_speak_diplomacy_2", "convincelordWar1", "Our fair and noble land has been invaded! The lady demands that you strike her enemies!", () => FullfillsEnvoyOfTheLadyCondition() && foreignForceRulesSettlementinbretonnia(), null, 200, null, null);
            campaignGameStarter.AddDialogLine("convincelordWar1", "convincelordWar1", "convincelordWar2", "Ah yeah so you know better, which thread bothers us most ?", null, null, 200, null);
            campaignGameStarter.AddDialogLine("convincelordWar2", "convincelordWar2", "convincelordplayerchoicewar", "I guess, I can't deny you your biding. If the lady demands, I will obey.", null, null, 200, null);

            campaignGameStarter.AddPlayerLine("convincelordplayerchoice1", "convincelordplayerchoice", "convincelord_end", "Stop war with {FACTION_NAME_1}.", condition_faction_war1, consequence_stop_war_faction1, 200, null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoice2", "convincelordplayerchoice", "convincelord_end", "Stop war with {FACTION_NAME_2}.", condition_faction_war2, consequence_stop_war_faction2, 200, null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoice3", "convincelordplayerchoice", "convincelord_end", "Stop war with {FACTION_NAME_3}.", condition_faction_war3, consequence_stop_war_faction3, 200, null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoice3", "convincelordplayerchoice", "lord_pretalk", "Actually never mind", null, null, 200, null, null);

            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar1", "convincelordplayerchoicewar", "convincelord_end", "We need to unite against {FACTION_NAME_1}.", condition_enemy1, consequence_declareWar1, 200, null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar2", "convincelordplayerchoicewar", "convincelord_end", "We need to unite against {FACTION_NAME_2}.", condition_enemy2, consequence_declareWar2, 200, null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar3", "convincelordplayerchoicewar", "convincelord_end", "We need to unite against {FACTION_NAME_3}.", condition_enemy3, consequence_declareWar3, 200, null, null);
            campaignGameStarter.AddPlayerLine("convincelordplayerchoicewar3", "convincelordplayerchoicewar", "lord_pretalk", "Actually never mind", null, null, 200, null, null);
            
            campaignGameStarter.AddDialogLine("convincelord_end", "convincelord_end", "lord_pretalk", "As you wish Mylady.", null, null, 200, null);
        }
        
        


        private bool foreignForceRulesSettlementinbretonnia()
        {
            var settlements = Campaign.Current.Settlements;

            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;

            if (!character.IsKingdomLeader) return false;

            _factionsRuleBretonnianSettlement = new List<Kingdom>();
            foreach (var settlement in settlements)
            {
                if (settlement.IsBretonnianMayorSettlement() && settlement.Owner.Culture.StringId != "vlandia")
                {
                    if (settlement.Owner.Clan != null || settlement.Owner.Clan.Kingdom != null)
                    {
                        if (character.Clan.Kingdom.IsAtWarWith(settlement.Owner.Clan.Kingdom))
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
                GameTexts.SetVariable("FACTION_NAME_1", _factionsRuleBretonnianSettlement[0].Name);
                return true;
            }

            return false;
        }

        private bool condition_enemy2()
        {
            if (_factionsRuleBretonnianSettlement.Count >= 2)
            {
                GameTexts.SetVariable("FACTION_NAME_2", _factionsRuleBretonnianSettlement[1].Name);
                return true;
            }

            return false;
        }

        private bool condition_enemy3()
        {
            if (_factionsRuleBretonnianSettlement.Count >= 2)
            {
                GameTexts.SetVariable("FACTION_NAME_3", _factionsRuleBretonnianSettlement[2].Name);
                return true;
            }

            return false;
        }

        public void consequence_declareWar1()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            DeclareWarAction.ApplyByKingdomDecision(character.Clan.Kingdom, _factionsRuleBretonnianSettlement[0]);
        }

        public void consequence_declareWar2()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            DeclareWarAction.ApplyByKingdomDecision(character.Clan.Kingdom, _factionsRuleBretonnianSettlement[1]);
        }

        public void consequence_declareWar3()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            DeclareWarAction.ApplyByKingdomDecision(character.Clan.Kingdom, _factionsRuleBretonnianSettlement[2]);
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
                GameTexts.SetVariable("FACTION_NAME_1", _factionsAtWarWith[0].Name);
                return true;
            }

            return false;
        }

        private bool condition_faction_war2()
        {
            if (_factionsAtWarWith.Count >= 2)
            {
                GameTexts.SetVariable("FACTION_NAME_2", _factionsAtWarWith[2].Name);
                return true;
            }

            return false;
        }

        private bool condition_faction_war3()
        {
            if (_factionsAtWarWith.Count >= 3)
            {
                GameTexts.SetVariable("FACTION_NAME_3", _factionsAtWarWith[3].Name);
                return true;
            }

            return false;
        }

        private bool CivilWarCondition()
        {
            var character = Campaign.Current.ConversationManager.OneToOneConversationCharacter.HeroObject;
            if (!character.IsKingdomLeader) return false;

            _factionsAtWarWith = new List<Kingdom>();
            foreach (var faction in Campaign.Current.Kingdoms.Where(faction => faction.IsAtWarWith(character.Clan.Kingdom) && faction.Culture == character.Culture))
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
    }
}