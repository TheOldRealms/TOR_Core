using Ink.Parsed;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.BattleMechanics.TriggeredEffect.Scripts;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment.CareerSystem.CareerButton;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem
{
    public static class CareerHelper
    {
        public static float AddSkillEffectToValue(CareerChoiceObject careerChoice, Agent agent, List<SkillObject> relevantSkills, float scalingFactor, bool highestOnly = false, bool onlyWielded = false)
        {
            float skillValue = 0f;
            if (agent != null && agent.IsHero && relevantSkills != null && relevantSkills.Count > 0)
            {
                if (onlyWielded)
                {
                    var getWeaponEquipment = agent.Character.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);
                    int skillValueWielded = 0;
                    if (!getWeaponEquipment.IsEmpty())
                    {
                        var value = 0;
                        foreach (var weapon in getWeaponEquipment)
                        {
                            var skill = weapon.PrimaryWeapon.RelevantSkill;
                            if (relevantSkills.Contains(skill) && value < agent.GetHero().GetSkillValue(skill))
                            {
                                value = agent.GetHero().GetSkillValue(skill);
                            }
                        }

                        skillValueWielded = value;
                    }
                    skillValue = skillValueWielded;
                }
                else
                if (highestOnly)
                {
                    skillValue = relevantSkills.Max(x => agent.GetHero().GetSkillValue(x));
                }
                else
                    foreach (var skill in relevantSkills)
                    {
                        skillValue += agent.GetHero().GetSkillValue(skill);

                    }

                if (careerChoice == TORCareerChoices.GetChoice("ProtectorOfTheWeakKeyStone"))
                {
                    if (agent.WieldedWeapon.Item?.PrimaryWeapon?.SwingDamageType != DamageTypes.Blunt) return 0f;
                }
            }

            return skillValue * scalingFactor;
        }

        public static bool ShruggedOffDamage(Hero hero, int damage)
        {
            var choices = hero.GetAllCareerChoices();
            return (from choiceID in choices
                    select TORCareerChoices.GetChoice(choiceID)
                into choice
                    where choice != null
                    where choice.Passive != null && choice.Passive.PassiveEffectType == PassiveEffectType.ShruggedOff
                    select choice).AnyQ(choice => choice.GetPassiveValue() < damage);
        }

        public static void ApplyBasicCareerPassives(Hero hero, ref ExplainedNumber number, PassiveEffectType passiveEffectType, AttackTypeMask mask, bool asFactor = false)
        {
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && choice.Passive.PassiveEffectType == passiveEffectType)
                {
                    var attackMask = choice.Passive.AttackTypeMask;
                    if ((mask & attackMask) == 0) //if mask does NOT contains attackmask
                        continue;

                    var value = choice.Passive.EffectMagnitude;
                    if (choice.Passive.InterpretAsPercentage)
                    {
                        value /= 100;
                    }
                    if (asFactor)
                    {
                        number.AddFactor(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                        return;
                    }
                    number.Add(value, new TextObject(choice.BelongsToGroup.Name.ToString()));
                }
            }
        }

        public static bool IsValidCareerMissionInteractionBetweenAgents(Agent affectorAgent, Agent affectedAgent)
        {
            if (Campaign.Current == null) return false;
            if (affectorAgent == null) return false;
            if (Agent.Main == null) return false;
            if (!Hero.MainHero.HasAnyCareer()) return false;
            if (affectorAgent.IsMount || affectedAgent.IsMount) return false;

            if (Hero.MainHero.HasCareer(TORCareers.Necromancer) && affectorAgent.HasAttribute("NecromancerChampion")) return true;

            return affectorAgent.BelongsToMainParty() || affectedAgent.BelongsToMainParty();
        }

        public static void ApplyCareerAbilityCharge(int amount, ChargeType chargeType, AttackTypeMask attackTypeMask, Agent affector = null, Agent affected = null, AttackCollisionData collisionData = new AttackCollisionData())
        {
            if (Agent.Main == null) return;
            var cAbility = Agent.Main.GetComponent<AbilityComponent>();
            if (cAbility != null)
            {
                var value = CalculateChargeForCareer(chargeType, amount, affector, affected, attackTypeMask, collisionData);
                if (value > 0)
                {
                    cAbility.CareerAbility.AddCharge(value);
                }
            }
        }

        public static float CalculateChargeForCareer(ChargeType chargeType, int chargeValue, Agent agent, Agent affected, AttackTypeMask mask, AttackCollisionData collisionData)
        {
            ChargeCollisionFlag flag = ChargeCollisionFlag.None;

            if (collisionData.AttackBlockedWithShield)
                flag |= ChargeCollisionFlag.HitShield;

            if (collisionData.VictimHitBodyPart == BoneBodyPartType.Head || collisionData.VictimHitBodyPart == BoneBodyPartType.Neck)
            {
                flag |= ChargeCollisionFlag.HeadShot;
            }

            if (Hero.MainHero == null || Hero.MainHero.GetCareer() == null) return 0;

            var heroCareer = Hero.MainHero.GetCareer();

            var result = heroCareer.GetCalculatedCareerAbilityCharge(agent, affected, chargeType, chargeValue, mask, flag);

            if (!result.ApproximatelyEqualsTo(0))
            {
                return result;
            }

            return 0;
        }

        public static void ApplyBasicCareerPassives(Hero hero, ref ExplainedNumber number, PassiveEffectType passiveEffectType, bool asFactor = true, CharacterObject characterObject = null)
        {
            var choices = hero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);

                if (choice?.Passive == null || choice.Passive.PassiveEffectType != passiveEffectType) continue;

                characterObject ??= hero.CharacterObject;

                var passive = choice.Passive;

                if (!passive.IsValidCharacterObject(characterObject)) continue;

                if (passive.WithFactorFlatSwitch)
                {
                    asFactor = !asFactor;
                }

                var value = passive.EffectMagnitude;
                var text = choice.BelongsToGroup.Name;

                if (passive.InterpretAsPercentage)
                {
                    value /= 100;
                }

                if (asFactor)
                {
                    number.AddFactor(value, text);
                    continue;
                }
                number.Add(value, text);
            }
        }

        public static void ApplySkillBonusForTroops(ref ExplainedNumber resultNumber, SkillObject skillObject, BasicCharacterObject troopCharacterObject)
        {
            var choices = Hero.MainHero.GetAllCareerChoices();

            if (troopCharacterObject == null)
            {
                return;
            }

            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);

                if (choice?.Passive == null || choice.Passive.PassiveEffectType != PassiveEffectType.TroopSkill) continue;

                if (!choice.Passive.IsValidCharacterObject(troopCharacterObject as CharacterObject))
                {
                    continue;
                }

                var skillEffectID = choice.Passive.TargetEffect;

                if (!skillEffectID.Contains(skillObject.StringId))
                {
                    continue;
                }

                var value = choice.Passive.EffectMagnitude;


                resultNumber.Add(value, choice.BelongsToGroup.Name);
            }
        }

        public static float[] AddCareerPassivesForDamageValues(Agent attacker, Agent victim, AttackTypeMask attackTypeMask, PropertyMask mask)
        {
            var damageValues = new float[(int)DamageType.All + 1];

            switch (mask)
            {
                case PropertyMask.Attack:
                    if (attacker.IsHero && attacker.IsMainAgent)
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.Damage);
                    }
                    else
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.TroopDamage);
                    }
                    return damageValues;
                case PropertyMask.Defense:
                    if (victim.IsHero && victim.IsMainAgent)
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.Resistance);
                    }
                    else
                    {
                        ApplyCareerPassivesForDamageValues(attacker, victim, ref damageValues, attackTypeMask, PassiveEffectType.TroopResistance);
                    }

                    return damageValues;
                default:
                    return null;
            }
        }

        private static void ApplyCareerPassivesForDamageValues(Agent agent, Agent victim, ref float[] values, AttackTypeMask attackMask, PassiveEffectType type)
        {
            if (type != PassiveEffectType.Damage &&
                type != PassiveEffectType.TroopDamage &&
                type != PassiveEffectType.Resistance &&
                type != PassiveEffectType.TroopResistance) return;

            var choices = Hero.MainHero.GetAllCareerChoices();
            foreach (var choiceID in choices)
            {
                var choice = TORCareerChoices.GetChoice(choiceID);
                if (choice == null)
                    continue;

                if (choice.Passive != null && (choice.Passive.PassiveEffectType == type))
                {
                    if (!choice.Passive.IsValidCombatInteraction(agent, victim, attackMask)) continue;
                    var passive = choice.Passive;
                    var mask = passive.AttackTypeMask;
                    if ((mask & attackMask) == 0) //if mask does NOT contains attackmask
                        continue;

                    var damageType = passive.DamageProportionTuple.DamageType;
                    values[(int)damageType] += (passive.DamageProportionTuple.Percent / 100);
                }
            }
        }


        public static float CalculateTroopWageCareerPerkEffect(TroopRosterElement troop, CareerChoiceObject careerPerk, out TextObject description)
        {
            float value = 0;
            description = new TextObject("");
            if (careerPerk == null) return 0;

            float effect = careerPerk.GetPassiveValue();
            value = (troop.Character.TroopWage * troop.Number) * effect;
            description = careerPerk.BelongsToGroup.Name;

            return value;
        }

        public static bool IsMagicCapableCareer(CareerObject career)
        {
            if (career == null) return false;

            if (career == TORCareers.Mercenary ||
                career == TORCareers.MinorVampire ||
                career == TORCareers.GrailDamsel ||
                career == TORCareers.Necromancer ||
                career == TORCareers.Necrarch)
                return true;

            return false;
        }

        /// <summary>
        /// Takes a hero and finds the attribute that corresponds to their god and returns the associated prayer list.
        /// </summary>
        /// <remarks>
        /// Assumes that a priest can only ever be the follower of a single god.
        /// 
        /// If prayers ever derive from LoreObject, this could instead be based on LoreId as every prayer has one listed in its ability template.
        /// 
        /// Sly : This is no longer career specific as it's used for granting and removing prayers when unlocking or reseting perks for any hero, but I'm undecided on where to move it in the mean time.
        /// </remarks>
        /// <returns>
        /// List of prayers based on the first valid attribute found, or an empty list if no attribute is found.
        /// </returns>
        public static List<(string PrayerID, int Rank)> GetPriestPrayerList(Hero priestHero)
        {
            List<(string PrayerID, int Rank)> prayers = new();
            var info = priestHero?.GetExtendedInfo();
            if (info == null) return prayers;
            var godAttribute = info.AllAttributes.Where(x => x.Contains("Priest") && x.ToString() != "Priest").FirstOrDefault(); //Priest attributes have 3 categories : "Priest" (sigmar and ulric careers only), "Priest(God)" (all priest careers), or "PriestTrainer" (spell trainers, for equipment blessings I believe).
            if (godAttribute == null) return prayers;

            if (godAttribute == "PriestSigmar")
            {
                prayers.Add(("HealingHand", 2));
                prayers.Add(("ArmourOfRighteousness", 3));
                prayers.Add(("Vanquish", 3));
                prayers.Add(("CometOfSigmar", 4));
                return prayers;
            }

            if (godAttribute == "PriestLady")
            {
                prayers.Add(("AuraOfTheLady", 2));
                prayers.Add(("ShieldOfCombat", 3));
                prayers.Add(("LadysFavour", 3));
                prayers.Add(("AerialShield", 4));
                return prayers;
            }

            if (godAttribute == "PriestUlric")
            {
                prayers.Add(("UlricsGift", 2));
                prayers.Add(("HeartOfTheWolf", 3));
                prayers.Add(("IceStorm", 3));
                prayers.Add(("SnowKingDecree", 4));
                return prayers;
            }

            if (godAttribute == "PriestShallya")
            {
                prayers.Add(("BlessingOfShallya", 2));
                return prayers;
            }

            return prayers;
        }

        public static bool PrayerCooldownIsNotShared(this Agent agent)
        {
            var hero = agent.GetHero();
            if (hero == null) return false;

            if (hero.HasCareerChoice("RelentlessFanaticPassive4"))
            {
                return true;
            }
            return false;
        }

        public static CareerButtonBehaviorBase GetCareerButton()
        {

            var career = Hero.MainHero.GetCareer();

            if (career != null)
            {
                return CareerButtons.Instance.GetCareerButton(career);
            }

            return null;
        }

        public static string GetButtonSprite()
        {
            var career = Hero.MainHero.GetCareer();
            if (career == null) return "";

            var button = GetCareerButton();
            if (button != null)
            {
                return CareerButtons.Instance.GetCareerButton(career).CareerButtonIcon;
            }

            return "";
        }


        public static void PowerstoneEffectAssignment(Agent agent)
        {
            //agent null checked before this method
            var statuseffectComponent = agent.GetComponent<StatusEffectComponent>();

            if (statuseffectComponent != null && GetCareerButton() is ImperialMagisterCareerButtonBehavior button)
            {
                var powerstone = button.GetPowerstone(agent.Character as CharacterObject);

                if (powerstone != null)
                {
                    AddMissionPermanentEffect(agent, powerstone.EffectId);
                }
            }
        }

        public static void AddDefaultPermanentMissionEffect(Agent agent, string effectID)
        {
            var statuseffectComponent = agent?.GetComponent<StatusEffectComponent>();

            if (statuseffectComponent != null)
            {
                AddMissionPermanentEffect(agent, effectID);
            }
        }


        private static void AddMissionPermanentEffect(Agent agent, string effectID)
        {
            var template = TriggeredEffectManager.GetTemplateWithId(effectID);

            if (template == null) return;

            foreach (var effect in template.ImbuedStatusEffects)
            {
                agent.ApplyStatusEffect(effect, Agent.Main, 99999); //null protection is present deeper down the method chain
            }

            if (template != null && template.ScriptNameToTrigger != "none")
            {
                try
                {
                    var obj = Activator.CreateInstance(Type.GetType(template.ScriptNameToTrigger));
                    if (obj is ITriggeredScript)
                    {
                        var script = obj as ITriggeredScript;
                        script.OnTrigger(agent.Position, Agent.Main, [agent], 9999); //each script has a null protection in its OnTrigger to cover this case as well as others
                    }
                }
                catch (Exception)
                {
                    TORCommon.Log("Tried to spawn TriggeredScript: " + template.ScriptNameToTrigger + ", but failed.", NLog.LogLevel.Error);
                }
            }
        }


        public enum ChargeCollisionFlag
        {
            None,
            HitShield,
            HeadShot
        }

        public static bool IsPriestCareer(CareerObject career)
        {
            return career == TORCareers.WarriorPriest ||
                   career == TORCareers.WarriorPriestUlric ||
                   career == TORCareers.GrailDamsel;
        }

        /// <summary>
        /// Removes a list of attributes containing "Priest".
        /// </summary>
        /// <remarks>
        /// Intended for bloodkiss to clear out all attributes so the player will not return IsPriest() == true unintentionally.
        /// </remarks>
        public static void RemovePriestAttributes(Hero hero)
        {
            var priestAttributes = new[]{
                "Priest",
                "PriestLady",
                "PriestSigmar",
                "PriestUlric",
                "PriestShallya"};

            foreach (var attr in priestAttributes)
            {
                hero.RemoveAttribute(attr);//no point in checking HasAttribute as RemoveAttribute does the same thing + removes it
            }
        }

        public static string GetGodCareerIsDevotedTo(CareerObject careerObject)
        {
            if (careerObject == TORCareers.GrailDamsel) return "cult_of_lady";
            if (careerObject == TORCareers.WarriorPriest) return "cult_of_sigmar";
            if (careerObject == TORCareers.WarriorPriestUlric) return "cult_of_ulric";
            return "-";
        }

        public static void RemovePowerstone(List<string> attributes)
        {
            if (GetCareerButton() is ImperialMagisterCareerButtonBehavior button)
            {
                var stones = button.AvailablePowerStones;

                foreach (var attribute in attributes)
                {
                    var removedStone = stones.FirstOrDefault(x => x.Id == attribute);

                    if (removedStone != null)
                    {
                        Hero.MainHero.AddCustomResource("Prestige", removedStone.ScrapPrestigeGain);
                        break;
                    }
                }
            }
        }

        public static ItemTrait GetTraitForReligion(Hero hero, ReligionObject religionObject)
        {
            ItemTrait result = ItemTrait.Invalid;
            var religion = Hero.MainHero.GetDominantReligion();

            if (religion == null || Hero.MainHero.GetDevotionLevelForReligion(religion) < DevotionLevel.Fanatic)
            {
                result = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knight_religion_default");
                return result ?? ItemTrait.Invalid;
            }

            switch (religion.StringId)
            {
                case "cult_of_sigmar":
                    result = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knight_religion_sigmar");
                    break;
                case "cult_of_ulric":
                    result = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knight_religion_ulric");
                    break;
                case "cult_of_taal":
                    result = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knight_religion_taal");
                    break;
                case "cult_of_manaan":
                    result = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knight_religion_manaan");
                    break;
                case "cult_of_shallya":
                    result = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == "ca_knight_religion_shallya");
                    break;
                default:
                    break;
            }

            return result ?? ItemTrait.Invalid;
        }

        public static void RemoveCareerRelatedTroopAttributes(MobileParty mobileParty, string troopId,
            MobilePartyExtendedInfo mobilePartyinfo)
        {
            if (!mobileParty.IsMainParty) return;

            if (Hero.MainHero.HasCareer(TORCareers.ImperialMagister))
            {
                RemovePowerstone(mobilePartyinfo.TroopAttributes[troopId]);
            }
        }

        public static void PuritySealAssignment(Agent agent)
        {
            MobilePartyExtendedInfo extendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);

            var button = GetCareerButton() as KnightOldWorldCareerButtonBehavior;
            var seals = button.GetAllPuritySeals();
            extendedInfo.TroopAttributes.TryGetValue(agent.Character.StringId, out var attributes);

            if (attributes == null)
            {
                return;
            }

            foreach (var seal in seals)
            {
                foreach (var attribute in attributes)
                {
                    if (attribute == seal.SealId)
                    {
                        if (seal.triggeredEffectId != null)
                        {
                            AddMissionPermanentEffect(agent, seal.triggeredEffectId);
                        }
                    }
                }
            }
        }


        public static void UnitRuneAssignment(Agent agent)
        {
            //agent null checked before this method
            MobilePartyExtendedInfo extendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
            extendedInfo.TroopAttributes.TryGetValue(agent.Character.StringId, out var attributes);
            if (attributes == null) return;

            var effectIds = RunelordCareerButtonBehavior.GetRuneIds;


            foreach (var attribute in attributes)
            {
                if (effectIds.Contains(attribute))
                {
                    AddMissionPermanentEffect(agent, attribute);
                }
            }
        }

        public static void ExtorsionAssignment(Agent agent)
        {
            MobilePartyExtendedInfo extendedInfo = ExtendedInfoManager.Instance.GetPartyInfoFor(Hero.MainHero.PartyBelongedTo.StringId);
            extendedInfo.TroopAttributes.TryGetValue(agent.Character.StringId, out var attributes);
            if (attributes == null) return;

            foreach (var attribute in attributes)
            {
                if (attribute == "Extorsion")
                {
                    var debuff = "greenskin_extorsion_debuff";
                    AddMissionPermanentEffect(agent, debuff);
                }
            }
        }
    }
}