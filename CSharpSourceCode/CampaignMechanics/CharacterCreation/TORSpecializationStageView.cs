using System;
using System.Collections.Generic;
using System.Linq;
using SandBox.View.CharacterCreation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    /// <summary>
    /// Custom character creation stage view for optional specialization selection
    /// (lore/bloodline/priesthood). Auto-skips if player doesn't need specialization.
    /// </summary>
    [CharacterCreationStageView(typeof(TORSpecializationStage))]
    public class TORSpecializationStageView : CharacterCreationStageViewBase
    {
        private readonly CharacterCreationManager _characterCreationManager;
        private bool _hasShownSelection;
        private bool _selectionCompleted;

        public TORSpecializationStageView(
            CharacterCreationManager characterCreationManager,
            ControlCharacterCreationStage affirmativeAction,
            TextObject affirmativeActionText,
            ControlCharacterCreationStage negativeAction,
            TextObject negativeActionText,
            ControlCharacterCreationStage onRefresh,
            ControlCharacterCreationStageReturnInt getCurrentStageIndexAction,
            ControlCharacterCreationStageReturnInt getTotalStageCountAction,
            ControlCharacterCreationStageReturnInt getFurthestIndexAction,
            ControlCharacterCreationStageWithInt goToIndexAction)
            : base(affirmativeAction, negativeAction, onRefresh, getTotalStageCountAction, getCurrentStageIndexAction, getFurthestIndexAction, goToIndexAction)
        {
            _characterCreationManager = characterCreationManager;
            _hasShownSelection = false;
            _selectionCompleted = false;

            TORCommon.Log("[TORSpecializationStageView] Constructed", NLog.LogLevel.Info);

            // Show specialization selection immediately in constructor
            // Get the handler to check specialization need
            var handler = GetHandler();
            if (handler == null)
            {
                TORCommon.Log("[TORSpecializationStageView] Handler not found, skipping stage", NLog.LogLevel.Warn);
                NextStage();
                return;
            }

            bool needsSpec = handler.NeedsSpecialization(handler.GetSelectedProfessionId());
            TORCommon.Log($"[TORSpecializationStageView] NeedsSpecialization: {needsSpec}", NLog.LogLevel.Info);

            if (!needsSpec)
            {
                // Skip this stage entirely
                TORCommon.Log("[TORSpecializationStageView] Skipping specialization stage", NLog.LogLevel.Info);
                NextStage();
            }
            else
            {
                // Show appropriate selection dialog
                _hasShownSelection = true;
                ShowSpecializationSelection(handler);
            }
        }

        public override void Tick(float dt)
        {
            base.Tick(dt);

            // On first tick, check if we need to show specialization or skip
            if (!_hasShownSelection && !_selectionCompleted)
            {
                _hasShownSelection = true;

                // Get the handler to check specialization need
                var handler = GetHandler();
                if (handler == null)
                {
                    TORCommon.Log("[TORSpecializationStageView] Handler not found, skipping stage", NLog.LogLevel.Warn);
                    NextStage();
                    return;
                }

                bool needsSpec = handler.NeedsSpecialization(handler.GetSelectedProfessionId());
                TORCommon.Log($"[TORSpecializationStageView] NeedsSpecialization: {needsSpec}", NLog.LogLevel.Info);

                if (!needsSpec)
                {
                    // Skip this stage entirely
                    TORCommon.Log("[TORSpecializationStageView] Skipping specialization stage", NLog.LogLevel.Info);
                    NextStage();
                }
                else
                {
                    // Show appropriate selection dialog
                    ShowSpecializationSelection(handler);
                }
            }
        }

        private TorCharacterCreationContentHandler GetHandler()
        {
            // Access handler from manager - need to use reflection
            try
            {
                var handlersField = typeof(CharacterCreationManager).GetField("_handlers",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (handlersField != null)
                {
                    var handlers = handlersField.GetValue(_characterCreationManager) as System.Collections.Generic.SortedList<int, ICharacterCreationContentHandler>;
                    if (handlers != null)
                    {
                        foreach (var handler in handlers.Values)
                        {
                            if (handler is TorCharacterCreationContentHandler torHandler)
                            {
                                return torHandler;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TORCommon.Log($"[TORSpecializationStageView] Failed to get handler: {ex.Message}", NLog.LogLevel.Error);
            }

            return null;
        }

        private void ShowSpecializationSelection(TorCharacterCreationContentHandler handler)
        {
            string professionId = handler.GetSelectedProfessionId();

            // Determine what type of selection to show
            if (handler.IsSpellcaster(professionId))
            {
                TORCommon.Log("[TORSpecializationStageView] Showing lore selection", NLog.LogLevel.Info);
                ShowLoreSelection(handler);
            }
            else if (handler.IsVampire(professionId))
            {
                TORCommon.Log("[TORSpecializationStageView] Showing bloodline selection", NLog.LogLevel.Info);
                ShowBloodlineSelection(handler);
            }
            else if (handler.IsPriest(professionId))
            {
                TORCommon.Log("[TORSpecializationStageView] Showing priesthood selection", NLog.LogLevel.Info);
                ShowPriesthoodSelection(handler);
            }
            else
            {
                // Shouldn't happen, but skip if it does
                TORCommon.Log("[TORSpecializationStageView] Unknown specialization type, skipping", NLog.LogLevel.Warn);
                NextStage();
            }
        }

        private void ShowLoreSelection(TorCharacterCreationContentHandler handler)
        {
            List<InquiryElement> list = new List<InquiryElement>();
            var lores = LoreObject.GetAll();

            foreach (var lore in lores)
            {
                if (lore.ID == "MinorMagic" || lore.IsRestrictedToVampires)
                    continue;

                if (lore.DisabledForCultures.Contains(CharacterObject.PlayerCharacter.Culture.StringId))
                    continue;

                if (Hero.MainHero.GetExtendedInfo().HasKnownLore(lore.ID))
                    continue;

                list.Add(new InquiryElement(lore, lore.Name, null));
            }

            if (list.IsEmpty())
            {
                TORCommon.Log("[TORSpecializationStageView] No lores available, skipping", NLog.LogLevel.Warn);
                NextStage();
                return;
            }

            var inquiryData = new MultiSelectionInquiryData(
                "Choose Lore",
                "Choose a lore of magic to specialize in.",
                list,
                false,
                1,
                1,
                "Confirm",
                null,
                OnLoreSelected,
                null);

            MBInformationManager.ShowMultiSelectionInquiry(inquiryData, true);
        }

        private void OnLoreSelected(List<InquiryElement> obj)
        {
            if (obj?[0].Identifier is LoreObject choice)
            {
                var info = Hero.MainHero.GetExtendedInfo();
                Hero.MainHero.AddKnownLore(choice.ID);
                if (info.SpellCastingLevel < SpellCastingLevel.Entry)
                {
                    Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                }
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned lore: " + choice.Name), 0, CharacterObject.PlayerCharacter);
                TORCommon.Log($"[TORSpecializationStageView] Lore selected: {choice.ID}", NLog.LogLevel.Info);
            }

            InformationManager.HideInquiry();
            _selectionCompleted = true;
            NextStage();
        }

        private void ShowBloodlineSelection(TorCharacterCreationContentHandler handler)
        {
            List<InquiryElement> list = new List<InquiryElement>
            {
                new InquiryElement("bloodline_von_carstein", "Von Carstein Vampire", null),
                new InquiryElement("bloodline_blood_knight", "Blood Knight", null),
                new InquiryElement("bloodline_necrarch", "Necrarch", null),
            };

            var inquiryData = new MultiSelectionInquiryData(
                "Choose Bloodline",
                "Choose your vampiric bloodline.",
                list,
                false,
                1,
                1,
                "Confirm",
                null,
                OnBloodlineSelected,
                null);

            MBInformationManager.ShowMultiSelectionInquiry(inquiryData, true);
        }

        private void OnBloodlineSelected(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as string;
            TORCommon.Log($"[TORSpecializationStageView] Bloodline selected: {choice}", NLog.LogLevel.Info);

            if (choice == "bloodline_von_carstein")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("NagashGaze");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy"), 0, CharacterObject.PlayerCharacter);
                Hero.MainHero.AddCareer(TORCareers.MinorVampire);
            }
            else if (choice == "bloodline_blood_knight")
            {
                Hero.MainHero.AddCareer(TORCareers.BloodKnight);
            }
            else if (choice == "bloodline_necrarch")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("NagashGaze");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                Hero.MainHero.AddCareer(TORCareers.Necrarch);
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy"), 0, CharacterObject.PlayerCharacter);
            }

            InformationManager.HideInquiry();
            _selectionCompleted = true;
            NextStage();
        }

        private void ShowPriesthoodSelection(TorCharacterCreationContentHandler handler)
        {
            List<InquiryElement> list = new List<InquiryElement>
            {
                new InquiryElement("god_sigmar", "Sigmar", null),
                new InquiryElement("god_ulric", "Ulric", null),
            };

            var inquiryData = new MultiSelectionInquiryData(
                "Choose God",
                "You are a priest of the Empire. Choose the God you are devoted to.",
                list,
                false,
                1,
                1,
                "Confirm",
                null,
                OnPriesthoodSelected,
                null);

            MBInformationManager.ShowMultiSelectionInquiry(inquiryData, true);
        }

        private void OnPriesthoodSelected(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as string;
            TORCommon.Log($"[TORSpecializationStageView] Priesthood selected: {choice}", NLog.LogLevel.Info);

            if (choice == "god_sigmar")
            {
                Hero.MainHero.AddCareer(TORCareers.WarriorPriest);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_sigmar"), 60);
                Hero.MainHero.AddAttribute("PriestSigmar");
            }
            else if (choice == "god_ulric")
            {
                Hero.MainHero.AddCareer(TORCareers.WarriorPriestUlric);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_ulric"), 60);
                Hero.MainHero.AddAttribute("PriestUlric");
            }

            var skill = Hero.MainHero.GetSkillValue(TORSkills.Faith);
            Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.Faith, Math.Max(skill, 25));
            Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.Faith.NovicePrayers);

            InformationManager.HideInquiry();
            _selectionCompleted = true;
            NextStage();
        }

        public override void NextStage()
        {
            TORCommon.Log("[TORSpecializationStageView] NextStage called", NLog.LogLevel.Info);
            _affirmativeAction();
        }

        public override void PreviousStage()
        {
            TORCommon.Log("[TORSpecializationStageView] PreviousStage called", NLog.LogLevel.Info);
            _negativeAction();
        }

        public override IEnumerable<ScreenLayer> GetLayers()
        {
            // No visual layers - we use inquiry dialogs
            return new List<ScreenLayer>();
        }

        public override int GetVirtualStageCount() => 1;
        public override void LoadEscapeMenuMovie()
        {
        }

        public override void ReleaseEscapeMenuMovie()
        {
        }

        protected override void OnFinalize()
        {
            base.OnFinalize();
            TORCommon.Log("[TORSpecializationStageView] Finalized", NLog.LogLevel.Info);
        }
    }
}