using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.TwoDimension;
using TOR_Core.AbilitySystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.CharacterDevelopment.CareerSystem.Choices;
using TOR_Core.Extensions;
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
                _ => TORSkills.Spellcraft,
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
                    if (Hero.MainHero.HasCareerChoice("UnrestrictedMagicPassive4"))
                    {
                        if (Agent.Main != null)
                        {
                            var comp = Agent.Main.GetComponent<AbilityComponent>();
                            if (!CareerChoicesHelper.ContainsSpellType(comp, [AbilityTargetType.AlliesInAOE, AbilityTargetType.EnemiesInAOE, AbilityTargetType.GroundAtPosition]))
                            {
                                explainedNumber.AddFactor(2);
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
                        if (playerHero.HasCareerChoice("LegendsOfMalokPassive4")) //this can go into BasicCareerPassives if the perk was set up differently
                        {
                            var count = Agent.Main.GetAbilities().Count; //Sly : this is wrong - description states equipped spells, not known ones
                            if (!CareerChoicesHelper.ContainsSpellType(Agent.Main.GetComponent<AbilityComponent>(), count, AbilityEffectType.Hex))
                            {
                                explainedNumber.AddFactor(0.25f); //value is wrong, PassiveEffect should be corrected, then access it's value here
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
                    }
                }


                //Sly : these are probably safe outside the Agent.Main check because they draw from the CharacterObject which exists regardless of the agent being dead/removed from the agent array
                var equipment = character.GetCharacterEquipment();

                foreach (var trait in equipment.Select(item => item.GetTraits()).SelectMany(traits => traits.WhereQ(trait => trait.StatsTuple?.StatType == ItemTraitStatType.SpellRadius)))
                {
                    explainedNumber.AddFactor(trait.StatsTuple.Value);
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
                    if (template.AbilityEffectType == AbilityEffectType.Heal && Hero.MainHero.HasCareerChoice("SoulBindingPassive4"))
                    {
                        var comp = Agent.Main.GetComponent<AbilityComponent>();
                        if (comp != null)
                        {
                            var count = Agent.Main.GetAbilities().Count;
                            if (!CareerChoicesHelper.ContainsSpellType(comp, count, AbilityEffectType.Projectile))
                            {
                                explainedNumber.AddFactor(0.5f);
                            }
                        }
                    }

                    if (template.AbilityEffectType == AbilityEffectType.Hex && Hero.MainHero.HasCareerChoice("ForbiddenScrollsOfSapheryPassive4"))
                    {
                        var comp = Agent.Main.GetComponent<AbilityComponent>();
                        if (comp != null)
                        {
                            var count = Agent.Main.GetAbilities().Count;
                            if (!CareerChoicesHelper.ContainsSpellType(comp, count, AbilityEffectType.Heal))
                            {
                                explainedNumber.AddFactor(0.5f);
                            }
                        }
                    }

                    if (template.AbilityEffectType == AbilityEffectType.Vortex || template.AbilityEffectType == AbilityEffectType.Bombardment && Hero.MainHero.HasCareerChoice("ByAllMeansPassive4"))
                    {
                        var comp = Agent.Main.GetComponent<AbilityComponent>();
                        if (comp != null)
                        {
                            var count = Agent.Main.GetAbilities().Count;
                            if (!CareerChoicesHelper.ContainsSpellType(comp, count, AbilityEffectType.Augment))
                            {
                                explainedNumber.AddFactor(0.5f);
                            }
                        }
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
                if (character.GetPerkValue(TORPerks.Spellcraft.OverCaster) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
                {
                    PerkHelper.AddPerkBonusForCharacter(TORPerks.Spellcraft.OverCaster, character, true, ref explainedNumber);
                }
                if (character.GetPerkValue(TORPerks.Spellcraft.EfficientSpellCaster) && abilityTemplate.IsSpell && abilityTemplate.DoesDamage)
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
                        foreach (var item in characterEquipment)
                        {
                            if (item.IsMagicalItem())
                            {
                                explainedNumber.Add(choice.GetPassiveValue());
                            }
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
                        explainedNumber.Add(20, ForestHarmonyHelper.TreeSymbolText("WEArielSymbol"));
                    }
                    else
                    {
                        explainedNumber.Add(10, ForestHarmonyHelper.TreeSymbolText("WEArielSymbol"));
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
    }
}