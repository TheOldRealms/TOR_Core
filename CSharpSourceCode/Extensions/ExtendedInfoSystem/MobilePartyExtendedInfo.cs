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
        [SaveableField(2)] public Dictionary<string, List<string>> TroopAttributes = new Dictionary<string, List<string>>();



        

        public void AddTroopAttribute(CharacterObject troop, string attribute)
        {
            if (TroopAttributes == null) TroopAttributes = new Dictionary<string, List<string>>();
            if (!TroopAttributes.ContainsKey(troop.StringId))
            {
                var list = new List<string>();
                list.Add(attribute);
                TroopAttributes.Add(troop.StringId, list );
            }
            else
            {
                var list = TroopAttributes[troop.StringId];
                list.Add(attribute);
                TroopAttributes[troop.StringId] = list;
            }
        }

        public void RemoveTroopAttribute(string troopId, string attribute)
        {
            if(TroopAttributes==null) return;
            
            
            if (!TroopAttributes.TryGetValue(troopId, out var list)) return;
            if (list.Contains(attribute))
            {
                list.Remove(attribute);
            }
            TroopAttributes[troopId] = list;
        }
    }
    
    
}
