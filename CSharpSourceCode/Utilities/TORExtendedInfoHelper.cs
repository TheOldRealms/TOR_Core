using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Utilities
{
    public static class TORExtendedInfoHelper
    {
        public static List<TooltipProperty> GenererateExtendedTroopInfoToolTip(CharacterObject characterObject)
        {
            if (characterObject == null) return new List<TooltipProperty>();

            var text = "Unit Attributes";
            var model = Campaign.Current.Models.CharacterStatsModel;
            var hitpoints = model.MaxHitpoints(characterObject).ResultNumber;

            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId);
            List<TooltipProperty> list = new List<TooltipProperty>();
            if (info == null) return list;
            
            
            list.Add(new TooltipProperty("Health", hitpoints.ToString, 0, false,
                TooltipProperty.TooltipPropertyFlags.RundownResult));
            
            
            list.AddRange(GenerateDamageDisplay(characterObject, info));
            list.AddRange(GenerateAmplifierDisplay(info));
            list.AddRange(GenerateResistanceDisplay(info));
            list.AddRange(GenerateAttributeDisplay(info));


            if (!list.IsEmpty())
            {
                list.Insert(0, new TooltipProperty(text, "", 0, false, TooltipProperty.TooltipPropertyFlags.Title));
            }

            return list;
        }

        private static List<TooltipProperty> GenerateAttributeDisplay(CharacterExtendedInfo info)
        {
            var attributeTexts = new List<TooltipProperty>();
            if (!info.CharacterAttributes.Any()) return attributeTexts;
            
            foreach (var attribute in info.CharacterAttributes)
            {
                if (GameTexts.TryGetText("tor_extendedInfo", out TextObject text, attribute))
                {
                    attributeTexts.Add(new TooltipProperty("", text.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.MultiLine));
                }
            }
            
            if (attributeTexts.Any())
            {
                attributeTexts.Insert(0,
                    new TooltipProperty("-", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
            }

            return attributeTexts;
        }

        private static List<TooltipProperty> GenerateResistanceDisplay(CharacterExtendedInfo info)
        {
            var resistanceList = new List<TooltipProperty>();
            foreach (var resistanceTuple in info.Resistances)
            {
                var percentText = resistanceTuple.ReductionPercent.ToString("P0");

                if (resistanceTuple.ResistedDamageType != DamageType.All)
                {
                    resistanceList.Add(new TooltipProperty(resistanceTuple.ResistedDamageType.ToString(), percentText,
                        0, false, TooltipProperty.TooltipPropertyFlags.None));
                    continue;
                }

                resistanceList.Add(new TooltipProperty("Wardsave", percentText, 0, false,
                    TooltipProperty.TooltipPropertyFlags.None));
            }

            if (!resistanceList.IsEmpty())
            {
                resistanceList.Insert(0,
                    new TooltipProperty("-", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
                resistanceList.Insert(1,
                    new TooltipProperty("Damage Resistances", "", 0, false,
                        TooltipProperty.TooltipPropertyFlags.RundownResult));
            }

            return resistanceList;
        }

        private static List<TooltipProperty> GenerateAmplifierDisplay(CharacterExtendedInfo info)
        {
            var amplifierlist = new List<TooltipProperty>();
            foreach (var amplifier in info.DamageAmplifiers)
            {
                var percentText = amplifier.DamageAmplifier.ToString("P0");
                amplifierlist.Add(new TooltipProperty(amplifier.AmplifiedDamageType.ToString(), percentText, 0, false,
                    TooltipProperty.TooltipPropertyFlags.None));
            }

            if (!amplifierlist.IsEmpty())
            {
                amplifierlist.Insert(0,
                    new TooltipProperty("-", "", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
                amplifierlist.Insert(1,
                    new TooltipProperty("Damage Amplification", "", 0, false,
                        TooltipProperty.TooltipPropertyFlags.RundownResult));
            }

            return amplifierlist;
        }

        private static List<TooltipProperty> GenerateDamageDisplay(CharacterObject characterObject, CharacterExtendedInfo info)
        {
            var listDamages = new List<(int DamageValue, string damageType)>();
            var equipment = characterObject.GetCharacterEquipment(EquipmentIndex.Weapon0, EquipmentIndex.Weapon3);
            foreach (var item in equipment)
            {
                if (characterObject.IsRanged)
                {
                    if (item.PrimaryWeapon.IsRangedWeapon && !item.PrimaryWeapon.IsAmmo)
                    {
                        listDamages.Add((item.PrimaryWeapon.MissileDamage, "Ranged"));
                    }
                }

                if (item.PrimaryWeapon.IsMeleeWeapon)
                {
                    var typetext = "Melee";
                    if (item.PrimaryWeapon.RelevantSkill == DefaultSkills.Polearm)
                    {
                        typetext = "Lance/Spear";
                    }

                    var damage = Math.Max(item.PrimaryWeapon.SwingDamage, item.PrimaryWeapon.ThrustDamage);
                    listDamages.Add((damage, typetext));
                }
            }
                
            var damagesList = new List<TooltipProperty>();

            foreach (var damageCategory in listDamages)
            {
                var resultDamge = 0;
                var propotionList = new List<TooltipProperty>();
                foreach (var tuple in info.DamageProportions)
                {
                    var value = (int)(damageCategory.DamageValue * tuple.Percent);
                    resultDamge += value;
                    propotionList.Add(new TooltipProperty(tuple.DamageType.ToString, value.ToString, 0, false,
                        TooltipProperty.TooltipPropertyFlags.None));
                }
                    
                damagesList.Add(new TooltipProperty(damageCategory.damageType + " Damage ",
                    resultDamge.ToString, 0, false, TooltipProperty.TooltipPropertyFlags.RundownResult));

                if (propotionList.Count > 1)
                {
                    propotionList.Insert(0,
                        new TooltipProperty("-", "", 0, false,
                            TooltipProperty.TooltipPropertyFlags.RundownSeperator));
                    damagesList.AddRange(propotionList);
                }
            }

            damagesList.Insert(0,
                new TooltipProperty("-", "-", 0, false, TooltipProperty.TooltipPropertyFlags.DefaultSeperator));
            return damagesList;
        }
    }
}