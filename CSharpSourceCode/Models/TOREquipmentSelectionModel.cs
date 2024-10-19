using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TOREquipmentSelectionModel : DefaultEquipmentSelectionModel
    {
        public override MBList<MBEquipmentRoster> GetEquipmentRostersForCompanion(
            Hero hero,
            bool isCivilian)
        {
            
            var list = new MBList<MBEquipmentRoster>(); 
            list.AddRange(base.GetEquipmentRostersForCompanion (hero, isCivilian));

            if (!list.IsEmpty()) return list;
            
            EquipmentFlags suitableFlags = isCivilian ? EquipmentFlags.IsCivilianTemplate | EquipmentFlags.IsNobleTemplate : EquipmentFlags.IsNobleTemplate | EquipmentFlags.IsMediumTemplate;
            AddTOREquipmentsToRoster(hero, suitableFlags, ref list, isCivilian);

            if (list.IsEmpty())
            {
                throw new NullReferenceException (( "Could not find suitable Equipmentset" ));
            }
            return list;
        }
        
        
        
        /**
         * Very ugly fix for jumping in case of mousillon to a default case(vlandia).
         * this method as well as the IsRosterAppropriateForHeroAsTemplate, both needs in future proper care.
         * However that again require massive changes in the equipment set catalog and potentially decisions on how we want it.
         * At the moment all lords are spawned with base culture equipment, which is very ugly.
         * Should player kingdoms even be a thing? 
         */
        private void AddTOREquipmentsToRoster(
            Hero hero,
            EquipmentFlags suitableFlags,
            ref MBList<MBEquipmentRoster> roster,
            bool shouldMatchGender = false)
        {
            foreach (MBEquipmentRoster equipmentRoster in (List<MBEquipmentRoster>) MBEquipmentRosterExtensions.All)
            {
                CultureObject culture= hero.Culture;
                if (culture.StringId == "mousillon")
                {
                    CultureObject vlandia = MBObjectManager.Instance.GetObject<CultureObject>(TORConstants.Cultures.BRETONNIA);
                    culture = vlandia;
                }
                    
                if (this.IsRosterAppropriateForHeroAsTemplate(equipmentRoster, hero,culture, suitableFlags, shouldMatchGender))
                    roster.Add(equipmentRoster);
            }
        }
        
        private bool IsRosterAppropriateForHeroAsTemplate(
            MBEquipmentRoster equipmentRoster,
            Hero hero,
            CultureObject culture,
            EquipmentFlags customFlags = EquipmentFlags.None,
            bool shouldMatchGender = false)
        {
            bool flag1 = false;
            if (equipmentRoster.IsEquipmentTemplate() && (!shouldMatchGender || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsFemaleTemplate) == hero.IsFemale) && equipmentRoster.EquipmentCulture == culture && (customFlags == EquipmentFlags.None || equipmentRoster.HasEquipmentFlags(customFlags)))
            {
                int num = equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsNomadTemplate) ? 1 : (equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsWoodlandTemplate) ? 1 : 0);
                bool flag2 = !hero.IsChild && (equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsChildEquipmentTemplate) || equipmentRoster.HasEquipmentFlags(EquipmentFlags.IsTeenagerEquipmentTemplate));
                if (num == 0 && !flag2)
                    flag1 = true;
            }
            return flag1;
        }
    }
}