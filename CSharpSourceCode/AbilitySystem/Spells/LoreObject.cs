using System.Collections.Generic;
using System.Linq;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem.Spells
{
    public class LoreObject
    {
        private static Dictionary<string, LoreObject> _lores = new Dictionary<string, LoreObject>();
        public string StringId { get; private set; }
        public string Name { get; private set; }
        public string SpriteName { get; private set; }
        public bool IsRestrictedToVampires { get; private set; }
        public List<string> DisabledForCultures { get; private set; } = new List<string>();

        private LoreObject(string id, string name, string spritename, List<string> disabledCultureIds, bool restrictedToVampires = false)
        {
            StringId = id;
            Name = name;
            SpriteName = spritename;
            IsRestrictedToVampires = restrictedToVampires;//Sly : why is this useful? Is this always used appropriately?
            foreach (string cultureId in disabledCultureIds)
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
                _lores.Add("MinorMagic", new LoreObject("MinorMagic", TORTextHelper.GetText("tor_spell_stat_name_minor", "Minor Magic"), "minormagic_symbol", new List<string>() { TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfFire", new LoreObject("LoreOfFire", TORTextHelper.GetText("tor_spell_stat_name_fire", "Lore of Fire"), "firemagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfLight", new LoreObject("LoreOfLight", TORTextHelper.GetText("tor_spell_stat_name_light", "Lore of Light"), "lightmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfHeavens", new LoreObject("LoreOfHeavens", TORTextHelper.GetText("tor_spell_stat_name_heavens", "Lore of Heavens"), "celestial_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfLife", new LoreObject("LoreOfLife", TORTextHelper.GetText("tor_spell_stat_name_life", "Lore of Life"), "lifemagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfMetal", new LoreObject("LoreOfMetal", TORTextHelper.GetText("tor_spell_stat_name_metal", "Lore of Metal"), "metalmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfBeasts", new LoreObject("LoreOfBeasts", TORTextHelper.GetText("tor_spell_stat_name_beasts", "Lore of Beasts"), "beastmagic_symbol", new List<string>() { TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }));
                _lores.Add("LoreOfDeath", new LoreObject("LoreOfDeath", TORTextHelper.GetText("tor_spell_stat_name_death", "Lore of Death"), "deathmagic_symbol", new List<string>(TORConstants.Cultures.All.Where(x => x != TORConstants.Cultures.EMPIRE && x != TORConstants.Cultures.ASRAI && x != TORConstants.Cultures.EONIR && x != TORConstants.Cultures.SYLVANIA && x != TORConstants.Cultures.MOUSILLON).ToList())));

                // NOTE: High Magic and Dark Magic are disabled for ASRAI (Wood Elves) in general, BUT Spellsingers can still access them
                // through the special Spellweaver dialog (SpellTrainerInTownBehavior.cs:390-403) which bypasses culture restrictions
                // by offering a hardcoded list of both lores. This ensures only Spellsingers can become High/Dark Weavers, not all Wood Elf casters.
                _lores.Add("HighMagic", new LoreObject("HighMagic", TORTextHelper.GetText("tor_spell_stat_name_high", "High Magic"), "highmagic_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.MOUSILLON, TORConstants.Cultures.SYLVANIA, TORConstants.Cultures.ASRAI, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }, true));

                _lores.Add("DarkMagic", new LoreObject("DarkMagic", TORTextHelper.GetText("tor_spell_stat_name_dark", "Dark Magic"), "darkmagic_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.ASRAI, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN }, true));
                _lores.Add("Necromancy", new LoreObject("Necromancy", TORTextHelper.GetText("tor_spell_stat_name_necromancy", "Necromancy"), "necromancy_symbol", new List<string>() { TORConstants.Cultures.EMPIRE, TORConstants.Cultures.BRETONNIA, TORConstants.Cultures.ASRAI, TORConstants.Cultures.EONIR, TORConstants.Cultures.DAWI, TORConstants.Cultures.GREENSKIN })); 
                _lores.Add("RuneMagic", new LoreObject("RuneMagic", "Rune Magic", "runemagic_symbol", new List<string>(TORConstants.Cultures.All.Where(x => x != TORConstants.Cultures.DAWI).ToList()))); // Why again are we adding all cultures that DONT have access to it?
                _lores.Add("BigWaaagh", new LoreObject("BigWaaagh", TORTextHelper.GetText("tor_spell_stat_name_big_waaagh", "Da Big WAAAGH!"), "bigwaagh_symbol", new List<string>(TORConstants.Cultures.All.Where(x => x != TORConstants.Cultures.GREENSKIN).ToList())));
            }
            return _lores.Values.ToList();
        }

        public static LoreObject GetLore(string id)
        {
            var lores = GetAll();
            return lores.FirstOrDefault(lo => lo.StringId == id);
        }
    }
}