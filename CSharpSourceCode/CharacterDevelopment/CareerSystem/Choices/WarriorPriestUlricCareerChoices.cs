using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class WarriorPriestUlricCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
        private CareerChoiceObject _warriorPriestUlricRoot;
        private CareerChoiceObject _crusherOfTheWeakKeystone;
        private CareerChoiceObject _crusherOfTheWeakPassive1;
        private CareerChoiceObject _crusherOfTheWeakPassive2;
        private CareerChoiceObject _crusherOfTheWeakPassive3;
        private CareerChoiceObject _crusherOfTheWeakPassive4;

        private CareerChoiceObject _wildPackKeystone;
        private CareerChoiceObject _wildPackPassive1;
        private CareerChoiceObject _wildPackPassive2;
        private CareerChoiceObject _wildPackPassive3;
        private CareerChoiceObject _wildPackPassive4;

        private CareerChoiceObject _teachingsOfTheWinterFatherKeystone;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive1;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive2;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive3;
        private CareerChoiceObject _teachingsOfTheWinterFatherPassive4;

        private CareerChoiceObject _frostsBiteKeystone;
        private CareerChoiceObject _frostsBitePassive1;
        private CareerChoiceObject _frostsBitePassive2;
        private CareerChoiceObject _frostsBitePassive3;
        private CareerChoiceObject _frostsBitePassive4;

        private CareerChoiceObject _runesOfTheWhiteWolfKeystone;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive1;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive2;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive3;
        private CareerChoiceObject _runesOfTheWhiteWolfPassive4;

        private CareerChoiceObject _furyOfWarKeystone;
        private CareerChoiceObject _furyOfWarPassive1;
        private CareerChoiceObject _furyOfWarPassive2;
        private CareerChoiceObject _furyOfWarPassive3;
        private CareerChoiceObject _furyOfWarPassive4;

        private CareerChoiceObject _flameOfUlricKeystone;
        private CareerChoiceObject _flameOfUlricPassive1;
        private CareerChoiceObject _flameOfUlricPassive2;
        private CareerChoiceObject _flameOfUlricPassive3;
        private CareerChoiceObject _flameOfUlricPassive4;


        protected override void RegisterAll()
        {
            _warriorPriestUlricRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_warriorPriestUlricRoot).UnderscoreFirstCharToUpper()));

            _crusherOfTheWeakKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakKeystone).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive1).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive2).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive3).UnderscoreFirstCharToUpper()));
            _crusherOfTheWeakPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_crusherOfTheWeakPassive4).UnderscoreFirstCharToUpper()));

            _wildPackKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackKeystone).UnderscoreFirstCharToUpper()));
            _wildPackPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive1).UnderscoreFirstCharToUpper()));
            _wildPackPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive2).UnderscoreFirstCharToUpper()));
            _wildPackPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive3).UnderscoreFirstCharToUpper()));
            _wildPackPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_wildPackPassive4).UnderscoreFirstCharToUpper()));

            _teachingsOfTheWinterFatherKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherKeystone).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive1).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive2).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive3).UnderscoreFirstCharToUpper()));
            _teachingsOfTheWinterFatherPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_teachingsOfTheWinterFatherPassive4).UnderscoreFirstCharToUpper()));

            _frostsBiteKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBiteKeystone).UnderscoreFirstCharToUpper()));
            _frostsBitePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive1).UnderscoreFirstCharToUpper()));
            _frostsBitePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive2).UnderscoreFirstCharToUpper()));
            _frostsBitePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive3).UnderscoreFirstCharToUpper()));
            _frostsBitePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_frostsBitePassive4).UnderscoreFirstCharToUpper()));

            _runesOfTheWhiteWolfKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfKeystone).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive1).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive2).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive3).UnderscoreFirstCharToUpper()));
            _runesOfTheWhiteWolfPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_runesOfTheWhiteWolfPassive4).UnderscoreFirstCharToUpper()));

            _furyOfWarKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarKeystone).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive1).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive2).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive3).UnderscoreFirstCharToUpper()));
            _furyOfWarPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_furyOfWarPassive4).UnderscoreFirstCharToUpper()));

            _flameOfUlricKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricKeystone).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive1).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive2).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive3).UnderscoreFirstCharToUpper()));
            _flameOfUlricPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_flameOfUlricPassive4).UnderscoreFirstCharToUpper()));
        }

        protected override void InitializeKeyStones()
        {
            _warriorPriestUlricRoot.Initialize(CareerID, "Let all know the fury of the wolf god! Like Ulric and his axe Blitzbeil before you, slam your weapon into the earth, erupting the ground in a 2 meter radius. This attack cannot be parried. For every level in your highest melee weapon skill, increase Axe of Ulric's damage by 0.1%. (Ability charges by dealing melee 'Physical' damage.)", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {

                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageAmount",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.TwoHanded,DefaultSkills.OneHanded,DefaultSkills.Polearm }, 0.06f,true),
                        MutationType = OperationType.Add
                    },
                });
            _crusherOfTheWeakKeystone.Initialize(CareerID, "Axe of Ulric knocks foes down.", "CrusherOfTheWeak", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "AxeOfUlric",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] {"ulric_smash_knockdown" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                }, new CareerChoiceObject.PassiveEffect());

            _wildPackKeystone.Initialize(CareerID, "Axe of Ulric also scales with Leadership.", "WildPack", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageAmount",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Leadership }, 0.06f),
                        MutationType = OperationType.Add
                    },
                });
            _teachingsOfTheWinterFatherKeystone.Initialize(CareerID, "Axe of Ulric also scales with Faith, and has its radius increased by 1 meter.", "TeachingsOfTheWinterFather", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "Radius",
                        PropertyValue = (choice, originalValue, agent) => 1,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageAmount",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.06f),
                        MutationType = OperationType.Add
                    },
                });
            _frostsBiteKeystone.Initialize(CareerID, "Axe of Ulric now deals 'Frost' damage, and enemies are slowed for 6s when hit.", "FrostsBite", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "DamageType",
                        PropertyValue = (choice, originalValue, agent) => DamageType.Frost,
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] {"frost_bite_mov_90" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });
            _runesOfTheWhiteWolfKeystone.Initialize(CareerID, "Axe of Ulric now applies a damage over time effect to the foe.", "RunesOfTheWhiteWolf", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "ulric_smash",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "white_wolf_dot" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                });

            _furyOfWarKeystone.Initialize(CareerID, "Axe of Ulric now charges twice as fast.", "FuryOfWar", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }); // special

            _flameOfUlricKeystone.Initialize(CareerID, "When Axe of Ulric is used, a random prayer become fully charged.", "FlameOfUlric", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                });
        }

        protected override void InitializePassives()
        {
            _crusherOfTheWeakPassive1.Initialize(CareerID, "+10 personal Hitpoints.", "CrusherOfTheWeak", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
            _crusherOfTheWeakPassive2.Initialize(CareerID, "+5% personal 'Physical' melee damage.", "CrusherOfTheWeak", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee));
            _crusherOfTheWeakPassive3.Initialize(CareerID, "Deal +10% more personal 'Physical' melee damage to foes less than tier 4.", "CrusherOfTheWeak", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee,
                (attacker, victim, mask) => attacker.IsMainAgent && mask == AttackTypeMask.Melee && victim != null && !victim.IsHero && victim.Character.Level < 16));
            _crusherOfTheWeakPassive4.Initialize(CareerID, "Prayers now start charged on battle start.", "CrusherOfTheWeak", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special, true));

            _wildPackPassive1.Initialize(CareerID, "+10% personal melee 'Physical Resistance'.", "WildPack", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
            _wildPackPassive2.Initialize(CareerID, "+10 increased party size.", "WildPack", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.PartySize));
            _wildPackPassive3.Initialize(CareerID, "Personal healing rate increased by +1.", "WildPack", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.HealthRegeneration));
            _wildPackPassive4.Initialize(CareerID, "+1 party move speed on campaign map.", "WildPack", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.PartyMovementSpeed));

            _teachingsOfTheWinterFatherPassive1.Initialize(CareerID, "Wounded troops heal faster.", "TeachingsOfTheWinterFather", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.TroopRegeneration));
            _teachingsOfTheWinterFatherPassive2.Initialize(CareerID, "Praying at a shrine of Ulric, fully restores personal Hitpoints. (5-day cooldown.)", "TeachingsOfTheWinterFather", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());
            _teachingsOfTheWinterFatherPassive3.Initialize(CareerID, "Melee attacks deal increased damage to shields.", "TeachingsOfTheWinterFather", false, ChoiceType.Passive, null);
            _teachingsOfTheWinterFatherPassive4.Initialize(CareerID, "+20% ranged 'Physical Resistance' for 'Melee' troops.", "TeachingsOfTheWinterFather", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.Physical, 20), AttackTypeMask.Ranged,
                (attacker, victim, mask) => victim.BelongsToMainParty() && !(victim.IsMainAgent || victim.IsHero) && mask == AttackTypeMask.Ranged));

            _frostsBitePassive1.Initialize(CareerID, "+10% personal 'Frost' damage.", "FrostsBite", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Frost, 10), AttackTypeMask.Melee));
            _frostsBitePassive2.Initialize(CareerID, "+10% 'Frost' damage for 'Melee' troops.", "FrostsBite", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Frost, 10), AttackTypeMask.Melee));
            _frostsBitePassive3.Initialize(CareerID, "Party is no longer affected by snow penalties.", "FrostsBite", false, ChoiceType.Passive, null); //TORPartySpeedCalculatingModel
            _frostsBitePassive4.Initialize(CareerID, "+20% duration for 'Hex' prayers.", "FrostsBite", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.DebuffDuration, true));

            _runesOfTheWhiteWolfPassive1.Initialize(CareerID, "Wolf heads/pelts now provide +5% personal 'Ward Save'.", "RunesOfTheWhiteWolf", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special, true));
            _runesOfTheWhiteWolfPassive2.Initialize(CareerID, "+15 personal Hitpoints.", "RunesOfTheWhiteWolf", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Health));
            _runesOfTheWhiteWolfPassive3.Initialize(CareerID, "+20% duration for prayers.", "RunesOfTheWhiteWolf", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(20f, PassiveEffectType.BuffDuration, true));
            _runesOfTheWhiteWolfPassive4.Initialize(CareerID, "+20% 'Ward Save' for 'Ulrician' troops.", "RunesOfTheWhiteWolf", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 20), AttackTypeMask.All,
                (attacker, victim, mask) => victim.IsPlayerTroop && victim.Character.UnitBelongsToCult("ulric")));

            _furyOfWarPassive1.Initialize(CareerID, "+5% personal 'Physical' melee damage per equipped melee weapon.", "FuryOfWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special, true));
            _furyOfWarPassive2.Initialize(CareerID, "+10% personal weapon swing speed.", "FuryOfWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10f, PassiveEffectType.SwingSpeed, true));
            _furyOfWarPassive3.Initialize(CareerID, "Victory against a stronger force refreshes your blessing of Ulric.", "FuryOfWar", false, ChoiceType.Passive);
            _furyOfWarPassive4.Initialize(CareerID, "Hits below 25 damage no longer stagger you.", "FuryOfWar", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.ShruggedOff));

            _flameOfUlricPassive1.Initialize(CareerID, "Radius of prayers is increased by +25%.", "FlameOfUlric", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25f, PassiveEffectType.SpellRadius, true));
            _flameOfUlricPassive2.Initialize(CareerID, "+15% personal 'Armor Penetration' for melee attacks.", "FlameOfUlric", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _flameOfUlricPassive3.Initialize(CareerID, "Victory against a stronger force provide +100% prestige.", "FlameOfUlric", false, ChoiceType.Passive);
            _flameOfUlricPassive4.Initialize(CareerID, "Kills done by you or 'Ulrician' troops cause 25% morale damage to enemies.", "FlameOfUlric", false, ChoiceType.Passive, null,
                new CareerChoiceObject.PassiveEffect(PassiveEffectType.MoraleDamageToEnemyOnKill, new DamageProportionTuple(DamageType.All, 25), AttackTypeMask.All,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && attacker.Character.UnitBelongsToCult("ulric")));

        }

    }
}