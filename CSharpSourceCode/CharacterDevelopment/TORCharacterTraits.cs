using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCharacterTraits
    {
        private TraitObject _spellCasterSkills;
        private TraitObject _shallyaDevoted;
        private TraitObject _ulricDevoted;
        private TraitObject _nagashCorrupted;
        private TraitObject _sigmarDevoted;
        private TraitObject _gunner;

        public static TORCharacterTraits Instance { get; private set; }
        public static TraitObject SpellCasterSkills => Instance._spellCasterSkills;
        
        public static TraitObject ShallyaDevoted => Instance._shallyaDevoted;
        public static TraitObject SigmarDevoted => Instance._sigmarDevoted;
        
        public static TraitObject UlricDevoted => Instance._ulricDevoted;
        
        public static TraitObject NagashCorrupted => Instance._ulricDevoted;
        public static TraitObject Gunner => Instance._gunner;

        public TORCharacterTraits()
        {
            Instance = this;
            _spellCasterSkills = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("SpellCasterSkills"));
            _shallyaDevoted = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("ShallyaDevoted"));
            _ulricDevoted = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("UlricDevoted"));
            _sigmarDevoted = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("SigmarDevoted"));
            _gunner = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("Gunner"));
            
            _nagashCorrupted = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("NagashCorrupted"));
            
            
            _spellCasterSkills.Initialize(new TextObject("{=tor_spellcaster_trait_name_str}Spellcaster", null), new TextObject("{=tor_spellcaster_trait_description_str}Spellcaster Description", null), true, 0, 10);
            _shallyaDevoted.Initialize(new TextObject("{=tor_shallyadevoted_trait_name_str}Shallya Devoted))",null), new TextObject("{=tor_shallya_trait_description_str}Devoted priest to Shallya",null), true,0,10);
            _sigmarDevoted.Initialize(new TextObject("{=tor_sigmardevoted_trait_name_str}Sigmar Devoted))",null), new TextObject("{=tor_sigmardevoted_trait_description_str}Devoted priest to Sigmar",null), true,0,10);
            _ulricDevoted.Initialize(new TextObject("{=tor_ulricdevoted_trait_name_str}Ulric Devoted))",null), new TextObject("{=tor_ulricdevoted_trait_description_str}Devoted priest to Ulric",null), true,0,10);
            _nagashCorrupted.Initialize(new TextObject("{=tor_nagashCorrupted_trait_name_str}Nagash Corrupted)",null), new TextObject("{=tor_nagashcorrupted_trait_description_str}This mind has been corrupted by the curse of nagash",null), true,0,10);

            _gunner.Initialize(new TextObject("{=tor_gunner_trait_name_str}Gunner))",null), new TextObject("{=tor_gunner_trait_description_str}Expert of Blackpowder weapons",null), true,0,10);
        }

        
    }
}
