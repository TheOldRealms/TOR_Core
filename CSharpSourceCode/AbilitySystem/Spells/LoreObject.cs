using System.Collections.Generic;
using System.Linq;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Spells
{
    public class LoreObject
    {
        private static Dictionary<string, LoreObject> _lores = new Dictionary<string, LoreObject>();
        public string ID { get; private set; }
        public string Name { get; private set; }
        public string SpriteName { get; private set; }
        public bool IsRestrictedToVampires { get; private set; }
        public List<string> DisabledForCultures { get; private set; } = new List<string>();
        
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
                    DisabledForCultures.Add(cultureId);
                }
            }
        }

        public static List<LoreObject> GetAll()
        {
            if (_lores.Count == 0)
            {
                _lores.Add("MinorMagic", new LoreObject("MinorMagic", "Minor Magic", "minormagic_symbol", new List<string>() { "none" }));
                _lores.Add("LoreOfFire", new LoreObject("LoreOfFire", "Lore of Fire", "firemagic_symbol", new List<string>() { TORConstants.SYLVANIA_CULTURE, TORConstants.BRETONNIA_CULTURE, "mousillon" }));
                _lores.Add("LoreOfLight", new LoreObject("LoreOfLight", "Lore of Light", "lightmagic_symbol", new List<string>() { TORConstants.SYLVANIA_CULTURE, TORConstants.BRETONNIA_CULTURE, "mousillon" }));
                _lores.Add("LoreOfHeavens", new LoreObject("LoreOfHeavens", "Lore of Heavens", "celestial_symbol", new List<string>() { TORConstants.SYLVANIA_CULTURE, TORConstants.BRETONNIA_CULTURE,"mousillon" }));
                _lores.Add("LoreOfLife", new LoreObject("LoreOfLife", "Lore of Life", "lifemagic_symbol", new List<string>() { TORConstants.SYLVANIA_CULTURE, "mousillon" }));
                _lores.Add("LoreOfMetal", new LoreObject("LoreOfMetal", "Lore of Metal", "metalmagic_symbol", new List<string>() { TORConstants.SYLVANIA_CULTURE, "mousillon", TORConstants.BRETONNIA_CULTURE }));
                _lores.Add("LoreOfBeasts", new LoreObject("LoreOfBeasts", "Lore of Beasts", "beastmagic_symbol", new List<string>() { TORConstants.SYLVANIA_CULTURE, "mousillon" }));
                _lores.Add("DarkMagic", new LoreObject("DarkMagic", "Dark Magic", "darkmagic_symbol", new List<string>() { "empire", TORConstants.BRETONNIA_CULTURE,  }, true));
                _lores.Add("Necromancy", new LoreObject("Necromancy", "Necromancy", "necromancy_symbol", new List<string>() { "empire", TORConstants.BRETONNIA_CULTURE }));
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
