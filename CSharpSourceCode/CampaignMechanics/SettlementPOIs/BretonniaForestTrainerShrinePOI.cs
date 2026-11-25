using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Crafting;
using TOR_Core.CampaignMechanics.SpellTrainers;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.CampaignMechanics.SettlementPOIs
{
    /// <summary>
    /// Spike: first POI attached to a non-town settlement (Grove of the Lady shrine).
    /// Validates that <c>PlayerEncounter.EnterSettlement()</c> can prime
    /// <c>LocationEncounter</c> on shrines so our existing scene-launch path works.
    /// </summary>
    public class BretonniaForestTrainerShrinePOI : SettlementPOI, ISpellTrainerHost, IEnchanterHost
    {
        public override string LocationId => "tor_bretonnia_forest_trainer_shrine";
        public override string ParentMenuId => "shrine_menu";
        public override string AnchorEntryId => "leave";
        public override bool AnchorAbove => true;

        public override string EnterLabelId => "tor_poi_bretonnia_forest_trainer_shrine.EnterLabel";
        public override string EnterLabelDefault => "Approach the shrine...";

        // Shrines don't get a vanilla LocationEncounter — launch the scene directly.
        public override bool UsesMissionLaunch => true;

        public string SpellTrainerTemplateId => "tor_spelltrainer_bretonnia_0";
        public string EnchanterTemplateId    => "tor_spelltrainer_bretonnia_0";
        public List<string> EnchantmentSuffixes => new() { "emp_enchant", "bret_blessing" };

        // Same template fills both roles; template-id dedup in the manager keeps it one hero.
        public override void EnsurePlaced(Settlement s) => PlaceResident(s, SpellTrainerTemplateId);

        public bool IsPartyEligibleForTrainer()
        {
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.BRETONNIA) return true;
            return SpellTrainerHelper.HasMatchingSpellTrainerCompanion(
                x => x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster());
        }

        // Same hero plays both roles via template-id dedup, so eligibility is identical.
        public bool IsPartyEligibleForEnchanter() => IsPartyEligibleForTrainer();

        public override bool CheckAccess(Settlement s, Hero h, out TextObject denialReason)
        {
            if (!IsDiscovered(s)) { denialReason = R("Undiscovered", "You have not yet found this place."); return false; }
            if (!AppliesTo(s))    { denialReason = R("Sealed", "This place is sealed."); return false; }
            if (s.IsUnderSiege)   { denialReason = R("SiegeSealed", "The grove's wards close during the siege."); return false; }

            if (h.Culture.StringId != TORConstants.Cultures.BRETONNIA)
            {
                denialReason = R("WrongCulture", "The Lady's shrine is a sacred place of Bretonnia.");
                return false;
            }
            denialReason = null;
            return true;
        }

        public override void RegisterDialogs(CampaignGameStarter obj)
        {
            obj.AddDialogLine("trainer_prophetesse_start", "start", "choices_prophetesse", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_start", "Welcome, child of Bretonnia. The Lady has guided you to my presence. Speak, and let your intentions unfold."), isMorgianaLeFay, null, 210, null);
            obj.AddDialogLine("trainer_prophetesse_start", "hub_prophetesse", "choices_prophetesse", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_choices", "Is there more you seek? Speak your desires."), isMorgianaLeFay, null, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_learnspells", "choices_prophetesse", "openbook_prophetesse", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_open_book", "Revered Fay Enchantress, share with me some of your magic teachings."), () => MobileParty.MainParty.HasSpellCasterMember() && damselCondition(), null, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_scrollShop", "choices_prophetesse", "hub_prophetesse", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_scrolls", "Gracious Enchantress, I ask you for the tomes and scrolls that hold the keys to the Lady's wisdom."), null, SpellTrainerHelper.OpenScrollShop, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_scrollShop", "choices_prophetesse", "hub_prophetesse", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_damselsecond_lore", "My Fay Enchantress, I feel that {DAMSELNAME} has reached  a new level of magical potential, is there anything you can teach her?"), () => MobileParty.MainParty.HasSpellCasterMember() && damselCondition() && damselSecondLoreCondition(), damselSecondLoreConsequence, 200, null);
            obj.AddPlayerLine("trainer_prophetesse_playergoodbye", "choices_prophetesse", "saygoodbye", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_player_goodbye", "Until we meet again, my Fay Enchantress."), null, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_goodbye", "saygoodbye", "close_window", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_goodbye", "Go forth, and may the Lady's grace illuminate your path."), isMorgianaLeFay, null, 200, null);
            obj.AddDialogLine("trainer_prophetesse_afterlearnspells", "openbook_prophetesse", "hub_prophetesse", TORTextHelper.GetTextForNative("tor_spelltrainer_prophetesse_close_book", "You have grasped this weave with prowess. Carry this knowledge, and may it serve you well, as a beacon of the Lady's blessings."), null, SpellTrainerHelper.OpenbookConsequence, 200, null);

            // Enchanter branch — same hero as the trainer via template-id dedup,
            // attached to the Prophetess hub.
            EnchanterHelper.RegisterEnchanterBranch(obj,
                culture: TORConstants.Cultures.BRETONNIA,
                hubToken: "choices_prophetesse",
                quitToken: "hub_prophetesse",
                isMultiUse: true,
                enchantmentSuffixes: EnchantmentSuffixes,
                cultureAccess: IsPartyEligibleForEnchanter);

            bool isMorgianaLeFay()
            {
                if (!SpellTrainerHelper.SpellTrainerStartCondition()) return false;
                var partner = CharacterObject.OneToOneConversationCharacter;
                return partner.HeroObject != null && partner.HeroObject.Template.StringId == SpellTrainerHelper.ProphetessTrainerId;
            }

            bool damselCondition()
            {
                if (Hero.MainHero.HasCareer(TORCareers.GrailDamsel)) return true;
                if (Hero.MainHero.PartyBelongedTo == null) return false;
                return Hero.MainHero.PartyBelongedTo.GetMemberHeroes()
                    .Any(x => x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster());
            }

            bool damselSecondLoreCondition()
            {
                var damselCompanion = Hero.MainHero.PartyBelongedTo?.GetMemberHeroes()
                    .FirstOrDefault(x => x != Hero.MainHero && x.Culture.StringId == TORConstants.Cultures.BRETONNIA && x.IsSpellCaster());
                if (damselCompanion == null) return false;
                if (Hero.MainHero.HasCareer(TORCareers.GrailDamsel) && !damselCompanion.HasKnownLore("LoreOfBeasts"))
                {
                    GameTexts.SetVariable("DAMSELNAME", damselCompanion.Name);
                    return Hero.MainHero.HasAttribute(CharacterAttributes.CAREER_TIER_3);
                }
                return false;
            }

            void damselSecondLoreConsequence()
            {
                var name = new TextObject("{DAMSELNAME}").ToString();
                var hero = Hero.MainHero.PartyBelongedTo?.GetMemberHeroes()
                    .FirstOrDefault(x => x.Name.ToString() == name && x.IsSpellCaster());
                if (hero == null) return;
                hero.AddKnownLore("LoreOfBeasts");
            }
        }
    }
}
