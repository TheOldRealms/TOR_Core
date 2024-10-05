using System.Collections.Generic;
using System.Linq;
using Ink.Parsed;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class MobilePartyExtendedInfo
    {
        [SaveableField(0)] public string CurrentBlessingStringId = null;
        [SaveableField(1)] public int CurrentBlessingRemainingDuration = -1;
        [SaveableField(2)] public Dictionary<string, List<string>> TroopAttributes = [];
        
        public void AddTroopAttribute(CharacterObject troop, string attribute)
        {
            TroopAttributes ??= [];
            if (!TroopAttributes.TryGetValue(troop.StringId, out var entryList))
            {
                var list = new List<string>
                {
                    attribute
                };
                TroopAttributes.Add(troop.StringId, list);
            }
            else
            {
                entryList.Add(attribute);
                TroopAttributes[troop.StringId] = entryList;
            }
        }

        public void RemoveTroopAttribute(string troopId, string attribute)
        {
            if (TroopAttributes == null) return;
            
            if (!TroopAttributes.TryGetValue(troopId, out var list)) return;
            if (list.Contains(attribute)) list.Remove(attribute);
            TroopAttributes[troopId] = list;
        }
    }
}