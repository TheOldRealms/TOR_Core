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

public class OrcBossCareerChoices(CareerObject id) : TORCareerChoicesBase(id)
{
    private CareerChoiceObject _bossRoot;
    private CareerChoiceObject _tufferDanNailsPassive1;
    private CareerChoiceObject _tufferDanNailsPassive2;
    private CareerChoiceObject _tufferDanNailsPassive3;
    private CareerChoiceObject _tufferDanNailsPassive4;
    private CareerChoiceObject _tufferDanNailsKeystone;

    private CareerChoiceObject _youAnWotArmourPassive1;
    private CareerChoiceObject _youAnWotArmourPassive2;
    private CareerChoiceObject _youAnWotArmourPassive3;
    private CareerChoiceObject _youAnWotArmourPassive4;
    private CareerChoiceObject _youAnWotArmourKeystone;

    private CareerChoiceObject _goodwivBlockasPassive1;
    private CareerChoiceObject _goodwivBlockasPassive2;
    private CareerChoiceObject _goodwivBlockasPassive3;
    private CareerChoiceObject _goodwivBlockasPassive4;
    private CareerChoiceObject _goodwivBlockasKeystone;

    private CareerChoiceObject _meanestanDaBaddestPassive1;
    private CareerChoiceObject _meanestanDaBaddestPassive2;
    private CareerChoiceObject _meanestanDaBaddestPassive3;
    private CareerChoiceObject _meanestanDaBaddestPassive4;
    private CareerChoiceObject _meanestanDaBaddestKeystone;

    private CareerChoiceObject _getToDaChoppasPassive1;
    private CareerChoiceObject _getToDaChoppasPassive2;
    private CareerChoiceObject _getToDaChoppasPassive3;
    private CareerChoiceObject _getToDaChoppasPassive4;
    private CareerChoiceObject _getToDaChoppasKeystone;

    private CareerChoiceObject _leafNuffinBehinPassive1;
    private CareerChoiceObject _leafNuffinBehinPassive2;
    private CareerChoiceObject _leafNuffinBehinPassive3;
    private CareerChoiceObject _leafNuffinBehinPassive4;
    private CareerChoiceObject _leafNuffinBehinKeystone;

    private CareerChoiceObject _bestofDaBestPassive1;
    private CareerChoiceObject _bestofDaBestPassive2;
    private CareerChoiceObject _bestofDaBestPassive3;
    private CareerChoiceObject _bestofDaBestPassive4;
    private CareerChoiceObject _bestofDaBestKeystone;

    private static bool HasAllThreeWeaponTypes(Agent agent)
    {
        bool hasPolearm = false, hasOneHanded = false, hasTwoHanded = false;

        for (var i = 0; i < 4; i++)
        {
            if (agent.Equipment[i].IsEmpty) continue;
            if (!hasPolearm)
            {
                hasPolearm = agent.Equipment[i].CurrentUsageItem.RelevantSkill == DefaultSkills.Polearm;
            }

            if (!hasOneHanded)
            {
                hasOneHanded = agent.Equipment[i].CurrentUsageItem.RelevantSkill == DefaultSkills.OneHanded;
            }

            if (!hasTwoHanded)
            {
                hasTwoHanded = agent.Equipment[i].CurrentUsageItem.RelevantSkill == DefaultSkills.TwoHanded;
            }
        }

        return hasPolearm && hasOneHanded && hasTwoHanded;
    }

    protected override void RegisterAll()
    {
        _bossRoot = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject("OrcBossRoot"));

        // TufferDanNails
        _tufferDanNailsKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tufferDanNailsKeystone).UnderscoreFirstCharToUpper()));
        _tufferDanNailsPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tufferDanNailsPassive1).UnderscoreFirstCharToUpper()));
        _tufferDanNailsPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tufferDanNailsPassive2).UnderscoreFirstCharToUpper()));
        _tufferDanNailsPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tufferDanNailsPassive3).UnderscoreFirstCharToUpper()));
        _tufferDanNailsPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_tufferDanNailsPassive4).UnderscoreFirstCharToUpper()));

        // YouAnWotArmour
        _youAnWotArmourKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_youAnWotArmourKeystone).UnderscoreFirstCharToUpper()));
        _youAnWotArmourPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_youAnWotArmourPassive1).UnderscoreFirstCharToUpper()));
        _youAnWotArmourPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_youAnWotArmourPassive2).UnderscoreFirstCharToUpper()));
        _youAnWotArmourPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_youAnWotArmourPassive3).UnderscoreFirstCharToUpper()));
        _youAnWotArmourPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_youAnWotArmourPassive4).UnderscoreFirstCharToUpper()));

        // GoodwivBlockas
        _goodwivBlockasKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_goodwivBlockasKeystone).UnderscoreFirstCharToUpper()));
        _goodwivBlockasPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_goodwivBlockasPassive1).UnderscoreFirstCharToUpper()));
        _goodwivBlockasPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_goodwivBlockasPassive2).UnderscoreFirstCharToUpper()));
        _goodwivBlockasPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_goodwivBlockasPassive3).UnderscoreFirstCharToUpper()));
        _goodwivBlockasPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_goodwivBlockasPassive4).UnderscoreFirstCharToUpper()));

        // Meanestan’daBaddest
        _meanestanDaBaddestKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_meanestanDaBaddestKeystone).UnderscoreFirstCharToUpper()));
        _meanestanDaBaddestPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_meanestanDaBaddestPassive1).UnderscoreFirstCharToUpper()));
        _meanestanDaBaddestPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_meanestanDaBaddestPassive2).UnderscoreFirstCharToUpper()));
        _meanestanDaBaddestPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_meanestanDaBaddestPassive3).UnderscoreFirstCharToUpper()));
        _meanestanDaBaddestPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_meanestanDaBaddestPassive4).UnderscoreFirstCharToUpper()));

        // GetToDaChoppas
        _getToDaChoppasKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_getToDaChoppasKeystone).UnderscoreFirstCharToUpper()));
        _getToDaChoppasPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_getToDaChoppasPassive1).UnderscoreFirstCharToUpper()));
        _getToDaChoppasPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_getToDaChoppasPassive2).UnderscoreFirstCharToUpper()));
        _getToDaChoppasPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_getToDaChoppasPassive3).UnderscoreFirstCharToUpper()));
        _getToDaChoppasPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_getToDaChoppasPassive4).UnderscoreFirstCharToUpper()));

        // LeafNuffinBehin
        _leafNuffinBehinKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_leafNuffinBehinKeystone).UnderscoreFirstCharToUpper()));
        _leafNuffinBehinPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_leafNuffinBehinPassive1).UnderscoreFirstCharToUpper()));
        _leafNuffinBehinPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_leafNuffinBehinPassive2).UnderscoreFirstCharToUpper()));
        _leafNuffinBehinPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_leafNuffinBehinPassive3).UnderscoreFirstCharToUpper()));
        _leafNuffinBehinPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_leafNuffinBehinPassive4).UnderscoreFirstCharToUpper()));

        // BestofDaBest
        _bestofDaBestKeystone = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bestofDaBestKeystone).UnderscoreFirstCharToUpper()));
        _bestofDaBestPassive1 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bestofDaBestPassive1).UnderscoreFirstCharToUpper()));
        _bestofDaBestPassive2 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bestofDaBestPassive2).UnderscoreFirstCharToUpper()));
        _bestofDaBestPassive3 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bestofDaBestPassive3).UnderscoreFirstCharToUpper()));
        _bestofDaBestPassive4 = Game.Current.ObjectManager.RegisterPresumedObject(new CareerChoiceObject(nameof(_bestofDaBestPassive4).UnderscoreFirstCharToUpper()));
    }

    protected override void InitializeKeyStones()
    {
        _bossRoot.Initialize(CareerID, "Take yer choppa, an' smash it proppa 'ard on any o' da gits, make 'em scream. Sum choppas are betta at uvva fings: Smalla choppas an' blockas are good fer timez when you'z in da scrap, an' iz time fer sum speedy 'taktikz'. Bigga choppas are best fer mobs o' gits. Makes 'em weaker da more ya hit 'em too. Longa choppas, now dese be useful fer ridas, an' uva pesky beasties. An' if you fink you can't chop any more, you jus' need ta wait, an' den chop sum more!", null, true,
            ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
            {
                new()
                {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "ImbuedStatusEffects",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    if (!agent.WieldedWeapon.IsEmpty && agent.WieldedWeapon.CurrentUsageItem.IsTwoHanded)
                    {
                        return ((List<string>)originalValue).Concat(new[] { "armed_to_da_teef_phys_res_debuff" }).ToList();
                    }

                    return originalValue;
                },
                MutationType = OperationType.Replace},

                new()
                {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "ImbuedStatusEffects",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    if (!agent.WieldedWeapon.IsEmpty && agent.WieldedWeapon.CurrentUsageItem.IsOneHanded)
                    {
                        return ((List<string>)originalValue).Concat(new[] { "armed_to_da_teef_speed_debuff" }).ToList();
                    }
                    return originalValue;
                }, MutationType = OperationType.Replace
                }
            });

        // Tuffer dan Nails - Add bleeding DOT
        _tufferDanNailsKeystone.Initialize(CareerID, "Armed to da Teef makes da gits bleed", "TufferDanNails", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "ImbuedStatusEffects",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    var list = (List<string>)originalValue;
                    if (!list.Contains("armed_to_da_teef_bleeding"))
                        list.Add("armed_to_da_teef_bleeding");
                    return list;
                },
                MutationType = OperationType.Replace
            }
        });

        // You an wot armour - Damage scales with Two Handed skill
        _youAnWotArmourKeystone.Initialize(CareerID, "Armed to da Teef iz betta killy wiv Two Handed pointz", "YouAnWotArmour", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "DamageAmount",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    var baseDamage = (int)originalValue;
                    var twoHandedSkill = agent.GetHero()?.GetSkillValue(DefaultSkills.TwoHanded) ?? 0;
                    return baseDamage + (twoHandedSkill * 0.5f);
                },
                MutationType = OperationType.Replace
            }
        });

        // Good wiv blockas - Damage scales with One Handed skill
        _goodwivBlockasKeystone.Initialize(CareerID, "Armed to da Teef iz betta killy wiv One Handed pointz", "GoodwivBlockas", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "DamageAmount",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    var oneHandedSkill = agent.GetHero()?.GetSkillValue(DefaultSkills.OneHanded) ?? 0;
                    return (oneHandedSkill * 0.5f);
                },
                MutationType = OperationType.Add
            }
        });

        // Meanest an da baddest - Damage scales with Polearm skill
        _meanestanDaBaddestKeystone.Initialize(CareerID, "Armed to da Teef iz betta killy wiv Polearm pointz", "MeanestanDaBaddest", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "DamageAmount",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    var polearmSkill = agent.GetHero()?.GetSkillValue(DefaultSkills.Polearm) ?? 0;
                    return (polearmSkill * 0.5f);
                },
                MutationType = OperationType.Add
            }
        });

        // Get to da Choppas - Friendly buff (handled in script)
        _getToDaChoppasKeystone.Initialize(CareerID, "Armed to da Teef makes da Boys proppa killy fer sum time", "GetToDaChoppas", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>());

        // Leave nuffing behind - Add slow debuff
        _leafNuffinBehinKeystone.Initialize(CareerID, "Armed to da Teef makes da gits run slowa'", "LeafNuffinBehin", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "ImbuedStatusEffects",
                PropertyValue = (choice, originalValue, agent) =>
                {
                    var list = (List<string>)originalValue;
                    if (!list.Contains("armed_to_da_teef_speed_debuff"))
                        list.Add("armed_to_da_teef_speed_debuff");
                    return list;
                },
                MutationType = OperationType.Replace
            }
        });

        // Best of da Best - Add knockdown via shockwave
        _bestofDaBestKeystone.Initialize(CareerID, "Armed to da Teef bashes gits to da ground", "BestofDaBest", false, ChoiceType.Keystone, new List<CareerChoiceObject.MutationObject>()
        {
            new CareerChoiceObject.MutationObject()
            {
                MutationTargetType = typeof(TriggeredEffectTemplate),
                MutationTargetOriginalId = "armed_to_da_teef",
                PropertyName = "HasShockWave",
                PropertyValue = (choice, originalValue, agent) => true,
                MutationType = OperationType.Replace
            }
        });
    }

    protected override void InitializePassives()
    {
        // TufferDanNails
        _tufferDanNailsPassive1.Initialize(CareerID, "'Ardiness iz 10% betta", "TufferDanNails", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee | AttackTypeMask.Ranged));
        _tufferDanNailsPassive2.Initialize(CareerID, "Triple 'ealin when da mob iz travellin'", "TufferDanNails", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(3, PassiveEffectType.HealthRegeneration));
        _tufferDanNailsPassive3.Initialize(CareerID, "75 more tuffness", "TufferDanNails", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(75, PassiveEffectType.Health));
        _tufferDanNailsPassive4.Initialize(CareerID, "Double Renown afta' winnin' scraps", "TufferDanNails", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.BattleRenownGain, true)); //applied as a factor, ie. +100%

        // YouAnWotArmour
        _youAnWotArmourPassive1.Initialize(CareerID, "10% betta killin' fer Boys an' Boar Boys", "YouAnWotArmour", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, (attacker, victim, mask) =>
            attacker.BelongsToMainParty() && !attacker.IsHero && (attacker.Character as CharacterObject).IsOrc() && (mask == AttackTypeMask.Melee)));
        _youAnWotArmourPassive2.Initialize(CareerID, "10% betta killin' when wearin' small, bigga', an' longa' choppas", "YouAnWotArmour", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, (attacker, victim, mask) =>
                attacker.IsMainAgent && mask == AttackTypeMask.Melee && HasAllThreeWeaponTypes(attacker)));
        _youAnWotArmourPassive3.Initialize(CareerID, "Ignore 10% uv armoured bits wiv choppas", "YouAnWotArmour", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-10, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
        _youAnWotArmourPassive4.Initialize(CareerID, "Bash down blockas forr timez fasta'", "YouAnWotArmour", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(400, PassiveEffectType.BonusDamageShield, true));

        // GoodwivBlockas
        _goodwivBlockasPassive1.Initialize(CareerID, "No more stumblin' from puny hits", "GoodwivBlockas", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(30, PassiveEffectType.ShruggedOff));
        _goodwivBlockasPassive2.Initialize(CareerID, "'Ardiness iz 10% betta when wearin' a blocka", "GoodwivBlockas", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Resistance, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee | AttackTypeMask.Ranged, (attacker, victim, mask) => victim.IsMainAgent && victim.WieldedOffhandWeapon.IsShield()));
        _goodwivBlockasPassive3.Initialize(CareerID, "25 more Boys fer da mob", "GoodwivBlockas", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(25, PassiveEffectType.PartySize));
        _goodwivBlockasPassive4.Initialize(CareerID, "'Ardiness uv choppa Boys iz 10% betta", "GoodwivBlockas", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.TroopDamage,
            new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, (attacker, victim, mask) => attacker.BelongsToMainParty() && !attacker.IsHero && (attacker.Character as CharacterObject).IsOrc() && !attacker.HasMount && mask == AttackTypeMask.Melee));

        // Meanestan'daBaddest
        _meanestanDaBaddestPassive1.Initialize(CareerID, "10% betta killin' ", "MeanestanDaBaddest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
        _meanestanDaBaddestPassive2.Initialize(CareerID, "5 more Bosses fer da mob", "MeanestanDaBaddest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
        _meanestanDaBaddestPassive3.Initialize(CareerID, "50% more shinies when leavin' loot in Kwartamasta piles", "MeanestanDaBaddest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, true));
        _meanestanDaBaddestPassive4.Initialize(CareerID, "Big Bosses in da mob pay 5 Teef e'ry day", "MeanestanDaBaddest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.Special, true));

        // GetToDaChoppas
        _getToDaChoppasPassive1.Initialize(CareerID, "10% betta killin' ", "GetToDaChoppas", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, (attacker, victim, mask) =>
                attacker.IsMainAgent && mask == AttackTypeMask.Melee && !attacker.WieldedWeapon.IsEmpty &&
                (attacker.WieldedWeapon.CurrentUsageItem.WeaponClass == WeaponClass.OneHandedAxe || attacker.WieldedWeapon.CurrentUsageItem.WeaponClass == WeaponClass.TwoHandedAxe)));
        _getToDaChoppasPassive2.Initialize(CareerID, "5 more Bosses fer da mob", "GetToDaChoppas", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(5, PassiveEffectType.CompanionLimit));
        _getToDaChoppasPassive3.Initialize(CareerID, "10% betta killin' when wearin' small, bigga', an' longa' choppas", "GetToDaChoppas", false, ChoiceType.Passive, null,
            new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee, (attacker, victim, mask) =>
                attacker.IsMainAgent && mask == AttackTypeMask.Melee && HasAllThreeWeaponTypes(attacker)));
        _getToDaChoppasPassive4.Initialize(CareerID, "50% more Teef afta' duelin' Warbosses", "GetToDaChoppas", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special, true));

        // LeafNuffinBehin
        _leafNuffinBehinPassive1.Initialize(CareerID, "Da Boys take one-forf less Teef to get betta", "LeafNuffinBehin", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-25, PassiveEffectType.CustomResourceUpgradeCostModifier, true));
        _leafNuffinBehinPassive2.Initialize(CareerID, "Ignore 20% uv armoured bits wiv choppas", "LeafNuffinBehin", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(-20, PassiveEffectType.ArmorPenetration, AttackTypeMask.Melee));
        _leafNuffinBehinPassive3.Initialize(CareerID, "50 more Boys fer da mob", "LeafNuffinBehin", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.PartySize));
        _leafNuffinBehinPassive4.Initialize(CareerID, "Less Morale penalty uv Extort Teef", "LeafNuffinBehin", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(50, PassiveEffectType.Special, true));

        // BestofDaBest
        _bestofDaBestPassive1.Initialize(CareerID, "10% betta killin' ", "BestofDaBest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(PassiveEffectType.Damage, new DamageProportionTuple(DamageType.Physical, 10), AttackTypeMask.Melee));
        _bestofDaBestPassive2.Initialize(CareerID, "Choppa attacks are 10% fasta", "BestofDaBest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(10f, PassiveEffectType.SwingSpeed, true));
        _bestofDaBestPassive3.Initialize(CareerID, "75 more tuffness", "BestofDaBest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(75, PassiveEffectType.Health));
        _bestofDaBestPassive4.Initialize(CareerID, "100 more tuffness fer Big Bosses in da mob", "BestofDaBest", false, ChoiceType.Passive, null, new CareerChoiceObject.PassiveEffect(100, PassiveEffectType.Special, true));
    }
}