using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using System.Linq;
using TaleWorlds.LinQuick;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using Helpers;
using TaleWorlds.CampaignSystem.CharacterDevelopment;

namespace TOR_Core.AbilitySystem.Spells
{
    internal static class SpellMiscastModel
    {
        private const float ARMOR_PENALTY_THRESHOLD = 11f;
        private const float ARMOR_PENALTY_PER_WEIGHT = 0.02f;
        private const float MAX_TOTAL_MISCAST_CHANCE = 0.95f;

        internal static bool TryHandleMiscast(Agent casterAgent, Spell spell)
        {
            if (casterAgent == null || spell == null)
                return false;

            Hero hero = casterAgent.GetHero();
            if (hero == null)
                return false;

            float miscastChance = CalculateMiscastChance(hero, casterAgent, spell);
            if (miscastChance <= 0f)
                return false;

            float roll = MBRandom.RandomFloat;

            if (roll > miscastChance)
                return false;

            ApplyMiscastPenalty(hero, spell);

            // lore dependant effect + txt
            bool handledByLore = SpellMiscastEffects.ApplyLoreSpecificMiscastConsequences(hero, casterAgent, spell);

            // not yet implemented lores
            if (!handledByLore)
            {
                ShowMiscastMessage(hero, spell);
            }

            // exit casting and /2 cd
            ApplyMiscastCooldownAndExitAbilityMode(casterAgent, spell);

            return true;
        }

        private static float CalculateMiscastChance(Hero hero, Agent casterAgent, Spell spell)
        {
            int spellcraftSkill = hero.GetSkillValue(TORSkills.SpellCraft);
            int spellTier = spell.Template.SpellTier;

            float baseChance = GetBaseMiscastChance(spellcraftSkill, spellTier);
            float armorPenalty = GetArmorPenalty(hero);
            float loreModifier = GetLoreSpecificMiscastModifier(hero, spell); // wip

            float total = baseChance + armorPenalty + loreModifier;

            if (total < 0f)
                total = 0f;
            else if (total > MAX_TOTAL_MISCAST_CHANCE)
                total = MAX_TOTAL_MISCAST_CHANCE;

            return total;
        }

        private static float GetBaseMiscastChance(int spellcraftSkill, int spellTier)
        {
            switch (spellTier)
            {
                case 2:
                    // Entry: skill 25 -> %30, 120 -> %0
                    return EvalLinearCurve(spellcraftSkill, 25, 120, 0.30f, 0.00f);

                case 3:
                    // Adept: skill 100 -> %45, 205 -> %5
                    return EvalLinearCurve(spellcraftSkill, 100, 205, 0.45f, 0.05f);

                case 4:
                    // Master: skill 200 -> %55, 300 -> %15
                    return EvalLinearCurve(spellcraftSkill, 200, 300, 0.55f, 0.15f);

                default:
                    // minor i guess? 
                    return 0f;
            }
        }



        private static float EvalLinearCurve(int skill, int startSkill, int endSkill, float startChance, float endChance)
        {
            if (skill <= startSkill)
                return startChance;

            if (skill >= endSkill)
                return endChance;

            float t = (skill - startSkill) / (float)(endSkill - startSkill);
            return startChance + (endChance - startChance) * t;
        }

        private static float GetArmorPenalty(Hero hero)
        {
            if (hero == null)
                return 0f;

            var character = hero.CharacterObject;
            if (character == null)
                return 0f;

            if (hero != Hero.MainHero && hero.IsVampire())
            {
                return 0f;
            }

            var equipment = character.Equipment;
            var effectiveWeight = new ExplainedNumber(equipment.GetTotalWeightOfArmor(true));

            PerkHelper.AddPerkBonusForCharacter(
                DefaultPerks.Athletics.FormFittingArmor,
                character,
                true,
                ref effectiveWeight);

            float total = effectiveWeight.ResultNumber;

            if (total <= ARMOR_PENALTY_THRESHOLD)
                return 0f;

            float over = total - ARMOR_PENALTY_THRESHOLD;
            return over * ARMOR_PENALTY_PER_WEIGHT;
        }

        private static float GetLoreSpecificMiscastModifier(Hero hero, Spell spell)
        {
            // wip
            return 0f;
        }

        private static void ApplyMiscastPenalty(Hero hero, Spell spell)
        {
            var info = hero.GetExtendedInfo();
            if (info == null)
                return;

            int fullCost = hero.GetEffectiveWindsCostForSpell(spell);
            if (fullCost <= 0)
                return;

            float penaltyCost = fullCost * 0.5f;
            info.AddCustomResource("WindsOfMagic", -penaltyCost);
        }

        private static void ApplyMiscastCooldownAndExitAbilityMode(Agent casterAgent, Spell spell)
        {
            if (casterAgent == null || spell == null || Mission.Current == null)
                return;

            var template = spell.Template;
            if (template == null)
                return;

            int baseCooldown = template.CoolDown;
            if (baseCooldown <= 0)
                return;

            var cooldown = new ExplainedNumber(baseCooldown);

            if (Game.Current?.GameType is Campaign)
            {
                if (casterAgent.IsMainAgent)
                {
                    var player = Hero.MainHero;

                    var type = template.AbilityType;
                    if (type == AbilityType.Spell)
                    {
                        // cd red base
                        CareerHelper.ApplyBasicCareerPassives(
                            player,
                            ref cooldown,
                            PassiveEffectType.WindsCooldownReduction,
                            true);

                        // from greylord
                        if (casterAgent.GetHero().HasCareer(TORCareers.GreyLord))
                        {
                            var choice = TORCareerChoices.GetChoice("SecretOfFellfangPassive1");
                            if (choice != null)
                            {
                                var component = Agent.Main.GetComponent<AbilityComponent>();
                                var count = component.KnownAbilitySystem.Count;
                                count--;

                                if (count < choice.GetPassiveValue())
                                {
                                    cooldown.AddFactor(-0.5f);
                                }
                            }
                        }
                    }
                }
            }

            int normalCooldown = (int)cooldown.ResultNumber;
            if (normalCooldown < 1)
                normalCooldown = 1;

            int miscastCooldown = normalCooldown / 2;
            if (miscastCooldown < 1)
                miscastCooldown = 1;

            spell.SetCoolDown(miscastCooldown);


            //exit slow mo
            if (casterAgent == Agent.Main)
            {
                var manager = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
                manager?.ForceExitAbilityModeAfterMiscast();
            }
        }


        private static void ShowMiscastMessage(Hero hero, Spell spell)
        {
            if (hero == null || spell == null)
                return;

            string spellName = spell.Template?.Name?.ToString() ?? "spell";

            TextObject text;
            if (hero == Hero.MainHero)
            {
                text = new TextObject("{=tor_spell_miscast_player}Your attempt to cast {SPELL} failed.");
                text.SetTextVariable("SPELL", spellName);
            }
            else
            {
                text = new TextObject("{=tor_spell_miscast_other}{CASTER}'s attempt to cast {SPELL} failed.");
                text.SetTextVariable("CASTER", hero.Name);
                text.SetTextVariable("SPELL", spellName);
            }

            InformationManager.DisplayMessage(new InformationMessage(text.ToString()));
        }


    }
}