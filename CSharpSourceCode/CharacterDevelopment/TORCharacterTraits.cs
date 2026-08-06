using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TOR_Core.Extensions;

namespace TOR_Core.CharacterDevelopment
{
    public class TORCharacterTraits
    {
        private TraitObject _spellCasterSkills;
        private TraitObject _shallyaDevoted;
        private TraitObject _ulricDevoted;
        private TraitObject _nagashCorrupted;
        private TraitObject _sigmarDevoted;
        private TraitObject _ladyDevoted;
        private TraitObject _gunner;

        public static TORCharacterTraits Instance { get; private set; }
        public static TraitObject SpellCasterSkills => Instance._spellCasterSkills;

        public static TraitObject ShallyaDevoted => Instance._shallyaDevoted;
        public static TraitObject SigmarDevoted => Instance._sigmarDevoted;
        public static TraitObject LadyDevoted => Instance._ladyDevoted;

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
            _ladyDevoted = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("LadyDevoted"));
            _gunner = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("Gunner"));

            _nagashCorrupted = Game.Current.ObjectManager.RegisterPresumedObject(new TraitObject("NagashCorrupted"));


            _spellCasterSkills.Initialize(TORTextHelper.GetTextObject("tor_spellcaster_trait_name", "Spellcaster"), TORTextHelper.GetTextObject("tor_spellcaster_trait_description", "Spellcaster Description"), true, 0, 10);
            _shallyaDevoted.Initialize(TORTextHelper.GetTextObject("tor_shallyadevoted_trait_name", "Shallya Devoted"), TORTextHelper.GetTextObject("tor_shallyadevoted_trait_description", "Devoted priest to Shallya"), true, 0, 10);
            _sigmarDevoted.Initialize(TORTextHelper.GetTextObject("tor_sigmardevoted_trait_name", "Sigmar Devoted"), TORTextHelper.GetTextObject("tor_sigmardevoted_trait_description", "Devoted priest to Sigmar"), true, 0, 10);
            _ladyDevoted.Initialize(TORTextHelper.GetTextObject("tor_ladydevoted_trait_name", "Lady Devoted"), TORTextHelper.GetTextObject("tor_ladydevoted_trait_description", "Devoted priestess to the Lady"), true, 0, 10);
            _ulricDevoted.Initialize(TORTextHelper.GetTextObject("tor_ulricdevoted_trait_name", "Ulric Devoted"), TORTextHelper.GetTextObject("tor_ulricdevoted_trait_description", "Devoted priest to Ulric"), true, 0, 10);
            _nagashCorrupted.Initialize(TORTextHelper.GetTextObject("tor_nagashcorrupted_trait_name", "Nagash Corrupted"), TORTextHelper.GetTextObject("tor_nagashcorrupted_trait_description", "This mind has been corrupted by the curse of nagash"), true, 0, 10);

            _gunner.Initialize(TORTextHelper.GetTextObject("tor_gunner_trait_name", "Gunner"), TORTextHelper.GetTextObject("tor_gunner_trait_description", "Expert of Blackpowder weapons"), true, 0, 10);
        }


    }
}