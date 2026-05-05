using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices
{
    public class WitchHunterCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
    {
        private CareerChoiceObject _witchHunterRoot;

        private CareerChoiceObject _toolsOfJudgementKeystone;
        private CareerChoiceObject _toolsOfJudgementPassive1;
        private CareerChoiceObject _toolsOfJudgementPassive2;
        private CareerChoiceObject _toolsOfJudgementPassive3;
        private CareerChoiceObject _toolsOfJudgementPassive4;

        private CareerChoiceObject _huntTheWickedKeystone;
        private CareerChoiceObject _huntTheWickedPassive1;
        private CareerChoiceObject _huntTheWickedPassive2;
        private CareerChoiceObject _huntTheWickedPassive3;
        private CareerChoiceObject _huntTheWickedPassive4;

        private CareerChoiceObject _silverHammerKeystone;
        private CareerChoiceObject _silverHammerPassive1;
        private CareerChoiceObject _silverHammerPassive2;
        private CareerChoiceObject _silverHammerPassive3;
        private CareerChoiceObject _silverHammerPassive4;

        private CareerChoiceObject _noRestAgainstEvilKeystone;
        private CareerChoiceObject _noRestAgainstEvilPassive1;
        private CareerChoiceObject _noRestAgainstEvilPassive2;
        private CareerChoiceObject _noRestAgainstEvilPassive3;
        private CareerChoiceObject _noRestAgainstEvilPassive4;

        private CareerChoiceObject _swiftProcedureKeystone;
        private CareerChoiceObject _swiftProcedurePassive1;
        private CareerChoiceObject _swiftProcedurePassive2;
        private CareerChoiceObject _swiftProcedurePassive3;
        private CareerChoiceObject _swiftProcedurePassive4;

        private CareerChoiceObject _guiltyByAssociationKeystone;
        private CareerChoiceObject _guiltyByAssociationPassive1;
        private CareerChoiceObject _guiltyByAssociationPassive2;
        private CareerChoiceObject _guiltyByAssociationPassive3;
        private CareerChoiceObject _guiltyByAssociationPassive4;

        private CareerChoiceObject _endsJustifiesMeansKeystone;
        private CareerChoiceObject _endsJustifiesMeansPassive1;
        private CareerChoiceObject _endsJustifiesMeansPassive2;
        private CareerChoiceObject _endsJustifiesMeansPassive3;
        private CareerChoiceObject _endsJustifiesMeansPassive4;


        protected override void RegisterAll()
        {
            _witchHunterRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("WitchHunterRoot"));
            _huntTheWickedKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedKeystone"));
            _huntTheWickedPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive1"));
            _huntTheWickedPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive2"));
            _huntTheWickedPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive3"));
            _huntTheWickedPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("HuntTheWickedPassive4"));

            _toolsOfJudgementKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementKeystone"));
            _toolsOfJudgementPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive1"));
            _toolsOfJudgementPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive2"));
            _toolsOfJudgementPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive3"));
            _toolsOfJudgementPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("ToolsOfJudgementPassive4"));

            _silverHammerKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerKeystone"));
            _silverHammerPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive1"));
            _silverHammerPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive2"));
            _silverHammerPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive3"));
            _silverHammerPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SilverHammerPassive4"));

            _noRestAgainstEvilKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilKeystone"));
            _noRestAgainstEvilPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive1"));
            _noRestAgainstEvilPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive2"));
            _noRestAgainstEvilPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive3"));
            _noRestAgainstEvilPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("NoRestAgainstEvilPassive4"));

            _swiftProcedureKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwiftProcedureKeystone"));
            _swiftProcedurePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwiftProcedurePassive1"));
            _swiftProcedurePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwiftProcedurePassive2"));
            _swiftProcedurePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwiftProcedurePassive3"));
            _swiftProcedurePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("SwiftProcedurePassive4"));

            _guiltyByAssociationKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationKeystone"));
            _guiltyByAssociationPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive1"));
            _guiltyByAssociationPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive2"));
            _guiltyByAssociationPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive3"));
            _guiltyByAssociationPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("GuiltyByAssociationPassive4"));

            _endsJustifiesMeansKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansKeystone"));
            _endsJustifiesMeansPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive1"));
            _endsJustifiesMeansPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive2"));
            _endsJustifiesMeansPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive3"));
            _endsJustifiesMeansPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("EndsJustifiesMeansPassive4"));
        }

        protected override void InitializeKeyStones()
        {
            _witchHunterRoot.Initialize(CareerID, "Accuse an enemy of Heresy! Place an Accusation Mark on an enemy for the next 8s, the first hit you inflict deals +20% 'Physical' damage. After a hit there is a 50% chance the foe stays marked. For every level of Gunpowder or Crossbow, increase Accusation Mark's chance to remain on the foe by 0.1%. (Ability charges by dealing ranged 'Physical' damage.)", null, true,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => 0.1f+ CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.GunPowder, DefaultSkills.Crossbow }, 0.001f,false,true),
                        MutationType = OperationType.Add
                    }
                });

            _toolsOfJudgementKeystone.Initialize(CareerID, "Accusation also scales with the highest skill of either One/Two-handed and gains +6s.", "ToolsOfJudgement", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffectDuration",
                        PropertyValue = (choice, originalValue, agent) => 6 ,
                        MutationType = OperationType.Add
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.OneHanded,DefaultSkills.TwoHanded}, 0.001f,true),
                        MutationType = OperationType.Add
                    }
                });

            _huntTheWickedKeystone.Initialize(CareerID, "Accusation is now also charged by melee damage, and gains +40% 'Physical' damage.", "HuntTheWicked", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "accusation_debuff_phy" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });

            _silverHammerKeystone.Initialize(CareerID, "Accusation also scales with Faith, and gains +40% 'Holy' damage.", "SilverHammer", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "accusation_debuff_holy" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.001f),
                        MutationType = OperationType.Add
                    }
                });

            _noRestAgainstEvilKeystone.Initialize(CareerID, "Accusation now allows for 'Shield Penetration' and no longer risks losing its Mark on hit.", "NoRestAgainstEvil", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "TriggeredEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "apply_accusation_selfbuff" }).ToList(),
                        MutationType = OperationType.Replace
                    }
                });


            _swiftProcedureKeystone.Initialize(CareerID, "Accusation also scales with Athletics and applies a swing/move speed decrease.", "SwiftProcedure", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(TriggeredEffectTemplate),
                        MutationTargetOriginalId = "apply_accusation",
                        PropertyName = "ImbuedStatusEffects",
                        PropertyValue = (choice, originalValue, agent) => ((List<string>)originalValue).Concat(new[] { "accusation_debuff_mov", "accusation_debuff_ats" }).ToList(),
                        MutationType = OperationType.Replace
                    },
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Athletics}, 0.001f),
                        MutationType = OperationType.Add
                    }
                });

            _guiltyByAssociationKeystone.Initialize(CareerID, "Companions also charge Accusation. Companions/Retinue now benefit from your Mark.", "GuiltyByAssociation", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {

                });

            _endsJustifiesMeansKeystone.Initialize(CareerID, "Accusation also scales with Rougery, overkilling an 'Accused' enemy spreads the Mark.", "EndsJustifiesMeans", false,
                ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
                {
                    new CareerChoiceObject.MutationObject()
                    {
                        MutationTargetType = typeof(AbilityTemplate),
                        MutationTargetOriginalId = "Accusation",
                        PropertyName = "ScaleVariable1",
                        PropertyValue = (choice, originalValue, agent) =>CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ DefaultSkills.Roguery}, 0.001f),
                        MutationType = OperationType.Add
                    }
                });
        }

        protected override void InitializePassives()
        {
            _toolsOfJudgementPassive1.Initialize(CareerID, "+25 personal Hitpoints.", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _toolsOfJudgementPassive2.Initialize(CareerID, "+5 extra ammo per ammunition pouch you equip.", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Ammo));
            _toolsOfJudgementPassive3.Initialize(CareerID, "+5% 'Physical' melee damage.", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee));
            _toolsOfJudgementPassive4.Initialize(CareerID, "Ranged headshot kills grant Roguery experience, twice for 'Accused' targets.", "ToolsOfJudgement", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));

            _huntTheWickedPassive1.Initialize(CareerID, "Personal healing rate increased by +1.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.HealthRegeneration));
            _huntTheWickedPassive2.Initialize(CareerID, "Gain 300% more 'Prestige' when fighting the forces of 'Chaos' or 'Undead'.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(300, PassiveEffectType.Special, true));
            _huntTheWickedPassive3.Initialize(CareerID, "Gain +5% melee 'Physical' damage per equipped ranged weapon.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special, true));
            _huntTheWickedPassive4.Initialize(CareerID, "+10% ranged 'Physical' damage for all 'Ranged' troops.", "HuntTheWicked", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !(attacker.IsMainAgent || attacker.IsHero) && mask == AttackTypeMask.Ranged));

            _silverHammerPassive1.Initialize(CareerID, "Gain Faith skill per kill when facing the forces of 'Chaos' or 'Undead'.", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special));
            _silverHammerPassive2.Initialize(CareerID, "+10% 'Holy' damage for all units when facing the forces of 'Chaos' or 'Undead'.", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 10), AttackTypeMask.All,
                (attacker, victim, mask) => victim.Character.Race != 0));
            _silverHammerPassive3.Initialize(CareerID, "'Sneak' attacks gain a 'Holy' damage boost based off Faith. (0.5% per level.)", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(0.005f, PassiveEffectType.Special));
            _silverHammerPassive4.Initialize(CareerID, "Promote non 'Knight' tier 4+ troops to your personal Retinue.", "SilverHammer", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.Special, true));          //TORAgentStatCalculateModel 458

            _noRestAgainstEvilPassive1.Initialize(CareerID, "+10% personal 'Holy' melee damage.", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Holy, 10), AttackTypeMask.Melee));
            _noRestAgainstEvilPassive2.Initialize(CareerID, "'Ranged' troops recieve +50 to their ranged skills.", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, new List<string>() { nameof(DefaultSkills.Bow), nameof(DefaultSkills.Crossbow), nameof(DefaultSkills.Throwing), nameof(TORSkills.GunPowder) },
                characterObject => !characterObject.IsHero && characterObject.IsRanged));
            _noRestAgainstEvilPassive3.Initialize(CareerID, "Personal ranged weapons recieve +15% accuracy.", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.AccuracyPenalty, true));
            _noRestAgainstEvilPassive4.Initialize(CareerID, "+5 Companion limit.", "NoRestAgainstEvil", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));

            _swiftProcedurePassive1.Initialize(CareerID, "+8 extra ammo per ammunition pouch you equip.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(8, PassiveEffectType.Ammo));
            _swiftProcedurePassive2.Initialize(CareerID, "Personal accuracy movement penalty reduced by -10%.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.RangedMovementPenalty, true));
            _swiftProcedurePassive3.Initialize(CareerID, "+1 party move speed on campaign map.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(1f, PassiveEffectType.PartyMovementSpeed));
            _swiftProcedurePassive4.Initialize(CareerID, "+10% personal weapon swing speed.", "SwiftProcedure", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10f, PassiveEffectType.SwingSpeed, true));

            _endsJustifiesMeansPassive1.Initialize(CareerID, "+10% personal ranged 'Physical' damage.", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Ranged));
            _endsJustifiesMeansPassive2.Initialize(CareerID, "+15% personal ranged 'Physical Resistance'.", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 15), AttackTypeMask.Ranged));
            _endsJustifiesMeansPassive3.Initialize(CareerID, "+15% 'Armour Penetration' for personal melee attacks.", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-15, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
            _endsJustifiesMeansPassive4.Initialize(CareerID, "+20% 'Armour Penetration' for personal ranged attacks.", "EndsJustifiesMeans", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Ranged));

            _guiltyByAssociationPassive1.Initialize(CareerID, "+25 personal Hitpoints.", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Health));
            _guiltyByAssociationPassive2.Initialize(CareerID, "'Ranged' troops recieve +15% 'Holy' 'Ranged' damage.", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Holy, 15), AttackTypeMask.Ranged,
                (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsMainAgent && mask == AttackTypeMask.Ranged));
            _guiltyByAssociationPassive3.Initialize(CareerID, "Companions gain +25 Hitpoints.", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special));
            _guiltyByAssociationPassive4.Initialize(CareerID, "Headshot kills provide a temporary increase to reload/swing speed.", "GuiltyByAssociation", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special));
        }
    }
}