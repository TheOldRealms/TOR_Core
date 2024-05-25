using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.ObjectSystem;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Extensions.UI;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions
{
    public static class CharacterObjectExtensions
    {
        public static bool IsTORTemplate(this CharacterObject characterObject)
        {
            return characterObject.StringId.StartsWith("tor_");
        }

        public static bool IsTORTemplate(this BasicCharacterObject characterObject)
        {
            return characterObject.StringId.StartsWith("tor_");
        }

        public static List<string> GetAbilities(this BasicCharacterObject characterObject)
        {
            var list = new List<string>();
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId);
            if (info != null)
            {
                list.AddRange(info.Abilities);
            }
            return list;
        }

        public static List<string> GetAttributes(this BasicCharacterObject characterObject)
        {
            var list = new List<string>();
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId);
            if (info != null)
            {
                list.AddRange(info.CharacterAttributes);
            }
            return list;
        }

        public static bool HasAttribute(this BasicCharacterObject characterObject, string attributeName)
        {
            return characterObject.GetAttributes().Contains(attributeName);
        }

        public static List<ResistanceTuple> GetDefenseProperties(this BasicCharacterObject characterObject)
        {
            var list = new List<ResistanceTuple>();
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId)?.Resistances;
            if (info != null)
            {
                list.AddRange(info);
            }
            return list;
        }

        public static List<AmplifierTuple> GetAttackProperties(this BasicCharacterObject characterObject)
        {
            var list = new List<AmplifierTuple>();
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId)?.DamageAmplifiers;
            if (info != null)
            {
                list.AddRange(info);
            }
            return list;
        }

        public static List<DamageProportionTuple> GetUnitDamageProportions(this BasicCharacterObject characterObject)
        {
            var list = new List<DamageProportionTuple>();
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId)?.DamageProportions;
            if (info != null)
            {
                list.AddRange(info);
            }
            else
            {
                TORCommon.Log("Couldn't find damage propotions for " + characterObject.Name.ToString(), LogLevel.Warn);
                var defaultProp = new DamageProportionTuple(DamageType.Physical, 1);
                list.Add(defaultProp);
            }
            return list;
        }
        
        public static bool IsUndead(this CharacterObject characterObject)
        {
            if (characterObject.IsHero)
            {
                return characterObject.HeroObject.IsUndead();
            }
            return characterObject.GetAttributes().Contains("Undead");
        }

        public static bool IsVampire(this CharacterObject characterObject)
        {
            if (characterObject.IsHero)
            {
                return characterObject.HeroObject.IsVampire();
            }
            return characterObject.Race == FaceGen.GetRaceOrDefault("vampire");
        }

        public static bool IsHuman(this CharacterObject characterObject)
        {
            if (characterObject.Culture.IsBandit)
            {
                if (characterObject.IsBeastman() || characterObject.IsCultist())
                    return false;
                else
                {
                    return true;
                }
            }
            
            return characterObject.Culture.StringId == "empire" ||
                   characterObject.Culture.StringId == TORConstants.BRETONNIA_CULTURE ||
                   characterObject.Culture.StringId == TORConstants.SYLVANIA_CULTURE &&
                   !(characterObject.IsVampire() || characterObject.IsUndead())||
                   characterObject.Culture.StringId == "mousillon" &&
                   !(characterObject.IsVampire() || characterObject.IsUndead());
            
        }

        public static bool IsKnightUnit(this BasicCharacterObject characterObject)
        {
            return  !characterObject.IsHero&&characterObject.IsMounted&&IsEliteTroop(characterObject);
        }


        public static bool IsEliteTroop(this CharacterObject characterObject)
        {
            var basicCharacterObject = (BasicCharacterObject)characterObject;
            return IsEliteTroop(basicCharacterObject);
        }
        public static bool IsEliteTroop(this BasicCharacterObject character)
        {
            var cultures = MBObjectManager.Instance.GetObjectTypeList<CultureObject>();
            bool result = false;
            foreach(var culture in cultures)
            {
                var elite = culture.EliteBasicTroop;
                if (elite == character)
                {
                    return true;
                }

                result= IsTroopInUpgradeTree(character, elite);
                if(result)
                    break;
            }
            return result;
        }

        private static bool IsTroopInUpgradeTree(this BasicCharacterObject character, CharacterObject basicCharacter)
        {
            bool result = false;
            if (basicCharacter == character) result = true;
            else if (basicCharacter.UpgradeTargets.Count() > 0)
            {
                foreach(var target in basicCharacter.UpgradeTargets)
                {
                    if(target == character)
                    {
                        result = true;
                        break;
                    } 
                    
                    result = IsTroopInUpgradeTree(character, target);
                }
            }
            return result;
        }

        public static CultureObject GetCultureObject(this BasicCharacterObject characterObject)
        {
            return (CultureObject)characterObject.Culture;
        }
        
        public static bool IsBeastman(this CharacterObject characterObject)     
        {
            return characterObject.Race == FaceGen.GetRaceOrDefault("ungor");
        }

        public static bool IsUndead(this BasicCharacterObject characterObject)
        {
            return characterObject.GetAttributes().Contains("Undead");
        }

        public static bool IsVampire(this BasicCharacterObject characterObject)
        {
            return characterObject.Race == FaceGen.GetRaceOrDefault("vampire");
        }
        
        public static bool IsCultist(this BasicCharacterObject characterObject)
        {
            return characterObject.Race == FaceGen.GetRaceOrDefault("chaos_ud_cultist");
        }

        public static bool IsBloodDragon(this BasicCharacterObject characterObject)
        {
            return characterObject.GetAttributes().Contains("BloodDragon");
        }

        public static bool IsReligiousUnit(this CharacterObject characterObject)
        {
            return ReligionObject.All.Any(x => x.ReligiousTroops.Contains(characterObject));
        }
        
        public static bool IsReligiousUnit(this BasicCharacterObject characterObject)
        {
            return ReligionObject.All.Any(x => x.ReligiousTroops.Contains(characterObject));
        }

        public static bool UnitBelongsToCult(this CharacterObject characterObject, string cultId)
        {
            var cult = ReligionObject.All.FirstOrDefault(x => x.StringId==cultId);
            return cult != null && cult.ReligiousTroops.Contains(characterObject);
        }
        public static bool UnitBelongsToCult(this BasicCharacterObject characterObject, string cultId)
        {
            var cult = ReligionObject.All.FirstOrDefault(x => x.StringId==cultId);
            return cult != null && cult.ReligiousTroops.Contains(characterObject);
        }
        
        public static bool HasCustomResourceUpgradeRequirement(this CharacterObject character)
        {
            var info = ExtendedInfoManager.GetCharacterInfoFor(character.StringId);
            if (info != null)
            {
                return info.ResourceCost != null && info.ResourceCost.UpgradeCost > 0;
            }
            return false;
        }

        public static bool HasCustomResourceUpkeepRequirement(this CharacterObject character)
        {
            var info = ExtendedInfoManager.GetCharacterInfoFor(character.StringId);
            if (info != null)
            {
                return info.ResourceCost != null && info.ResourceCost.UpkeepCost > 0;
            }
            return false;
        }

        public static Tuple<CustomResource, int> GetCustomResourceRequiredForUpgrade(this CharacterObject character, bool belongsToMainParty=false)
        {
            var info = ExtendedInfoManager.GetCharacterInfoFor(character.StringId);
            if (info != null && character.HasCustomResourceUpgradeRequirement())
            {
                var cost = info.ResourceCost.UpgradeCost;
                if (belongsToMainParty)
                {
                    var explainedNumber = new ExplainedNumber(cost);
                    CareerHelper.ApplyBasicCareerPassives(Hero.MainHero,ref explainedNumber,PassiveEffectType.CustomResourceUpgradeCostModifier,true);
                    
                    cost = (int)explainedNumber.ResultNumber;
                }
               
                return new Tuple<CustomResource, int>(CustomResourceManager.GetResourceObject(info.ResourceCost.ResourceType), cost);
            }
            return null;
        }

        public static List<TooltipProperty> GetExtendedInfoToolTipText(this CharacterObject character)
        {
            return TORExtendedInfoHelper.GenererateExtendedTroopInfoToolTip(character);
        }

        public static Tuple<CustomResource, int> GetCustomResourceRequiredForUpkeep(this CharacterObject character)
        {
            var info = ExtendedInfoManager.GetCharacterInfoFor(character.StringId);
            if (info != null && character.HasCustomResourceUpkeepRequirement())
            {
                return new Tuple<CustomResource, int>(CustomResourceManager.GetResourceObject(info.ResourceCost.ResourceType), info.ResourceCost.UpkeepCost);
            }
            return null;
        }

        /// <summary>
        /// Access item objects from the equipment of the character
        /// Equipment Indexes can define the Range. Note that horses are not a valid item object to be accessed
        /// </summary>
        public static List<ItemObject> GetCharacterEquipment(this BasicCharacterObject characterObject,
            EquipmentIndex BeginningFrom = EquipmentIndex.Weapon0, EquipmentIndex EndingAt = EquipmentIndex.ArmorItemEndSlot)
        {
            int index = (int)BeginningFrom;
            int end = (int)EndingAt;
            List<ItemObject> CharacterEquipmentItems = new List<ItemObject>();
            for (int i = index; i <= end; i++)
            {
                if (characterObject.Equipment[i].Item != null)
                {
                    CharacterEquipmentItems.Add(characterObject.Equipment[i].Item);
                }
            }
            return CharacterEquipmentItems;
        }
    }
}
