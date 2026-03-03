using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.BattleMechanics.StatusEffect;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.Choices;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Items;
using TOR_Core.Utilities;

namespace TOR_Core.CharacterDevelopment.CareerSystem.Choices;

public class OrcShamanCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _shamanRoot;

    // Bones an' Firepitz (Tier 1)
    private CareerChoiceObject _bonesAnFirepitzPassive1;
    private CareerChoiceObject _bonesAnFirepitzPassive2;
    private CareerChoiceObject _bonesAnFirepitzPassive3;
    private CareerChoiceObject _bonesAnFirepitzPassive4;
    private CareerChoiceObject _bonesAnFirepitzKeystone;

    // Visions uv da Orc-ayne (Tier 1)
    private CareerChoiceObject _visionsUvDaOrcaynePassive1;
    private CareerChoiceObject _visionsUvDaOrcaynePassive2;
    private CareerChoiceObject _visionsUvDaOrcaynePassive3;
    private CareerChoiceObject _visionsUvDaOrcaynePassive4;
    private CareerChoiceObject _visionsUvDaOrcayneKeystone;

    // Giftz from Da Great Green (Tier 2)
    private CareerChoiceObject _giftzFromDaGreatGreenPassive1;
    private CareerChoiceObject _giftzFromDaGreatGreenPassive2;
    private CareerChoiceObject _giftzFromDaGreatGreenPassive3;
    private CareerChoiceObject _giftzFromDaGreatGreenPassive4;
    private CareerChoiceObject _giftzFromDaGreatGreenKeystone;

    // Brutal Cunnin' (Tier 2)
    private CareerChoiceObject _brutalCunninPassive1;
    private CareerChoiceObject _brutalCunninPassive2;
    private CareerChoiceObject _brutalCunninPassive3;
    private CareerChoiceObject _brutalCunninPassive4;
    private CareerChoiceObject _brutalCunninKeystone;

    // Cunnin' Brutality (Tier 2)
    private CareerChoiceObject _cunninBrutalityPassive1;
    private CareerChoiceObject _cunninBrutalityPassive2;
    private CareerChoiceObject _cunninBrutalityPassive3;
    private CareerChoiceObject _cunninBrutalityPassive4;
    private CareerChoiceObject _cunninBrutalityKeystone;

    // Gork an' Mork are watchin' (Tier 3)
    private CareerChoiceObject _gorkAnMorkAreWatchinPassive1;
    private CareerChoiceObject _gorkAnMorkAreWatchinPassive2;
    private CareerChoiceObject _gorkAnMorkAreWatchinPassive3;
    private CareerChoiceObject _gorkAnMorkAreWatchinPassive4;
    private CareerChoiceObject _gorkAnMorkAreWatchinKeystone;

    // Power uv da Waaagh! (Tier 3)
    private CareerChoiceObject _powerUvDaWaaaghPassive1;
    private CareerChoiceObject _powerUvDaWaaaghPassive2;
    private CareerChoiceObject _powerUvDaWaaaghPassive3;
    private CareerChoiceObject _powerUvDaWaaaghPassive4;
    private CareerChoiceObject _powerUvDaWaaaghKeystone;

    private static bool IsWearingLightArmor(Agent agent)
    {
        if (agent == null) return false;

        float totalArmorWeight = 0f;

        var head = agent.SpawnEquipment[EquipmentIndex.Head];
        if (!head.IsEmpty) totalArmorWeight += head.GetEquipmentElementWeight();

        var body = agent.SpawnEquipment[EquipmentIndex.Body];
        if (!body.IsEmpty) totalArmorWeight += body.GetEquipmentElementWeight();

        var legs = agent.SpawnEquipment[EquipmentIndex.Leg];
        if (!legs.IsEmpty) totalArmorWeight += legs.GetEquipmentElementWeight();

        var gloves = agent.SpawnEquipment[EquipmentIndex.Gloves];
        if (!gloves.IsEmpty) totalArmorWeight += gloves.GetEquipmentElementWeight();

        var cape = agent.SpawnEquipment[EquipmentIndex.Cape];
        if (!cape.IsEmpty) totalArmorWeight += cape.GetEquipmentElementWeight();

        return totalArmorWeight < 15f;
    }

    protected override void RegisterAll()
    {
        _shamanRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("OrcShamanRoot"));

        // Bones an' Firepitz
        _bonesAnFirepitzKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bonesAnFirepitzKeystone).UnderscoreFirstCharToUpper()));
        _bonesAnFirepitzPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bonesAnFirepitzPassive1).UnderscoreFirstCharToUpper()));
        _bonesAnFirepitzPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bonesAnFirepitzPassive2).UnderscoreFirstCharToUpper()));
        _bonesAnFirepitzPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bonesAnFirepitzPassive3).UnderscoreFirstCharToUpper()));
        _bonesAnFirepitzPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bonesAnFirepitzPassive4).UnderscoreFirstCharToUpper()));

        // Visions uv da Orc-ayne
        _visionsUvDaOrcayneKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_visionsUvDaOrcayneKeystone).UnderscoreFirstCharToUpper()));
        _visionsUvDaOrcaynePassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_visionsUvDaOrcaynePassive1).UnderscoreFirstCharToUpper()));
        _visionsUvDaOrcaynePassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_visionsUvDaOrcaynePassive2).UnderscoreFirstCharToUpper()));
        _visionsUvDaOrcaynePassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_visionsUvDaOrcaynePassive3).UnderscoreFirstCharToUpper()));
        _visionsUvDaOrcaynePassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_visionsUvDaOrcaynePassive4).UnderscoreFirstCharToUpper()));

        // Giftz from Da Great Green
        _giftzFromDaGreatGreenKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giftzFromDaGreatGreenKeystone).UnderscoreFirstCharToUpper()));
        _giftzFromDaGreatGreenPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giftzFromDaGreatGreenPassive1).UnderscoreFirstCharToUpper()));
        _giftzFromDaGreatGreenPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giftzFromDaGreatGreenPassive2).UnderscoreFirstCharToUpper()));
        _giftzFromDaGreatGreenPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giftzFromDaGreatGreenPassive3).UnderscoreFirstCharToUpper()));
        _giftzFromDaGreatGreenPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_giftzFromDaGreatGreenPassive4).UnderscoreFirstCharToUpper()));

        // Brutal Cunnin'
        _brutalCunninKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_brutalCunninKeystone).UnderscoreFirstCharToUpper()));
        _brutalCunninPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_brutalCunninPassive1).UnderscoreFirstCharToUpper()));
        _brutalCunninPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_brutalCunninPassive2).UnderscoreFirstCharToUpper()));
        _brutalCunninPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_brutalCunninPassive3).UnderscoreFirstCharToUpper()));
        _brutalCunninPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_brutalCunninPassive4).UnderscoreFirstCharToUpper()));

        // Cunnin' Brutality
        _cunninBrutalityKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_cunninBrutalityKeystone).UnderscoreFirstCharToUpper()));
        _cunninBrutalityPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_cunninBrutalityPassive1).UnderscoreFirstCharToUpper()));
        _cunninBrutalityPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_cunninBrutalityPassive2).UnderscoreFirstCharToUpper()));
        _cunninBrutalityPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_cunninBrutalityPassive3).UnderscoreFirstCharToUpper()));
        _cunninBrutalityPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_cunninBrutalityPassive4).UnderscoreFirstCharToUpper()));

        // Gork an' Mork are watchin'
        _gorkAnMorkAreWatchinKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gorkAnMorkAreWatchinKeystone).UnderscoreFirstCharToUpper()));
        _gorkAnMorkAreWatchinPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gorkAnMorkAreWatchinPassive1).UnderscoreFirstCharToUpper()));
        _gorkAnMorkAreWatchinPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gorkAnMorkAreWatchinPassive2).UnderscoreFirstCharToUpper()));
        _gorkAnMorkAreWatchinPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gorkAnMorkAreWatchinPassive3).UnderscoreFirstCharToUpper()));
        _gorkAnMorkAreWatchinPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_gorkAnMorkAreWatchinPassive4).UnderscoreFirstCharToUpper()));

        // Power uv da Waaagh!
        _powerUvDaWaaaghKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_powerUvDaWaaaghKeystone).UnderscoreFirstCharToUpper()));
        _powerUvDaWaaaghPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_powerUvDaWaaaghPassive1).UnderscoreFirstCharToUpper()));
        _powerUvDaWaaaghPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_powerUvDaWaaaghPassive2).UnderscoreFirstCharToUpper()));
        _powerUvDaWaaaghPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_powerUvDaWaaaghPassive3).UnderscoreFirstCharToUpper()));
        _powerUvDaWaaaghPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_powerUvDaWaaaghPassive4).UnderscoreFirstCharToUpper()));
    }

    protected override void InitializeKeyStones()
    {
        _shamanRoot.Initialize(CareerID, "Da Shaman iz da centah uv da WAAAGH! You kunnect yerself to da boys 'round, an' dey will give you more ju-ju powa' fer ZAPPIN' an' BLASTIN'. But if da boys start dyin', den yer zappy energy gets blasted outta ya, an' they take it wiv dem! Da betta ya get, de more you can kunnect to da WAAAGH!. An' remember to stay close to da old ways. Wearin' 'ard bits an' metal an' stuff like dem 'ard boyz iz gunna get ya less ju-ju powa'.", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>());

        // Bones an' Firepitz Keystone: Ability is charged at battle start
        _bonesAnFirepitzKeystone.Initialize(CareerID, "Call uf da Green is charged at battle start.", "BonesAnFirepitz", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>());

        // Visions uv da Orc-ayne Keystone: Gaze uv Mork is free and ready after casting CA
        _visionsUvDaOrcayneKeystone.Initialize(CareerID, "Gaze uv Mork is free and ready after casting Call uf da Green.", "VisionsUvDaOrcayne", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>());

        // Giftz from Da Great Green Keystone: Call uf da Green scales with Spellcraft skill
        _giftzFromDaGreatGreenKeystone.Initialize(CareerID, "Call uf da Green effectiveness scales with Spellcraft skill.", "GiftzFromDaGreatGreen", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "CallOfDaGreen",
                    PropertyName = "Duration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Spellcraft }, 0.05f),
                    MutationType = OperationType.Add
                }
            });

        // Brutal Cunnin' Keystone: 10% extra resistance buff
        _brutalCunninKeystone.Initialize(CareerID, "Call uf da Green grants 10% extra physical resistance.", "BrutalCunnin", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>());

        // Cunnin' Brutality Keystone: 15% damage bonus for Greenskins
        _cunninBrutalityKeystone.Initialize(CareerID, "Nearby Greenskins deal 15% extra damage during Call uf da Green.", "CunninBrutality", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>());

        // Gork an' Mork are watchin' Keystone: Career ability scales with Faith
        _gorkAnMorkAreWatchinKeystone.Initialize(CareerID, "Call uf da Green effectiveness scales with Faith skill.", "GorkAnMorkAreWatchin", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>()
            {
                new CareerChoiceObject.MutationObject()
                {
                    MutationTargetType = typeof(AbilityTemplate),
                    MutationTargetOriginalId = "CallOfDaGreen",
                    PropertyName = "Duration",
                    PropertyValue = (choice, originalValue, agent) => CareerHelper.AddSkillEffectToValue(choice, agent, new List<SkillObject>(){ TORSkills.Faith }, 0.05f),
                    MutationType = OperationType.Add
                }
            });

        // Power uv da Waaagh! Keystone: 50% physical resistance during Call uf da Green
        _powerUvDaWaaaghKeystone.Initialize(CareerID, "Gain 50% physical resistance during Call uf da Green.", "PowerUvDaWaaagh", false, ChoiceType.Keystone,
            new List<CareerChoiceObject.MutationObject>());
    }

    protected override void InitializePassives()
    {
        // Bones an' Firepitz Passives
        _bonesAnFirepitzPassive1.Initialize(CareerID, "10% extra melee damage.", "BonesAnFirepitz", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
        _bonesAnFirepitzPassive2.Initialize(CareerID, "+10 Maximum Winds of Magic.", "BonesAnFirepitz", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
        _bonesAnFirepitzPassive3.Initialize(CareerID, "Extra enchantment ingredients when looting.", "BonesAnFirepitz", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.Special, true));
        _bonesAnFirepitzPassive4.Initialize(CareerID, "Looting shrines grants Spellcraft experience.", "BonesAnFirepitz", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special, true)); // CUSTOM - needs implementation

        // Visions uv da Orc-ayne Passives
        _visionsUvDaOrcaynePassive1.Initialize(CareerID, "35% increased spotting range.", "VisionsUvDaOrcayne", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(35, PassiveEffectType.PartySpottingRange, true));
        _visionsUvDaOrcaynePassive2.Initialize(CareerID, "10% extra spell damage if armor weight under 15kg.", "VisionsUvDaOrcayne", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell, (attacker, victim, mask) =>
                attacker.IsMainAgent && mask == AttackTypeMask.Spell && IsWearingLightArmor(attacker)));
        _visionsUvDaOrcaynePassive3.Initialize(CareerID, "Shrine defilement provides more meat and shinies.", "VisionsUvDaOrcayne", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special, true)); // CUSTOM - needs implementation
        _visionsUvDaOrcaynePassive4.Initialize(CareerID, "10% extra melee damage when wielding a staff.", "VisionsUvDaOrcayne", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.Melee, (attacker, victim, mask) =>
                attacker.IsMainAgent
                && mask == AttackTypeMask.Melee
                && !attacker.WieldedWeapon.IsEmpty
                && attacker.WieldedWeapon.Item.IsMagicalStaff()));

        // Giftz from Da Great Green Passives
        _giftzFromDaGreatGreenPassive1.Initialize(CareerID, "Looting shrines grants Faith experience.", "GiftzFromDaGreatGreen", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(1, PassiveEffectType.Special, true)); // CUSTOM - needs implementation
        _giftzFromDaGreatGreenPassive2.Initialize(CareerID, "10% extra spell damage.", "GiftzFromDaGreatGreen", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
        _giftzFromDaGreatGreenPassive3.Initialize(CareerID, "+50 Health.", "GiftzFromDaGreatGreen", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
        _giftzFromDaGreatGreenPassive4.Initialize(CareerID, "5% physical resistance.", "GiftzFromDaGreatGreen", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 5), AttackTypeMask.Melee | AttackTypeMask.Ranged));

        // Brutal Cunnin' Passives
        _brutalCunninPassive1.Initialize(CareerID, "+60 party size.", "BrutalCunnin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(60, PassiveEffectType.PartySize));
        _brutalCunninPassive2.Initialize(CareerID, "+50 Health.", "BrutalCunnin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Health));
        _brutalCunninPassive3.Initialize(CareerID, "10% extra physical damage with axes.", "BrutalCunnin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, (attacker, victim, mask) =>
                attacker.IsMainAgent && mask == AttackTypeMask.Melee && !attacker.WieldedWeapon.IsEmpty &&
                (attacker.WieldedWeapon.CurrentUsageItem.WeaponClass == WeaponClass.OneHandedAxe || attacker.WieldedWeapon.CurrentUsageItem.WeaponClass == WeaponClass.TwoHandedAxe)));
        _brutalCunninPassive4.Initialize(CareerID, "Hits below 25 damage no longer stagger.", "BrutalCunnin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.ShruggedOff));

        // Cunnin' Brutality Passives
        _cunninBrutalityPassive1.Initialize(CareerID, "10% extra spell damage if armor weight under 15kg.", "CunninBrutality", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell, (attacker, victim, mask) =>
                attacker.IsMainAgent && mask == AttackTypeMask.Spell && IsWearingLightArmor(attacker)));
        _cunninBrutalityPassive2.Initialize(CareerID, "+10 Winds of Magic.", "CunninBrutality", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
        _cunninBrutalityPassive3.Initialize(CareerID, "10% Wardsave.", "CunninBrutality", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All));
        _cunninBrutalityPassive4.Initialize(CareerID, "20% Armor penetration.", "CunninBrutality", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));

        // Gork an' Mork are watchin' Passives
        _gorkAnMorkAreWatchinPassive1.Initialize(CareerID, "15% spell damage resistance when wearing under 15kg.", "GorkAnMorkAreWatchin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Magical, 15), AttackTypeMask.Spell, (attacker, victim, mask) =>
                victim.IsMainAgent && IsWearingLightArmor(victim)));
        _gorkAnMorkAreWatchinPassive2.Initialize(CareerID, "+10 Winds of Magic.", "GorkAnMorkAreWatchin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(10, PassiveEffectType.WindsOfMagic));
        _gorkAnMorkAreWatchinPassive3.Initialize(CareerID, "10% Wardsave for Greenskins.", "GorkAnMorkAreWatchin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopResistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All, (attacker, victim, mask) =>
                victim.BelongsToMainParty() && !victim.IsHero && (victim.Character as CharacterObject).IsGreenskin()));
        _gorkAnMorkAreWatchinPassive4.Initialize(CareerID, "+70 HP for Shaman Boss companion.", "GorkAnMorkAreWatchin", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(70, PassiveEffectType.Special, true)); // CUSTOM - companion-specific buff

        // Power uv da Waaagh! Passives
        _powerUvDaWaaaghPassive1.Initialize(CareerID, "10% extra spell damage.", "PowerUvDaWaaagh", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Magical, 10), AttackTypeMask.Spell));
        _powerUvDaWaaaghPassive2.Initialize(CareerID, "+30 Winds of Magic for Shaman companion.", "PowerUvDaWaaagh", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.Special, true)); // CUSTOM - companion-specific buff
        _powerUvDaWaaaghPassive3.Initialize(CareerID, "10% extra wardsave when wearing under 15kg.", "PowerUvDaWaaagh", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.All, 10), AttackTypeMask.All, (attacker, victim, mask) =>
                victim.IsMainAgent && IsWearingLightArmor(victim)));
        _powerUvDaWaaaghPassive4.Initialize(CareerID, "+60 party size.", "PowerUvDaWaaagh", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(60, PassiveEffectType.PartySize));
    }
}
