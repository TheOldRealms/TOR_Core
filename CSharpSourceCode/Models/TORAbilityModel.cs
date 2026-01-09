using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.SpellCasting;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.BattleMechanics;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAbilityModel : GameModel
    {
        public SkillObject GetRelevantSkillForAbility(AbilityTemplate ability)
        {
            return ability.AbilityType switch
            {
                AbilityType.Spell => TORSkills.Spellcraft,
                AbilityType.Prayer => TORSkills.Faith,
                AbilityType.CareerAbility => null, // Career abilities don't grant skill XP
                _ => null,
            };
        }

        public SkillEffect GetRelevantSkillEffectForAbilityDamage(AbilityTemplate ability)
        {
            return ability.AbilityType switch
            {
                AbilityType.Spell => TORSkillEffects.SpellEffectiveness,
                AbilityType.Prayer => TORSkillEffects.PrayerEffectiveness,
                _ => null,
            };
        }

        public SkillEffect GetRelevantSkillEffectForAbilityDuration(AbilityTemplate ability)
        {
            return ability.AbilityType switch
            {
                AbilityType.Spell => TORSkillEffects.SpellDuration,
                AbilityType.Prayer => TORSkillEffects.PrayerDuration,
                _ => null,
            };
        }

        public int GetSkillXpForCastingAbility(AbilityTemplate ability)
        {
            if (ability.AbilityType == AbilityType.Prayer)
            {
                return ability.CoolDown * 4;
            }
            return ability.WindsOfMagicCost * 20;
        }

        public int GetSkillXpForAbilityDamage(AbilityTemplate ability, int damageAmount)
        {
            return damageAmount / 5;
        }

        /// <summary>
        /// Calculates total XP for a completed spell session based on damage, healing, and status effects.
        /// </summary>
        public int CalculateSpellSessionXp(SpellCastSession session)
        {
            if (session == null)
                return 0;

            int xp = 0;

            // XP for damage dealt (damage / 5)
            if (session.TotalDamageDealt > 0)
            {
                xp += session.TotalDamageDealt / 5;
            }

            // XP for healing done (healing / 5)
            if (session.TotalHealingDone > 0)
            {
                xp += session.TotalHealingDone / 5;
            }

            // XP for status effects applied (10 XP per unique agent affected)
            if (session.AgentsAffectedByStatusEffectsCount > 0)
            {
                xp += session.AgentsAffectedByStatusEffectsCount * 10;
            }

            // Single-target spells get 2x XP to balance against AoE spells
            if (session.AbilityTemplate != null && IsSingleTargetAbility(session.AbilityTemplate))
            {
                xp *= 5;
            }

            return xp;
        }

        /// <summary>
        /// Returns true if the ability is single-target (not AoE).
        /// </summary>
        private bool IsSingleTargetAbility(AbilityTemplate ability)
        {
            return ability.AbilityTargetType == AbilityTargetType.SingleEnemy ||
                   ability.AbilityTargetType == AbilityTargetType.SingleAlly ||
                   ability.AbilityTargetType == AbilityTargetType.Self;
        }

        /// <summary>
        /// Grants skill XP for dealing ability damage. Call this after damage is applied.
        /// </summary>
        public void ApplyAbilityDamageXp(Agent attacker, AbilityTemplate abilityTemplate, int damageDealt)
        {
            if (attacker == null || abilityTemplate == null || damageDealt <= 0)
                return;

            if (!attacker.IsHero)
                return;

            var hero = attacker.GetHero();
            if (hero == null)
                return;

            var skill = GetRelevantSkillForAbility(abilityTemplate);
            var xpAmount = GetSkillXpForAbilityDamage(abilityTemplate, damageDealt);

            if (xpAmount > 0)
            {
                hero.AddSkillXp(skill, xpAmount);

                // DarkVisionPassive3 - also grants Roguery XP
                if (hero.HasAnyCareer() && hero.HasCareerChoice("DarkVisionPassive3"))
                {
                    hero.AddSkillXp(DefaultSkills.Roguery, xpAmount);
                }
            }
        }

        public float GetSkillEffectivenessForAbilityDamage(CharacterObject character, AbilityTemplate ability)
        {
            ExplainedNumber explainedNumber = new(1f, false, null);
            var skill = GetRelevantSkillForAbility(ability);
            if (skill != null)
            {
                var skillValue = character.GetSkillValue(skill);
                var skillEffect = GetRelevantSkillEffectForAbilityDamage(ability);
                if (skillEffect != null) SkillHelper.AddSkillBonusForCharacter(skillEffect, character, ref explainedNumber);
            }

            if (character.IsHero && character.IsPlayerCharacter)
            {
                var playerHero = character.HeroObject;

                if (playerHero.HasCareer(TORCareers.GreyLord))
                {
                    var choice = TORCareerChoices.GetChoice("UnrestrictedMagicPassive4");
                    if (choice != null && Hero.MainHero.HasCareerChoice("UnrestrictedMagicPassive4"))
                    {
                        if (Agent.Main != null)
                        {
                            var comp = Agent.Main.GetComponent<AbilityComponent>();
                            if (!CareerChoicesHelper.ContainsSpellType(comp, [AbilityTargetType.AlliesInAOE, AbilityTargetType.EnemiesInAOE, AbilityTargetType.GroundAtPosition]))
                            {
                                explainedNumber.AddFactor(choice.GetPassiveValue());
                            }
                        }

                    }
                }

                if (playerHero.HasCareer(TORCareers.Runelord))
                {
                    if (Hero.MainHero.HasCareerChoice("AnvilOfDoomKeystone"))
                    {
                        if (Agent.Main != null)
                        {
                            var comp = Agent.Main.GetComponent<AbilityComponent>();
                            var spellcraftValue = Hero.MainHero.GetSkillValue(TORSkills.Spellcraft);
                            explainedNumber.AddFactor(0.005f * spellcraftValue);
                        }

                    }
                }
            }

            return explainedNumber.ResultNumber;
        }

        public float CalculateStatusEffectDurationForAbility(CharacterObject character, AbilityTemplate originAbilityTemplate, float statusEffectDuration)
        {

            float skillmultiplier = GetSkillEffectivenessForAbilityDuration(character, originAbilityTemplate);
            float perkmultiplier = 1f;
            if (character.IsHero) perkmultiplier = GetPerkEffectsOnAbilityDuration(character, originAbilityTemplate);

            if (character.IsHero && character.HeroObject == Hero.MainHero)
            {
                var player = character.HeroObject;
                var explainedNumber = new ExplainedNumber(1);

                if (originAbilityTemplate.AbilityEffectType == AbilityEffectType.Augment || originAbilityTemplate.AbilityEffectType == AbilityEffectType.Heal)
                {
                    CareerHelper.ApplyBasicCareerPassives(player, ref explainedNumber, PassiveEffectType.BuffDuration, true);
                }
                else if (originAbilityTemplate.AbilityEffectType == AbilityEffectType.Hex)
                {
                    CareerHelper.ApplyBasicCareerPassives(player, ref explainedNumber, PassiveEffectType.DebuffDuration, true);
                }

                perkmultiplier += (explainedNumber.ResultNumber - 1);
            }


            return statusEffectDuration * skillmultiplier * perkmultiplier;
        }

        public float CalculateRadiusForAbility(CharacterObject character, AbilityTemplate originAbilityTemplate, float radius)
        {
            if (character.IsHero && character.HeroObject == Hero.MainHero)
            {
                var playerHero = character.HeroObject;
                var explainedNumber = new ExplainedNumber(radius);
                if (Agent.Main != null)
                {
                    if (playerHero.HasCareer(TORCareers.GreyLord))
                    {
                        var choice = TORCareerChoices.GetChoice("LegendsOfMalokPassive4");
                        if (choice != null && playerHero.HasCareerChoice("LegendsOfMalokPassive4"))
                        {
                            var count = Agent.Main.GetAbilities().Count;
                            if (!CareerChoicesHelper.ContainsSpellType(Agent.Main.GetComponent<AbilityComponent>(), count, AbilityEffectType.Hex))
                            {
                                explainedNumber.AddFactor(choice.GetPassiveValue());
                            }
                        }
                    }
                    if (playerHero.HasCareer(TORCareers.Runelord))
                    {
                        if (playerHero.HasCareerChoice("ChiselAndHammerKeystone")) //Sly : why are these separate?
                        {
                            explainedNumber.AddFactor(0.2f);
                        }

                        if (playerHero.HasCareerChoice("ChiselAndHammerKeystone"))
                        {
                            var comp = Agent.Main.GetComponent<AbilityComponent>(); //what's this component for?
                            var smithingValue = playerHero.GetSkillValue(DefaultSkills.Crafting);
                            explainedNumber.AddFactor(0.005f * smithingValue);
                        }

                        if (playerHero.HasCareerChoice("LegacyOfGrungniPassive4"))
                        {
                            var smithingValue = playerHero.GetSkillValue(DefaultSkills.Crafting);
                            explainedNumber.AddFactor(0.005f * smithingValue);
                        }
                    }
                }


                //Sly : these are probably safe outside the Agent.Main check because they draw from the CharacterObject which exists regardless of the agent being dead/removed from the agent array
                var equipment = character.GetCharacterEquipment();

                foreach (var trait in equipment.Select(item => item.GetTraits())
                    .SelectMany(traits => traits.WhereQ(trait => trait.StatsTuple?.StatType == ItemTraitStatType.SpellRadius)))
                {
                    explainedNumber.AddFactor(trait.StatsTuple.Value / 100f);
                }


                CareerHelper.ApplyBasicCareerPassives(playerHero, ref explainedNumber, PassiveEffectType.SpellRadius, true);

                return explainedNumber.ResultNumber;
            }

            return radius;
        }

        public float GetSkillEffectivenessForAbilityDuration(CharacterObject character, AbilityTemplate ability)
        {
            ExplainedNumber explainedNumber = new(1f, false, null);
            var skill = GetRelevantSkillForAbility(ability);
            if (skill != null)
            {
                var skillValue = character.GetSkillValue(skill);
                var skillEffect = GetRelevantSkillEffectForAbilityDuration(ability);
                if (skillEffect != null) SkillHelper.AddSkillBonusForCharacter(skillEffect, character, ref explainedNumber);
            }
            return explainedNumber.ResultNumber;
        }

        public float GetPerkEffectsOnAbilityDuration(CharacterObject character, AbilityTemplate template)
        {
            ExplainedNumber explainedNumber = new(1f, false, null);
            if (character.GetPerkValue(TORPerks.Spellcraft.Selfish) && template.IsSpell)
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.Selfish, character, false, ref explainedNumber);
            }

            if (character.IsHero && character.HeroObject == Hero.MainHero && Agent.Main != null && Agent.Main.IsActive())
            {
                if (Hero.MainHero.HasCareer(TORCareers.GreyLord))
                {
                    if (template.AbilityEffectType == AbilityEffectType.Heal)
                    {
                        var choice = TORCareerChoices.GetChoice("SoulBindingPassive4");
                        if (choice != null && Hero.MainHero.HasCareerChoice("SoulBindingPassive4"))
                        {
                            var comp = Agent.Main.GetComponent<AbilityComponent>();
                            if (comp != null)
                            {
                                var count = Agent.Main.GetAbilities().Count;
                                if (!CareerChoicesHelper.ContainsSpellType(comp, count, AbilityEffectType.Projectile))
                                {
                                    explainedNumber.AddFactor(choice.GetPassiveValue());
                                }
                            }
                        }
                    }

                    if (template.AbilityEffectType == AbilityEffectType.Hex)
                    {
                        var choice = TORCareerChoices.GetChoice("ForbiddenScrollsOfSapheryPassive4");
                        if (choice != null && Hero.MainHero.HasCareerChoice("ForbiddenScrollsOfSapheryPassive4"))
                        {
                            var comp = Agent.Main.GetComponent<AbilityComponent>();
                            if (comp != null)
                            {
                                var count = Agent.Main.GetAbilities().Count;
                                if (!CareerChoicesHelper.ContainsSpellType(comp, count, AbilityEffectType.Heal))
                                {
                                    explainedNumber.AddFactor(choice.GetPassiveValue());
                                }
                            }
                        }
                    }

                    if (template.AbilityEffectType == AbilityEffectType.Vortex || template.AbilityEffectType == AbilityEffectType.Bombardment)
                    {
                        var choice = TORCareerChoices.GetChoice("ByAllMeansPassive4");
                        if (choice != null && Hero.MainHero.HasCareerChoice("ByAllMeansPassive4"))
                        {
                            var comp = Agent.Main.GetComponent<AbilityComponent>();
                            if (comp != null)
                            {
                                var count = Agent.Main.GetAbilities().Count;
                                if (!CareerChoicesHelper.ContainsSpellType(comp, count, AbilityEffectType.Augment))
                                {
                                    explainedNumber.AddFactor(choice.GetPassiveValue());
                                }
                            }
                        }
                    }
                }

                if (Hero.MainHero.HasCareer(TORCareers.Runelord))
                {
                    if (Hero.MainHero.HasCareerChoice("ForHearthAndHomePassive4"))
                    {
                        // 10% base duration increase
                        explainedNumber.AddFactor(0.1f);
                        // +0.1% per faith point (30% at 300 faith)
                        var faithValue = Hero.MainHero.GetSkillValue(TORSkills.Faith);
                        explainedNumber.AddFactor(0.001f * faithValue);
                    }
                }
            }
            return explainedNumber.ResultNumber;
        }

        public float GetPerkEffectsOnAbilityDamage(CharacterObject character, Agent victim, AbilityTemplate abilityTemplate)
        {
            ExplainedNumber explainedNumber = new(1f, false, null);
            var victimLeader = victim.GetPartyLeaderCharacter();
            var victimCaptain = victim.GetCaptainCharacter();

            if (character != null && abilityTemplate != null)
            {
                if (character.GetPerkValue(TORPerks.Spellcraft.Selfish) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    if (victim.Character is CharacterObject victimCharacter && character == victimCharacter)
                    {
                        PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.Selfish, character, true, ref explainedNumber);
                    }
                }
                if (character.GetPerkValue(TORPerks.Spellcraft.WellControlled) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    if (victimLeader != null && character == victimLeader)
                    {
                        PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.WellControlled, character, true, ref explainedNumber);
                    }
                }
                if (character.IsPlayerCharacter && character.IsHero && character.HeroObject == Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(Hero.MainHero, ref explainedNumber, PassiveEffectType.SpellEffectiveness, true);
                }
                if (character.GetPerkValue(TORPerks.Spellcraft.OverCaster) && abilityTemplate.IsSpell && (abilityTemplate.DoesDamage || abilityTemplate.DoesHeal))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.OverCaster, character, true, ref explainedNumber);
                }
                if (character.GetPerkValue(TORPerks.Spellcraft.EfficientSpellCaster) && abilityTemplate.IsSpell && (abilityTemplate.DoesDamage || abilityTemplate.DoesHeal))
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.EfficientSpellCaster, character, true, ref explainedNumber);
                }
                if (character.GetPerkValue(TORPerks.Spellcraft.Dampener) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.Dampener, character, true, ref explainedNumber);
                }
                if (victimCaptain != null && victimCaptain.GetPerkValue(TORPerks.Spellcraft.Dampener) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    explainedNumber.AddFactor(-0.3f);
                }

                if (character.HeroObject == Hero.MainHero)
                {
                    if (victimLeader != null && victimLeader.HeroObject == Hero.MainHero && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                    {
                        //friendly fire

                        if (Hero.MainHero.HasCareerChoice("ImperialEnchantmentPassive2"))
                        {
                            var choice = TORCareerChoices.GetChoice("ImperialEnchantmentPassive2");
                            explainedNumber.AddFactor(choice.GetPassiveValue());
                        }
                    }

                }
            }
            return explainedNumber.ResultNumber;
        }

        public int GetSpellGoldCostForHero(Hero hero, AbilityTemplate spellTemplate)
        {
            ExplainedNumber goldCost = new(spellTemplate.GoldCost);
            if (hero.GetPerkValue(TORPerks.Spellcraft.Librarian))
            {
                PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.Librarian, hero.CharacterObject, false, ref goldCost);
            }
            return (int)goldCost.ResultNumber;
        }

        public int GetEffectiveWindsCost(CharacterObject character, AbilityTemplate template)
        {
            ExplainedNumber cost = new(template.WindsOfMagicCost);
            if (character != null && template != null)
            {
                if (character.GetPerkValue(TORPerks.Spellcraft.OverCaster))
                {
                    cost.AddFactor(TORPerks.Spellcraft.OverCaster.SecondaryBonus);
                }
                if (character.GetPerkValue(TORPerks.Spellcraft.EfficientSpellCaster))
                {
                    cost.AddFactor(TORPerks.Spellcraft.EfficientSpellCaster.SecondaryBonus);
                }

                if (character.IsPlayerCharacter)
                {
                    var player = Hero.MainHero;

                    CareerHelper.ApplyBasicCareerPassives(player, ref cost, PassiveEffectType.WindsCostReduction, true);
                }
            }
            return (int)cost.ResultNumber;
        }

        //Sly : we could make this take a bool for including descriptions if we want the UI to be able to display a detailed breakdown in the future
        public float GetWindsRechargeRate(CharacterObject baseCharacter)
        {
            var hero = baseCharacter?.HeroObject;
            if (hero == null || !hero.IsSpellCaster()) return 0f;
            if (baseCharacter.Culture.StringId == TORConstants.Cultures.DAWI || baseCharacter.Culture.StringId == TORConstants.Cultures.GREENSKIN) return 0f;
            if (hero.PartyBelongedTo != MobileParty.MainParty) return 2f;//equiv to 267 spellcraft

            ExplainedNumber explainedNumber = new(1f, false, null);
            SkillHelper.AddSkillBonusForCharacter(TORSkillEffects.WindsRechargeRate, baseCharacter, ref explainedNumber);

            //PartyBelongedTo is necessarily not null here due to the "!= MobileParty.MainParty" condition. If a hero is a prisoner, sitting in a town alone, etc..., they use the default npc value above.
            if (MobileParty.MainParty.HasBlessing("cult_of_isha"))
            {
                explainedNumber.AddFactor(0.25f);
            }

            if (hero == Hero.MainHero)
            {
                CareerHelper.ApplyBasicCareerPassives(hero, ref explainedNumber, PassiveEffectType.WindsRegeneration, false);

                // WellspringOfDharPassive4: +0.1 Winds regen per spellcasting companion
                if (Hero.MainHero.HasCareerChoice("WellspringOfDharPassive4"))
                {
                    var companions = Hero.MainHero.PartyBelongedTo?.GetMemberHeroes();
                    if (companions != null)
                    {
                        int spellcasterCount = 0;
                        foreach (var companion in companions)
                        {
                            if (companion != Hero.MainHero && companion.IsSpellCaster())
                            {
                                spellcasterCount++;
                            }
                        }
                        if (spellcasterCount > 0)
                        {
                            var choice = TORCareerChoices.GetChoice("WellspringOfDharPassive4");
                            explainedNumber.Add(choice.GetPassiveValue() * spellcasterCount);
                        }
                    }
                }
            }


            if (hero == Hero.MainHero && (Hero.MainHero.HasCareerChoice("ArkaynePassive1") || Hero.MainHero.HasCareerChoice("WardenOfTalsynPassive1")))
            {
                //nothing, player ignores weight penalty here
            }
            else if (hero != Hero.MainHero && hero.IsVampire())
            {
                //nothing, companion vampires can ignore weight penalties as if they were AI nobles
            }
            else
            {
                var effectiveWeight = new ExplainedNumber(baseCharacter.Equipment.GetTotalWeightOfArmor(true));
                PerkHelper.AddPerkBonusForCharacter(DefaultPerks.Athletics.FormFittingArmor, baseCharacter, true, ref effectiveWeight);

                var weightmalus = effectiveWeight.ResultNumber / 25;
                weightmalus = Mathf.Min(weightmalus, 0.85f);

                explainedNumber.AddFactor(-weightmalus);
            }
            //Sly : seems weird that you take penalties everywhere from naked to 21.75 weight, then everything after doesn't matter. This could instead be penalties from 0-40, then no regen after? Or maybe the first ~10 is no penalty, then penalties up to 0 regen at say 30-40 weight. I'm tempted to continue past 0 and go into negative WoM regen for extremely heavy armours.
            //WoM regen an explained number and being able to add the armour penalty would go a long way to making the effect of armour in particular more accessible to players.


            var WoMRegenFromEquipment = hero.GetAggregatedStatEffectFromEquipment(ItemTraitStatType.WindsOfMagicRegen);
            if (WoMRegenFromEquipment > 0)
            {
                explainedNumber.Add(WoMRegenFromEquipment, GameTexts.FindText("tor_generic_enchantedEquipment"));
            }

            //debuffs are for asrai player campaigns, not any asrai-cultured wanderer regardless of the player's culture
            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                if (!Hero.MainHero.HasAttribute("WEWandererSymbol"))
                {
                    var level = Hero.MainHero.GetForestHarmonyLevel();
                    switch (level)
                    {
                        case ForestHarmonyLevel.Harmony: break;
                        case ForestHarmonyLevel.Unbound:
                            explainedNumber.AddFactor(ForestHarmonyHelper.WindsDebuffUnbound, GameTexts.FindText("tor_forest_harmony_level", ForestHarmonyLevel.Unbound.ToString()));
                            break;
                        case ForestHarmonyLevel.Bound:
                            explainedNumber.AddFactor(ForestHarmonyHelper.WindsDebuffBound, GameTexts.FindText("tor_forest_harmony_level", ForestHarmonyLevel.Bound.ToString()));
                            break;
                    }
                }

                if (Hero.MainHero.HasAttribute("WEArielSymbol"))
                {
                    if (hero.PartyBelongedTo.InAthelLoren())
                    {
                        explainedNumber.Add(1, ForestHarmonyHelper.TreeSymbolText("WEArielSymbol"));
                    }
                    else
                    {
                        explainedNumber.Add(0.5f, ForestHarmonyHelper.TreeSymbolText("WEArielSymbol"));
                    }
                }
            }

            return explainedNumber.ResultNumber;
        }

        public float GetMaximumWindsOfMagic(CharacterObject baseCharacter)
        {
            var hero = baseCharacter?.HeroObject;
            if (hero == null || !hero.IsSpellCaster()) return 0f;
            if (hero.Culture.StringId == TORConstants.Cultures.DAWI) return 0;
            if (hero.PartyBelongedTo != MobileParty.MainParty) return 100f; //equiv to 333 spellcraft --  Sly : leaving this at 100 for the moment because the AI is dumb and wastes half of it anyways

            ExplainedNumber explainedNumber = new(10f, false, null);
            SkillHelper.AddSkillBonusForCharacter(TORSkillEffects.MaxWinds, baseCharacter, ref explainedNumber);

            //PartyBelongedTo is necessarily not null here due to the "!= MobileParty.MainParty" condition. If a hero is a prisoner, sitting in a town alone, etc..., they use the default npc value above.

            var WoMFromEquipment = hero.GetAggregatedStatEffectFromEquipment(ItemTraitStatType.WindsOfMagicMax);
            if (WoMFromEquipment > 0)
            {
                explainedNumber.Add(WoMFromEquipment, GameTexts.FindText("tor_generic_enchantedEquipment"));
            }

            if (Hero.MainHero == null) return explainedNumber.ResultNumber;
            if (Hero.MainHero.HasAnyCareer())
            {
                var careerChoices = Hero.MainHero.GetAllCareerChoices();
                if (hero == Hero.MainHero)
                {
                    CareerHelper.ApplyBasicCareerPassives(hero, ref explainedNumber, PassiveEffectType.WindsOfMagic, false);
                    if (careerChoices.Contains("DarkVisionPassive4"))
                    {
                        var spellCount = Hero.MainHero.GetExtendedInfo().AcquiredAbilities.Count; //does acquired abilities include ones known at game start?
                        var choice = TORCareerChoices.GetChoice("DarkVisionPassive4");
                        explainedNumber.Add(choice.GetPassiveValue() * spellCount);
                    }

                    if (careerChoices.Contains("DiscipleOfAccursedPassive4"))
                    {
                        var characterEquipment = baseCharacter.GetCharacterEquipment();
                        var choice = TORCareerChoices.GetChoice("DiscipleOfAccursedPassive4");
                        var traitCount = 0;
                        foreach (var item in characterEquipment)
                        {
                            traitCount += item.GetTraits().Count;
                        }
                        if (traitCount > 0)
                        {
                            explainedNumber.Add(choice.GetPassiveValue() * traitCount);
                        }
                    }

                    if (careerChoices.Contains("ArcaneKnowledgePassive4"))
                    {
                        var heroes = hero.PartyBelongedTo.GetMemberHeroes();
                        heroes.Remove(Hero.MainHero);
                        var choice = TORCareerChoices.GetChoice("ArcaneKnowledgePassive4");

                        foreach (var member in heroes)
                        {
                            if (member.IsImperialMagister())
                            {
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
                        }
                    }

                    // HeartOfTheTreePassive4: +0.5 max Winds per tree spirit unit in party
                    if (careerChoices.Contains("HeartOfTheTreePassive4"))
                    {
                        var party = hero.PartyBelongedTo;
                        if (party?.MemberRoster != null)
                        {
                            int treeSpiritCount = 0;
                            foreach (var element in party.MemberRoster.GetTroopRoster())
                            {
                                if (element.Character != null && element.Character.IsTreeSpirit())
                                {
                                    treeSpiritCount += element.Number;
                                }
                            }
                            if (treeSpiritCount > 0)
                            {
                                var choice = TORCareerChoices.GetChoice("HeartOfTheTreePassive4");
                                explainedNumber.Add(choice.GetPassiveValue() * treeSpiritCount);
                            }
                        }
                    }
                }
                else
                {
                    if (careerChoices.Contains("EnvoyOfTheLadyPassive3") && hero.HasAttribute("PriestLady"))
                    {
                        var choice = TORCareerChoices.GetChoice("EnvoyOfTheLadyPassive3");
                        explainedNumber.Add(choice.GetPassiveValue());
                    }

                    if (careerChoices.Contains("LieOfLadyPassive2") && hero.IsNecromancer())//this applies to vamp companions as well because IsNecro also looks for the lore
                    {
                        var choice = TORCareerChoices.GetChoice("LieOfLadyPassive2");
                        explainedNumber.Add(choice.GetPassiveValue());
                    }

                    if (careerChoices.Contains("CollegeOrdersPassive2") && hero.IsImperialMagister())
                    {
                        var choice = TORCareerChoices.GetChoice("CollegeOrdersPassive2");
                        explainedNumber.Add(choice.GetPassiveValue());
                    }

                    if (careerChoices.Contains("WellspringOfDharPassive3") && hero.IsNecromancer())
                    {
                        var choice = TORCareerChoices.GetChoice("WellspringOfDharPassive3");
                        explainedNumber.Add(choice.GetPassiveValue());
                    }

                    if (careerChoices.Contains("WardenOfArgwylonPassive4") && hero.IsSpellSinger()) //wardens can't be spellsingers so no need to check player
                    {
                        var choice = TORCareerChoices.GetChoice("WardenOfArgwylonPassive4");
                        explainedNumber.Add(choice.GetPassiveValue());
                    }

                    // Orc Shaman: +30 WoM for Shaman companion
                    if (careerChoices.Contains("PowerUvDaWaaaghPassive2") && hero.HasAttribute("ShamanBoss"))
                    {
                        var choice = TORCareerChoices.GetChoice("PowerUvDaWaaaghPassive2");
                        explainedNumber.Add(choice.GetPassiveValue());
                    }
                }

                if (Hero.MainHero.HasCareer(TORCareers.ImperialMagister)) //Sly : penalizes all mages in party, don't care because it requires off-culture companions to occur - idk what the description is for powerstones
                {
                    var stoneBehavior =
                        CareerButtons.Instance.GetCareerButton(TORCareers.ImperialMagister) as
                            ImperialMagisterCareerButtonBehavior;

                    var powerstones = stoneBehavior.GetAllPowerstones();

                    var reserved = powerstones.Sum(pair => (pair.Upkeep));

                    explainedNumber.Add(-reserved);
                }
            }

            if (Hero.MainHero.Culture.StringId == TORConstants.Cultures.ASRAI)
            {
                if (Hero.MainHero.HasAttribute("WEArielSymbol"))
                {
                    if (hero.PartyBelongedTo.InAthelLoren())
                    {
                        explainedNumber.Add(15, ForestHarmonyHelper.TreeSymbolText("WEArielSymbol"));
                    }
                    else
                    {
                        explainedNumber.Add(5, ForestHarmonyHelper.TreeSymbolText("WEArielSymbol"));
                    }
                }
            }

            return explainedNumber.ResultNumber;
        }

        public bool IsValidLoreForCharacter(Hero hero, LoreObject loreObject)
        {
            if (!hero.IsVampire() && loreObject.IsRestrictedToVampires) return false;

            if (hero.HasCareer(TORCareers.Necrarch))
            {
                if (loreObject.ID == "LoreOfLife" || loreObject.ID == "LoreOfLight" || loreObject.ID == "HighMagic") return false;
                if (hero.HasUnlockedCareerChoiceTier(3))
                    if (!hero.HasKnownLore("DarkMagic") && loreObject.ID != "DarkMagic")
                        return false;

                return true;
            }
            return !loreObject.DisabledForCultures.Contains(hero.Culture.StringId);
        }

        /// <summary>
        /// Calculates final ability damage with all modifiers applied.
        /// Call this for spells/prayers that deal damage (positive DamageAmount).
        /// </summary>
        public int CalculateAbilityDamage(Agent attacker, Agent victim, int baseDamage, DamageType damageType, AbilityTemplate abilityTemplate)
        {
            if (attacker == null || victim == null || baseDamage <= 0)
                return baseDamage;

            var damageModel = MissionGameModels.Current?.AgentApplyDamageModel as TORAgentApplyDamageModel;
            if (damageModel == null)
                return baseDamage;

            // Get property containers
            var attackerPropertyContainer = damageModel.CreateAgentPropertyContainer(attacker, PropertyMask.Attack, AttackTypeMask.Spell);
            var victimPropertyContainer = damageModel.CreateAgentPropertyContainer(victim, PropertyMask.Defense, AttackTypeMask.Spell);

            var damageAmplifications = attackerPropertyContainer.DamagePercentages;
            var additionalDamagePercentages = attackerPropertyContainer.AdditionalDamagePercentages;
            var resistancePercentages = victimPropertyContainer.ResistancePercentages;

            bool friendlyFire = attacker.Team == victim.Team;
            int damageTypeIndex = (int)damageType;
            float resultDamage = baseDamage;

            // Apply career passives for damage values
            TORDamageHelper.ApplyCareerPassives(attacker, victim, AttackTypeMask.Spell, additionalDamagePercentages, resistancePercentages);

            // Apply damage modifiers (virtual hook)
            resultDamage = ApplyDamageModifiers(resultDamage, attacker, victim, damageType, abilityTemplate, damageAmplifications, additionalDamagePercentages, resistancePercentages);

            // Calculate ward save
            float wardSaveFactor = damageModel.CalculateWardSaveFactor(victim, resistancePercentages, friendlyFire);

            // Apply amplifications and resistances
            damageAmplifications[damageTypeIndex] += additionalDamagePercentages[damageTypeIndex];
            damageAmplifications[damageTypeIndex] -= resistancePercentages[damageTypeIndex];
            resultDamage *= (1 + damageAmplifications[damageTypeIndex]);

            // Apply ward save
            resultDamage *= wardSaveFactor;
            
            return Math.Max(0, (int)resultDamage);
        }

        /// <summary>
        /// Calculates final ability healing with all modifiers applied.
        /// Call this for spells/prayers that heal (negative DamageAmount in XML).
        /// </summary>
        public int CalculateAbilityHealing(Agent caster, Agent target, int baseHealing, AbilityTemplate abilityTemplate)
        {
            if (caster == null || target == null || baseHealing <= 0)
                return baseHealing;

            float resultHealing = baseHealing;

            // Apply healing modifiers (virtual hook)
            resultHealing = ApplyHealingModifiers(resultHealing, caster, target, abilityTemplate);

            return Math.Max(0, (int)resultHealing);
        }

        /// <summary>
        /// Virtual hook for applying damage modifiers. Override to customize damage calculation.
        /// </summary>
        protected virtual float ApplyDamageModifiers(
            float damage,
            Agent attacker,
            Agent victim,
            DamageType damageType,
            AbilityTemplate abilityTemplate,
            float[] damageAmplifications,
            float[] additionalDamagePercentages,
            float[] resistancePercentages)
        {
            if (Game.Current.GameType is not Campaign)
                return damage;

            int damageTypeIndex = (int)damageType;

            if (abilityTemplate != null && attacker.IsHero)
            {
                var hero = attacker.GetHero();
                if (hero != null)
                {
                    // Perk effects multiplier
                    damage *= GetPerkEffectsOnAbilityDamage(hero.CharacterObject, victim, abilityTemplate);

                    // Skill effectiveness multiplier
                    damage *= GetSkillEffectivenessForAbilityDamage(hero.CharacterObject, abilityTemplate);

                    // Career-specific bonuses
                    if (hero.HasAnyCareer())
                    {
                        if (hero.HasCareerChoice("EverlingsSecretPassive4"))
                        {
                            for (int i = (int)DamageType.Magical; i < (int)DamageType.All; i++)
                            {
                                if (i == damageTypeIndex) continue;
                                damageAmplifications[damageTypeIndex] += additionalDamagePercentages[i];
                                damageAmplifications[damageTypeIndex] += damageAmplifications[i];
                            }
                        }
                    }

                    if (hero.PartyBelongedTo == MobileParty.MainParty)
                    {
                        if (MobileParty.MainParty.LeaderHero.HasAnyCareer())
                        {
                            if (Hero.MainHero.HasCareerChoice("AncientScrollsPassive4"))
                            {
                                damageAmplifications[damageTypeIndex] += 0.2f;
                            }

                            if (Hero.MainHero.HasCareerChoice("ArcaneKnowledgePassive1") && hero != Hero.MainHero)
                            {
                                damageAmplifications[damageTypeIndex] += 0.1f;
                            }
                        }
                    }

                    // Note: Using temporary attribute instead of StatusEffect DamageAmplification because
                    // the DamageAmplification system doesn't properly handle damage_type="All" - it stores
                    // the value at the "All" index but damage calculations only read specific damage type indices.
                    if (attacker.HasAttribute("Arcane_Dmg"))
                    {
                        damageAmplifications[damageTypeIndex] += 0.3f;
                    }
                }
            }

            return damage;
        }

        /// <summary>
        /// Virtual hook for applying healing modifiers. Override to customize healing calculation.
        /// </summary>
        protected virtual float ApplyHealingModifiers(float healing, Agent caster, Agent target, AbilityTemplate abilityTemplate)
        {
            if (Game.Current.GameType is not Campaign)
                return healing;

            if (abilityTemplate != null && caster.IsHero)
            {
                var hero = caster.GetHero();
                if (hero != null)
                {
                    // Perk effects multiplier (includes Overcaster/EfficientSpellCaster)
                    healing *= GetPerkEffectsOnAbilityDamage(hero.CharacterObject, target, abilityTemplate);

                    // Skill effectiveness for healing
                    healing *= GetSkillEffectivenessForAbilityDamage(hero.CharacterObject, abilityTemplate);
                }
            }

            return healing;
        }

        /// <summary>
        /// Applies spell damage to a group of agents with aggregate tracking.
        /// Handles damage calculation and application. XP and display are handled when session is collected.
        /// </summary>
        public void ApplySpellDamageToAgents(
            IEnumerable<Agent> agents,
            int minDamage,
            int maxDamage,
            Agent caster,
            DamageType damageType,
            AbilityTemplate abilityTemplate,
            TriggeredEffectTemplate triggeredEffectTemplate,
            bool hasShockWave,
            Vec3 impactPosition,
            int castId = -1)
        {
            if (agents == null || caster == null) return;

            var logic = Mission.Current?.GetMissionBehavior<AbilitySystem.AbilityManagerMissionLogic>();

            foreach (var agent in agents)
            {
                if (agent == null) continue;

                // Calculate base damage with variance
                var baseDamage = maxDamage < minDamage ? minDamage : MBRandom.RandomInt(minDamage, maxDamage);
                if (baseDamage <= 0) continue;

                // Apply radius falloff for shockwave effects
                if (impactPosition != default && hasShockWave && triggeredEffectTemplate != null)
                {
                    var distance = agent.Position.Distance(impactPosition);
                    baseDamage = (int)((triggeredEffectTemplate.Radius - distance) / triggeredEffectTemplate.Radius * baseDamage);
                }

                if (baseDamage <= 0) continue;

                // Calculate final damage with all modifiers
                int finalDamage = CalculateAbilityDamage(caster, agent, baseDamage, damageType, abilityTemplate);

                if (finalDamage > 0)
                {
                    // Apply the damage
                    agent.ApplyDamage(finalDamage, impactPosition, caster, doBlow: true, hasShockWave: hasShockWave, originatesFromAbility: abilityTemplate != null);

                    // Book damage to session if we have a valid castId
                    if (castId >= 0 && logic != null)
                    {
                        logic.BookSpellDamage(castId, agent, finalDamage, 0, damageType);

                        // Track kill if the agent died from this damage
                        if (agent.Health <= 0 || agent.State == AgentState.Killed || agent.State == AgentState.Unconscious)
                        {
                            logic.BookSpellKill(castId, agent);
                        }
                    }
                    // Note: Career ability charge is applied through OnAgentHit when RegisterBlow is called
                }
            }
        }

        /// <summary>
        /// Applies spell healing to a group of agents with aggregate tracking.
        /// Handles healing application. XP and display are handled when session is collected.
        /// </summary>
        public void ApplySpellHealingToAgents(
            IEnumerable<Agent> agents,
            int minHeal,
            int maxHeal,
            Agent healer,
            AbilityTemplate abilityTemplate,
            int castId = -1)
        {
            if (agents == null) return;

            var logic = Mission.Current?.GetMissionBehavior<AbilitySystem.AbilityManagerMissionLogic>();

            foreach (var agent in agents)
            {
                if (agent == null) continue;

                var baseHealing = minHeal;
                if (maxHeal >= minHeal)
                {
                    baseHealing = MBRandom.RandomInt(minHeal, maxHeal);
                }

                if (baseHealing <= 0) continue;

                // Calculate final healing with modifiers
                int finalHealing = CalculateAbilityHealing(healer, agent, baseHealing, abilityTemplate);

                if (finalHealing > 0)
                {
                    agent.Heal(finalHealing);

                    // Book healing to session if we have a valid castId
                    if (castId >= 0 && logic != null)
                    {
                        logic.BookSpellHealing(castId, agent, finalHealing);
                    }

                    // Apply career ability charge
                    if (CareerHelper.IsValidCareerMissionInteractionBetweenAgents(healer, agent))
                    {
                        CareerHelper.ApplyCareerAbilityCharge(finalHealing, ChargeType.Healed, AttackTypeMask.Spell, healer, agent);
                    }
                }
            }
        }
    }
}