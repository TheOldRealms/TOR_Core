using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class NecromancerCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
        private CareerChoiceObject _necromancerRoot;

        private CareerChoiceObject _liberNecrisKeystone; // 1
        private CareerChoiceObject _liberNecrisPassive1;
        private CareerChoiceObject _liberNecrisPassive2;
        private CareerChoiceObject _liberNecrisPassive3;
        private CareerChoiceObject _liberNecrisPassive4;

        private CareerChoiceObject _deArcanisKadonKeystone; // 2
        private CareerChoiceObject _deArcanisKadonPassive1;
        private CareerChoiceObject _deArcanisKadonPassive2;
        private CareerChoiceObject _deArcanisKadonPassive3;
        private CareerChoiceObject _deArcanisKadonPassive4;

        private CareerChoiceObject _codexMortificaKeystone;     //3
        private CareerChoiceObject _codexMortificaPassive1;
        private CareerChoiceObject _codexMortificaPassive2;
        private CareerChoiceObject _codexMortificaPassive3;
        private CareerChoiceObject _codexMortificaPassive4;

        private CareerChoiceObject _liberMortisKeystone; //4
        private CareerChoiceObject _liberMortisPassive1;
        private CareerChoiceObject _liberMortisPassive2;
        private CareerChoiceObject _liberMortisPassive3;
        private CareerChoiceObject _liberMortisPassive4;

        private CareerChoiceObject _bookofWsoranKeystone;        //5
        private CareerChoiceObject _bookofWsoranPassive1;
        private CareerChoiceObject _bookofWsoranPassive2;
        private CareerChoiceObject _bookofWsoranPassive3;
        private CareerChoiceObject _bookofWsoranPassive4;

        private CareerChoiceObject _grimoireNecrisKeystone; //6
        private CareerChoiceObject _grimoireNecrisPassive1;
        private CareerChoiceObject _grimoireNecrisPassive2;
        private CareerChoiceObject _grimoireNecrisPassive3;
        private CareerChoiceObject _grimoireNecrisPassive4;

        private CareerChoiceObject _booksOfNagashKeystone; // 7
        private CareerChoiceObject _booksOfNagashPassive1;
        private CareerChoiceObject _booksOfNagashPassive2;
        private CareerChoiceObject _booksOfNagashPassive3;
        private CareerChoiceObject _booksOfNagashPassive4;


        protected override void RegisterAll()
        {
            _necromancerRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NecromancerRoot"));

            _liberNecrisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisKeystone"));
            _liberNecrisPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive1"));
            _liberNecrisPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive2"));
            _liberNecrisPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive3"));
            _liberNecrisPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberNecrisPassive4"));

            _deArcanisKadonKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonKeystone"));
            _deArcanisKadonPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive1"));
            _deArcanisKadonPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive2"));
            _deArcanisKadonPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive3"));
            _deArcanisKadonPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("DeArcanisKadonPassive4"));

            _codexMortificaKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaKeystone"));
            _codexMortificaPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive1"));
            _codexMortificaPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive2"));
            _codexMortificaPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive3"));
            _codexMortificaPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("CodexMortificaPassive4"));

            _liberMortisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisKeystone"));
            _liberMortisPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive1"));
            _liberMortisPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive2"));
            _liberMortisPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive3"));
            _liberMortisPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("LiberMortisPassive4"));

            _bookofWsoranKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranKeystone"));
            _bookofWsoranPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive1"));
            _bookofWsoranPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive2"));
            _bookofWsoranPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive3"));
            _bookofWsoranPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BookofWsoranPassive4"));

            _grimoireNecrisKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisKeystone"));
            _grimoireNecrisPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive1"));
            _grimoireNecrisPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive2"));
            _grimoireNecrisPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive3"));
            _grimoireNecrisPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GrimoireNecrisPassive4"));

            _booksOfNagashKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashKeystone"));
            _booksOfNagashPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive1"));
            _booksOfNagashPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive2"));
            _booksOfNagashPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive3"));
            _booksOfNagashPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("BooksOfNagashPassive4"));


        }

        protected override void InitializeKeyStones()
        {
            _necromancerRoot.Initialize(CareerID, "What is death but an obstacle to overcome? Summon and possess an ancient Harbinger of an old age, to slay your foes! Every 2s, the Harbinger loses -5 Hitpoints. For every 3 levels in Spellcraft, the Harbinger gains 1 Hitpoint. (Ability charges by dealing 'Spell' damage, healing, summoning 'Lesser Undead' troops, or by 'Lesser Undead' troop damage.)", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "GreaterHarbinger",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Spellcraft}, 0.33f),
                        MutationType = OperationType.Add
                    }
                });

            _liberNecrisKeystone.Initialize(CareerID, "Harbinger also scales with Roguery and gains a Two-Handed weapon.", "LiberNecris", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "summon_champion",
                        PropertyName = "TroopIdToSummon",
                        PropertyValue = (choice, originalValue, agent) => originalValue+"_two_handed",
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "GreaterHarbinger",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Roguery}, 0.33f),
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect());

            _deArcanisKadonKeystone.Initialize(CareerID, "Harbinger can be un-possessed. (Re-use ability to swap between caster and Harbinger).", "DeArcanisKadon", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect());  // switch controls

            _codexMortificaKeystone.Initialize(CareerID, "Harbinger also scales with Medicine. Caster gains +90% 'Ward Save' when possessing.", "CodexMortifica", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "GreaterHarbinger",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Medicine}, 0.33f),
                        MutationType = OperationType.Add
                    }
                }, new CareerChoiceObject.PassiveEffect()); // add 90% resistence for necromancer while controlled


            _liberMortisKeystone.Initialize(CareerID, "Harbinger can be summoned on battle start and gains +25% 'Physical' melee damage.", "LiberMortis", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true)); // add 25% extra damage to champion

            _bookofWsoranKeystone.Initialize(CareerID, "Harbinger gains +25% 'Ward Save' and can no longer be staggered.", "BookOfWsoran", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true)); // add 25% Wardsave to champion

            _grimoireNecrisKeystone.Initialize(CareerID, "Harbinger kills restore 2 Hitpoints to you and Harbinger. It also gains a set of ancient armour.", "GrimoireNecris", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "summon_champion",
                        PropertyName = "TroopIdToSummon",
                        PropertyValue = (choice, originalValue, agent) => originalValue+"_plate",
                        MutationType = OperationType.Replace
                    },
                }, new CareerChoiceObject.PassiveEffect()); // For every kill as Harbinger you and the Harbinger regain 2 HP

            _booksOfNagashKeystone.Initialize(CareerID, "+20% 'Magic' melee damage for Harbinger, kills it makes restores 1 of your 'Winds of Magic'.", "BooksOfNagash", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                }, new CareerChoiceObject.PassiveEffect(20, PassiveEffectType.Special, true));// For every kill as Harbinger the necromancer gains 1 Winds of Magic
        }

        protected override void InitializePassives()
        {
            _liberNecrisPassive1.Initialize(CareerID, "-20% party weight of 'Lesser Undead' troops.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.UnitPartyWeight, false, characterObject => characterObject.IsSkeleton()));
            _liberNecrisPassive2.Initialize(CareerID, "Harbinger kills grant Rougery experience.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0, PassiveEffectType.Special));
            _liberNecrisPassive3.Initialize(CareerID, "+10 personal Hitpoints.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Health));
            _liberNecrisPassive4.Initialize(CareerID, "+10% personal 'Magic' spell damage.", "LiberNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));

            _deArcanisKadonPassive1.Initialize(CareerID, "+10% 'Physical' melee damage for 'Lesser Undead' troops.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, DeArcanisKadonPassive1));
            _deArcanisKadonPassive2.Initialize(CareerID, "-20% party weight of 'Lesser Undead' troops.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.UnitPartyWeight, false, characterObject => characterObject.IsSkeleton()));
            _deArcanisKadonPassive3.Initialize(CareerID, "-15% personal 'Winds of Magic' cost for spells.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.WindsCostReduction, true));
            _deArcanisKadonPassive4.Initialize(CareerID, "+5 personal 'Winds of Magic' capacity.", "DeArcanisKadon", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));

            _codexMortificaPassive1.Initialize(CareerID, "-20% party weight of 'Lesser Undead' troops.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.2f, PassiveEffectType.UnitPartyWeight, false, characterObject => characterObject.IsSkeleton()));
            _codexMortificaPassive2.Initialize(CareerID, "+5 personal 'Winds of Magic' capacity.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.WindsOfMagic));
            _codexMortificaPassive3.Initialize(CareerID, "Wounded troops heal faster.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(2, PassiveEffectType.TroopRegeneration)); //can be more precise
            _codexMortificaPassive4.Initialize(CareerID, "+20% chance tier 4+ 'Lesser Undead' troops will be wounded instead of killed.", "CodexMortifica", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.Special, true));

            _liberMortisPassive1.Initialize(CareerID, "+10 personal 'Winds of Magic' capacity.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
            _liberMortisPassive2.Initialize(CareerID, "+15% 'Armour Penetration' for 'Lesser Undead' troops.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.Special, true));
            _liberMortisPassive3.Initialize(CareerID, "+10% 'Physical' melee damage for 'Lesser Undead' troops.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, LiberMortisPassive3));
            _liberMortisPassive4.Initialize(CareerID, "+20% personal spell cooldown reduction.", "LiberMortis", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.WindsCooldownReduction, true));

            _bookofWsoranPassive1.Initialize(CareerID, "+50 increased party size.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.PartySize));
            _bookofWsoranPassive2.Initialize(CareerID, "+50% duration of 'Hex' spells.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50f, PassiveEffectType.DebuffDuration, true));
            _bookofWsoranPassive3.Initialize(CareerID, "+25% 'Ward Save' for 'Lesser Undead' troops.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 25), AttackTypeMask.All, BookofWsoranPassive3));
            _bookofWsoranPassive4.Initialize(CareerID, "+0.5 daily 'Dark Energy' per Grave Guard troop.", "BookOfWsoran", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.5f, PassiveEffectType.Special, false));

            _grimoireNecrisPassive1.Initialize(CareerID, "+10% 'Magic' melee damage for 'Lesser Undead' troops.", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Melee, LiberMortisPassive3));
            _grimoireNecrisPassive2.Initialize(CareerID, "-25% 'Dark Energy' upkeep cost for troops.", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpkeepModifier, true));
            _grimoireNecrisPassive3.Initialize(CareerID, "+50% duration for 'Augment' and 'Healing' spells.", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.BuffDuration, true));
            _grimoireNecrisPassive4.Initialize(CareerID, "'Summon' spells raise 1 more troop per cast for every 'Enchanted' piece of equipment.", "GrimoireNecris", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect());

            _booksOfNagashPassive1.Initialize(CareerID, "+15 'Dark Energy' daily.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(15, PassiveEffectType.CustomResourceGain));
            _booksOfNagashPassive2.Initialize(CareerID, "+50 increased party size.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.PartySize));
            _booksOfNagashPassive3.Initialize(CareerID, "+25 personal 'Winds of Magic' capacity.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.WindsOfMagic));
            _booksOfNagashPassive4.Initialize(CareerID, "-50% 'Dark Energy' cost for upgrading troops.", "BooksOfNagash", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-50, PassiveEffectType.CustomResourceUpgradeCostModifier, true));
        }


        public override void InitialCareerSetup()
        {
            // Become hostile to Human pantheon religions (your former faith)
            var religions = ReligionObject.All.FindAll(x => x.Pantheon == Pantheon.Human);
            foreach (var religion in religions)
            {
                Hero.MainHero.AddReligiousInfluence(religion, -100, true);
            }

            ReligionObject nagash = ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash");
            if (nagash != null)
            {
                Hero.MainHero.AddReligiousInfluence(nagash, 25, true);
            }
        }

        private static bool IsSkeletonTroop(Agent agent)
        {
            if (!agent.BelongsToMainParty()) return false;
            return !agent.IsHero && agent.Character.IsSkeleton();
        }

        private static bool DeArcanisKadonPassive1(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            return IsSkeletonTroop(attacker);
        }

        private static bool LiberMortisPassive3(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            return IsSkeletonTroop(attacker);
        }


        private static bool BookofWsoranPassive3(Agent attacker, Agent victim, AttackTypeMask mask)
        {
            return IsSkeletonTroop(victim);
        }


    }
}