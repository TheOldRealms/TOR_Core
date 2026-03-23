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
                _lores.Add("MinorMagic", new LoreObject("MinorMagic", "Minor Magic", "minormagic_symbol", new List<string>() { TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfFire", new LoreObject("LoreOfFire", "Lore of Fire", "firemagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfLight", new LoreObject("LoreOfLight", "Lore of Light", "lightmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfHeavens", new LoreObject("LoreOfHeavens", "Lore of Heavens", "celestial_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfLife", new LoreObject("LoreOfLife", "Lore of Life", "lifemagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfMetal", new LoreObject("LoreOfMetal", "Lore of Metal", "metalmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfBeasts", new LoreObject("LoreOfBeasts", "Lore of Beasts", "beastmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));

                // NOTE: High Magic and Dark Magic are disabled for ASRAI (Wood Elves) in general, BUT Spellsingers can still access them
                // through the special Spellweaver dialog (SpellTrainerInTownBehavior.cs:390-403) which bypasses culture restrictions
                // by offering a hardcoded list of both lores. This ensures only Spellsingers can become High/Dark Weavers, not all Wood Elf casters.
                _lores.Add("HighMagic", new LoreObject("HighMagic", "High Magic", "highmagic_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.ASRAI, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }, true));

                _lores.Add("DarkMagic", new LoreObject("DarkMagic", "Dark Magic", "darkmagic_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.ASRAI, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }, true));
                _lores.Add("Necromancy", new LoreObject("Necromancy", "Necromancy", "necromancy_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));

                _lores.Add("LoreOfDeath", new LoreObject("LoreOfDeath", "Lore of Death", "deathmagic_symbol", new List<string>(TORConstants.Cultures.All.Where(x => x != TORConstants.Cultures.EMPIRE && x != TORConstants.Cultures.ASRAI && x != TORConstants.Cultures.EONIR && x != TORConstants.Cultures.SYLVANIA && x != TORConstants.Cultures.MOUSILLON).ToList()))); 
                _lores.Add("RuneMagic", new LoreObject("RuneMagic", "Rune Magic", "runemagic_symbol", new List<string>(TORConstants.Cultures.All.Where(x => x != TORConstants.Cultures.DAWI).ToList()))); // Why again are we adding all cultures that DONT have access to it?
                _lores.Add("BigWaaagh", new LoreObject("BigWaaagh", "Big Waaagh!", "bigwaagh_symbol", new List<string>(TORConstants.Cultures.All.Where(x => x != TORConstants.Cultures.GREENSKIN).ToList())));
            }
            return _lores.Values.ToList();
        }

        public static LoreObject GetLore(string id)
        {
            var lores = GetAll();
            return lores.FirstOrDefault(lo => lo.ID == id);
        }
    }
}