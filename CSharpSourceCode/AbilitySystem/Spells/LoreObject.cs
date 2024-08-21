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
                _lores.Add("MinorMagic", new LoreObject("MinorMagic", "Minor Magic", "minormagic_symbol", new List<string>() { TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR}));
                _lores.Add("LoreOfFire", new LoreObject("LoreOfFire", "Lore of Fire", "firemagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON }));
                _lores.Add("LoreOfLight", new LoreObject("LoreOfLight", "Lore of Light", "lightmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON }));
                _lores.Add("LoreOfHeavens", new LoreObject("LoreOfHeavens", "Lore of Heavens", "celestial_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON }));
                _lores.Add("LoreOfLife", new LoreObject("LoreOfLife", "Lore of Life", "lifemagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON }));
                _lores.Add("LoreOfMetal", new LoreObject("LoreOfMetal", "Lore of Metal", "metalmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.BRETONNIA }));
                _lores.Add("LoreOfBeasts", new LoreObject("LoreOfBeasts", "Lore of Beasts", "beastmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA,TORConstants.Cultures.MOUSILLON }));
                _lores.Add("HighMagic", new LoreObject("HighMagic", "High Magic", "highmagic_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA,TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.SYLVANIA  }, true));

                _lores.Add("DarkMagic", new LoreObject("DarkMagic", "Dark Magic", "darkmagic_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA}, true));
                _lores.Add("Necromancy", new LoreObject("Necromancy", "Necromancy", "necromancy_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA , TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR}));
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
