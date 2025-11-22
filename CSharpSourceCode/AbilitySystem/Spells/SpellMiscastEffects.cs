using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem.Spells
{
    internal static class SpellMiscastEffects
    {
        private const string LORE_FIRE_ID = "LoreOfFire";

        private const string EFFECT_AQSHY_ENTRY = "tor_miscast_aqshy_entry";
        private const string EFFECT_AQSHY_ADEPT = "tor_miscast_aqshy_adept";
        private const string EFFECT_AQSHY_MASTER = "tor_miscast_aqshy_master";

        internal static bool ApplyLoreSpecificMiscastConsequences(Hero hero, Agent casterAgent, Spell spell)
        {
            if (hero == null || casterAgent == null || spell == null)
                return false;

            var template = spell.Template;
            if (template == null)
                return false;

            // testing, fire only
            if (template.BelongsToLoreID != LORE_FIRE_ID)
                return false;

            ApplyAqshyBurn(casterAgent, spell);
            ShowAqshyMiscastMessage(hero);

            return true;
        }

        private static void ApplyAqshyBurn(Agent casterAgent, Spell spell)
        {
            int spellTier = spell.Template.SpellTier;
            string effectId;
            float durationSeconds;

            // 1 minor, 2 entry, 3 adept, 4 =master
            if (spellTier <= 2)
            {
                //entry penalty
                effectId = EFFECT_AQSHY_ENTRY;
                durationSeconds = 3f;
            }
            else if (spellTier == 3)
            {
                //adept penalty
                effectId = EFFECT_AQSHY_ADEPT;
                durationSeconds = 5f;
            }
            else
            {
                // master penalty
                effectId = EFFECT_AQSHY_MASTER;
                durationSeconds = 7f;
            }

            casterAgent.ApplyStatusEffect(effectId, casterAgent, durationSeconds, true);
        }


        private static void ShowAqshyMiscastMessage(Hero hero)
        {
            TextObject text;

            if (hero == Hero.MainHero)
            {
                //player
                text = new TextObject("{=tor_spell_miscast_aqshy_player}Your attempt to call upon the Winds of Aqshy has failed. Aqshy lashes back, burning you from within.");
            }
            else
            {
                //rest
                text = new TextObject("{=tor_spell_miscast_aqshy_other}{CASTER}'s attempt to call upon the Winds of Aqshy has failed. Aqshy lashes back, burning them from within.");
                text.SetTextVariable("CASTER", hero.Name);
            }
            InformationManager.DisplayMessage(new InformationMessage(text.ToString(), Colors.Red));
        }
    }
}
