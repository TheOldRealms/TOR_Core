using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.BattleMechanics.DamageSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions.ExtendedInfoSystem;
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

        public static bool HasAnyCareer(this CharacterObject characterObject)
        {
            return characterObject.HeroObject ==null && characterObject.HeroObject.HasAnyCareer();
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

        public static bool IsUndead(this BasicCharacterObject characterObject)
        {
            return characterObject.GetAttributes().Contains("Undead");
        }

        public static bool IsVampire(this BasicCharacterObject characterObject)
        {
            return characterObject.Race == FaceGen.GetRaceOrDefault("vampire");
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
