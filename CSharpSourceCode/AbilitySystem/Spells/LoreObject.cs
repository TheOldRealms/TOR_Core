using System.Collections.Generic;
using System.Linq;

namespace TOR_Core.AbilitySystem.Spells
{
    public class LoreObject
    {
        private static Dictionary<string, LoreObject> _lores = new Dictionary<string, LoreObject>();
        public string ID { get; private set; }
        public string Name { get; private set; }
        public string SpriteName { get; private set; }
        public bool IsRestrictedToVampires { get; private set; }
        public List<string> DisabledForTrainersWithCultures { get; private set; } = new List<string>();
        
        private LoreObject(string id, string name, string spritename, List<string> cultureIds, bool restricted = false)
        {
            ID = id;
            Name = name;
            SpriteName = spritename;
            IsRestrictedToVampires = restricted;
            foreach (string cultureId in cultureIds)
            {
                if (cultureId != "none" || cultureId != string.Empty)
                {
                    DisabledForTrainersWithCultures.Add(cultureId);
                }
            }
        }

        public static List<LoreObject> GetAll()
        {
            if (_lores.Count == 0)
            {
                _lores.Add("MinorMagic", new LoreObject("MinorMagic", "Minor Magic", "minormagic_symbol", new List<string>() { "none" }));
                _lores.Add("LoreOfFire", new LoreObject("LoreOfFire", "Lore of Fire", "firemagic_symbol", new List<string>() { "khuzait" }));
                _lores.Add("LoreOfLight", new LoreObject("LoreOfLight", "Lore of Light", "lightmagic_symbol", new List<string>() { "khuzait" }));
                _lores.Add("LoreOfHeavens", new LoreObject("LoreOfHeavens", "Lore of Heavens", "celestial_symbol", new List<string>() { "khuzait" }));
                _lores.Add("LoreOfLife", new LoreObject("LoreOfLife", "Life Magic", "darkmagic_symbol", new List<string>() { "khuzait" }, true));
                _lores.Add("DarkMagic", new LoreObject("DarkMagic", "Dark Magic", "darkmagic_symbol", new List<string>() { "empire" }, true));
                _lores.Add("Necromancy", new LoreObject("Necromancy", "Necromancy", "necromancy_symbol", new List<string>() { "empire" }));
            }
            return _lores.Values.ToList();
        }

        public static LoreObject GetLore(string id)
        {
            var lores = GetAll();
            return lores.First(lo => lo.ID == id);
        }
    }
}
