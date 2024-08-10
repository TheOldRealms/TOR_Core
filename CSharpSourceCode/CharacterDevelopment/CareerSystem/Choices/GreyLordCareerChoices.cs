using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Models;
using TOR_Core.Utilities;


    namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
    {
        public class GreyLordCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
        {
            private CareerChoiceObject _greyLordRoot;
            
            private CareerChoiceObject _caelithsWisdomPassive1;
            private CareerChoiceObject _caelithsWisdomPassive2;
            private CareerChoiceObject _caelithsWisdomPassive3;
            private CareerChoiceObject _caelithsWisdomPassive4;
            private CareerChoiceObject _caelithsWisdomKeystone;

            private CareerChoiceObject _secretOfForestDragonPassive1;
            private CareerChoiceObject _secretOfForestDragonPassive2;
            private CareerChoiceObject _secretOfForestDragonPassive3;
            private CareerChoiceObject _secretOfForestDragonPassive4;
            private CareerChoiceObject _secretOfForestDragonKeystone;

            private CareerChoiceObject _legendsOfMalokPassive1;
            private CareerChoiceObject _legendsOfMalokPassive2;
            private CareerChoiceObject _legendsOfMalokPassive3;
            private CareerChoiceObject _legendsOfMalokPassive4;
            private CareerChoiceObject _legendsOfMalokKeystone;

            private CareerChoiceObject _secretOfSunDragonPassive1;
            private CareerChoiceObject _secretOfSunDragonPassive2;
            private CareerChoiceObject _secretOfSunDragonPassive3;
            private CareerChoiceObject _secretOfSunDragonPassive4;
            private CareerChoiceObject _secretOfSunDragonKeystone;

            private CareerChoiceObject _secretOfStarDragonPassive1;
            private CareerChoiceObject _secretOfStarDragonPassive2;
            private CareerChoiceObject _secretOfStarDragonPassive3;
            private CareerChoiceObject _secretOfStarDragonPassive4;
            private CareerChoiceObject _secretOfStarDragonKeystone;

            private CareerChoiceObject _secretOfMoonDragonPassive1;
            private CareerChoiceObject _secretOfMoonDragonPassive2;
            private CareerChoiceObject _secretOfMoonDragonPassive3;
            private CareerChoiceObject _secretOfMoonDragonPassive4;
            private CareerChoiceObject _secretOfMoonDragonKeystone;

            private CareerChoiceObject _secretOfFellfangPassive1;
            private CareerChoiceObject _secretOfFellfangPassive2;
            private CareerChoiceObject _secretOfFellfangPassive3;
            private CareerChoiceObject _secretOfFellfangPassive4;
            private CareerChoiceObject _secretOfFellfangKeystone;


            protected override void RegisterAll()
            {
                _greyLordRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GreyLordRoot"));
                
                _caelithsWisdomPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive1).UnderscoreFirstCharToUpper()));
                _caelithsWisdomPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive2).UnderscoreFirstCharToUpper()));
                _caelithsWisdomPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive3).UnderscoreFirstCharToUpper()));
                _caelithsWisdomPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomPassive4).UnderscoreFirstCharToUpper()));
                _caelithsWisdomKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_caelithsWisdomKeystone).UnderscoreFirstCharToUpper()));

                _secretOfForestDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfForestDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfForestDragonKeystone).UnderscoreFirstCharToUpper()));

                _legendsOfMalokPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive1).UnderscoreFirstCharToUpper()));
                _legendsOfMalokPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive2).UnderscoreFirstCharToUpper()));
                _legendsOfMalokPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive3).UnderscoreFirstCharToUpper()));
                _legendsOfMalokPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokPassive4).UnderscoreFirstCharToUpper()));
                _legendsOfMalokKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_legendsOfMalokKeystone).UnderscoreFirstCharToUpper()));

                _secretOfSunDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfSunDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfSunDragonKeystone).UnderscoreFirstCharToUpper()));

                _secretOfStarDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfStarDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfStarDragonKeystone).UnderscoreFirstCharToUpper()));

                _secretOfMoonDragonPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive1).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive2).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive3).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonPassive4).UnderscoreFirstCharToUpper()));
                _secretOfMoonDragonKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfMoonDragonKeystone).UnderscoreFirstCharToUpper()));

                _secretOfFellfangPassive1 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive1).UnderscoreFirstCharToUpper()));
                _secretOfFellfangPassive2 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive2).UnderscoreFirstCharToUpper()));
                _secretOfFellfangPassive3 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive3).UnderscoreFirstCharToUpper()));
                _secretOfFellfangPassive4 =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangPassive4).UnderscoreFirstCharToUpper()));
                _secretOfFellfangKeystone =
                    Game.Current.ObjectManager.RegisterPresumedObject(
                        new CareerChoiceObject(nameof(_secretOfFellfangKeystone).UnderscoreFirstCharToUpper()));


            }

            protected override void InitializeKeyStones()
            {
                
                _greyLordRoot.Initialize(CareerID, "root", null, true,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
 
                    });
                _caelithsWisdomKeystone.Initialize(CareerID, "Melee attacks can charge career ability. Ability starts charged.", "CaelithsWisdom", false,
                    ChoiceType.Passive);
                _secretOfForestDragonKeystone.Initialize(CareerID, "Burning effects are increased by 100% in duration. Cityborn can charge ability.", "SecretOfForestDragon", false,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(TriggeredEffectTemplate),
                            MutationTargetOriginalId = "apply_dragonfire",
                            PropertyName = "ImbuedStatusEffectDuration",
                            PropertyValue = (choice, originalValue, agent) => 2f,
                            MutationType = OperationType.Multiply
                        }
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _legendsOfMalokKeystone.Initialize(CareerID, "Range of dragon breath is increased. Cityborn can charge ability.", "LegendsOfMalok", false,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "DragonFire",
                            PropertyName = "Offset",
                            PropertyValue = (choice, originalValue, agent) => 5f,
                            MutationType = OperationType.Replace
                        },
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(AbilityTemplate),
                            MutationTargetOriginalId = "DragonFire",
                            PropertyName = "TargetCapturingRadius",
                            PropertyValue = (choice, originalValue, agent) => 3f,
                            MutationType = OperationType.Replace
                        },
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(TriggeredEffectTemplate),
                            MutationTargetOriginalId = "apply_dragonfire",
                            PropertyName = "Radius",
                            PropertyValue = (choice, originalValue, agent) => 4f,
                            MutationType = OperationType.Replace
                        },
                        
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _secretOfSunDragonKeystone.Initialize(CareerID, "Impact damage of dragon fire is doubled.", "SecretOfSunDragon", false,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(TriggeredEffectTemplate),
                            MutationTargetOriginalId = "apply_dragonfire",
                            PropertyName = "DamageAmount",
                            PropertyValue = (choice, originalValue, agent) => 2f,
                            MutationType = OperationType.Multiply
                        },
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                
                _secretOfStarDragonKeystone.Initialize(CareerID, "Dragon breath slows movement and swing speed of all enemy units.", "SecretOfStarDragon", false,
                    ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                    {
                        new CareerChoiceObject.MutationObject()
                        {
                            MutationTargetType = typeof(TriggeredEffectTemplate),
                            MutationTargetOriginalId = "apply_dragonfire",
                            PropertyName = "ImbuedStatusEffects",
                            PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] {"dragonfire_debuff_mov","dragonfire_debuff_ats" }).ToList(),
                            MutationType = OperationType.Replace
                        },
                    },new CareerChoiceObject.PassiveEffect(0,PassiveEffectType.Special));
                _secretOfMoonDragonKeystone.Initialize(CareerID, "For every hit enemy by dragon breath, increase magical spell damage by 3% for 10 seconds.", "SecretOfMoonDragon", false,
                    ChoiceType.Passive);
                
                _secretOfFellfangKeystone.Initialize(CareerID, "Every eliminated unit of dragon fire (within 5 seconds), will recharge 0.25 winds.", "SecretOfFellfang", false,
                    ChoiceType.Passive);
            }

            protected override void InitializePassives()
            {
                _caelithsWisdomPassive1.Initialize(CareerID, "Increases Hitpoints by 25.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health)); 
                _caelithsWisdomPassive2.Initialize(CareerID, "{=avatar_of_death_passive1_str}Gain 25% fire resistance.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Fire, 25), AttackTypeMask.All));
                _caelithsWisdomPassive3.Initialize(CareerID, "Cityborn upkeep is reduced by 25%.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.TroopWages,true, 
                    characterObject => characterObject.IsEliteTroop() && characterObject.Culture.StringId == TORConstants.Cultures.EONIR));
                _caelithsWisdomPassive4.Initialize(CareerID, "Cityborn troops gain 50% fire resistance.", "CaelithsWisdom", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Fire, 50), AttackTypeMask.All, 
                    (attacker, victim, mask) => victim.BelongsToMainParty()&& !victim.IsHero && victim.Character.Culture.StringId == TORConstants.Cultures.EONIR ));
                
                _secretOfForestDragonPassive1.Initialize(CareerID, "Increases maximum winds of magic capacities by 10.", "SecretOfForestDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
                _secretOfForestDragonPassive2.Initialize(CareerID, "{=vivid_visions_passive3_str}Increases Magic resistance against spells by 25%.", "SecretOfForestDragon", false, ChoiceType.Passive, null,new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical,25),AttackTypeMask.Spell));
                _secretOfForestDragonPassive2.Initialize(CareerID, "Wounded troops in your party heal faster.", "SecretOfForestDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration)); 
                _secretOfForestDragonPassive4.Initialize(CareerID, "Not wielding projectile spells increase spell duration for healing spells by 50%", "SecretOfForestDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special)); 
                
                _legendsOfMalokPassive1.Initialize(CareerID, "Increases maximum winds of magic capacities by 10.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
                _legendsOfMalokPassive2.Initialize(CareerID, "Favor costs for cityborn  troop upgrades is reduced by 20%.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.CustomResourceUpgradeCostModifier,true));
                _legendsOfMalokPassive3.Initialize(CareerID, "Adds 25% fire damage to all troops.", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Fire, 25), AttackTypeMask.Melee, 
                    (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsHero && attacker.Character.IsEliteTroop() &&  attacker.Character.Culture.StringId == TORConstants.Cultures.EONIR));
                _legendsOfMalokPassive4.Initialize(CareerID, "If no hex spells are equipped you gain 50% spell radius.\n", "LegendsOfMalok", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special)); 

                
                _secretOfSunDragonPassive1.Initialize(CareerID, "Increases maximum winds of magic capacities by 15.", "SecretOfSunDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
                _secretOfSunDragonPassive2.Initialize(CareerID, "Spell effect radius is increased by 20%.", "SecretOfSunDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.SpellRadius,true));
                _secretOfSunDragonPassive3.Initialize(CareerID, "Increases fire spell damage by 10%.", "SecretOfSunDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Fire, 10), AttackTypeMask.Spell));
                _secretOfSunDragonPassive4.Initialize(CareerID, "If no AOE effect is equipped, your projectile spells deal 200% extra damage.", "SecretOfSunDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special)); 
                
                _secretOfStarDragonPassive1.Initialize(CareerID, "Increases Windsregeneration by 0.5.", "SecretOfStarDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.WindsRegeneration));
                _secretOfStarDragonPassive2.Initialize(CareerID, "Increase hex durations by 20%.", "SecretOfStarDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.DebuffDuration,true)); 
                _secretOfStarDragonPassive3.Initialize(CareerID, "Increases maximum winds of magic capacities by 15.", "SecretOfStarDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.WindsOfMagic));
                _secretOfStarDragonPassive4.Initialize(CareerID, "Not wielding any healing spells increase hex duration by 50%", "SecretOfStarDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));
                
                _secretOfMoonDragonPassive1.Initialize(CareerID, "Extra 25% Wardsave if your armor weight does not exceed 11 weight.", "SecretOfMoonDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 15), AttackTypeMask.Spell,
                    (attacker, victim, attackmask) => attacker.IsMainAgent && CareerChoicesHelper.ArmorWeightUndershootCheck(attacker,11)));
                _secretOfMoonDragonPassive2.Initialize(CareerID, "Increases Lightning spell damage by 10%.", "SecretOfMoonDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Lightning, 10), AttackTypeMask.Spell));
                _secretOfMoonDragonPassive3.Initialize(CareerID, "Increases magic spell damage by 10%.", "SecretOfMoonDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
                _secretOfMoonDragonPassive4.Initialize(CareerID, "If no buff spells are equipped, duration of vortex and area effects are doubled.", "SecretOfMoonDragon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));
                
                _secretOfFellfangPassive1.Initialize(CareerID, "50% cooldown reduction if you wield less than 11 spells", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(11, PassiveEffectType.Special));
                _secretOfFellfangPassive2.Initialize(CareerID, "Increases maximum winds of magic capacities by 15.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.WindsOfMagic));
                _secretOfFellfangPassive3.Initialize(CareerID, "After battle, 30% of your winds are regenerated.", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.Special));
                _secretOfFellfangPassive4.Initialize(CareerID, "For every spell dealing fire damage, 20% chance the used spell doesn't cost any winds", "SecretOfFellfang", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special,true));
                

            }
        }
    }