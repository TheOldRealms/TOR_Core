using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;

namespace TOR_Core.CharacterDevelopment
{
    public class TORPerks
    {
        private PerkObject _entrySpells;
        private PerkObject _adeptSpells;
        private PerkObject _masterSpells;

        public static TORPerks Instance { get; private set; }
        public static PerkObject EntrySpells => Instance._entrySpells;
        public static PerkObject AdeptSpells => Instance._adeptSpells;
        public static PerkObject MasterSpells => Instance._masterSpells;

        public TORPerks()
        {
            Instance = this;
            _entrySpells = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("EntrySpells"));
            _adeptSpells = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("AdeptSpells"));
            _masterSpells = Game.Current.ObjectManager.RegisterPresumedObject(new PerkObject("MasterSpells"));
            _entrySpells.Initialize("{=!}Novice Spellcaster", "{=!}Gain access to entry level spells.", TORSkills.SpellCraft, 25, null, SkillEffect.PerkRole.Personal);
            _adeptSpells.Initialize("{=!}Adept Spellcaster", "{=!}Gain access to adept level spells.", TORSkills.SpellCraft, 100, null, SkillEffect.PerkRole.Personal);
            _masterSpells.Initialize("{=!}Master Spellcaster", "{=!}Gain access to master level spells.", TORSkills.SpellCraft, 200, null, SkillEffect.PerkRole.Personal);
        }
    }
}
