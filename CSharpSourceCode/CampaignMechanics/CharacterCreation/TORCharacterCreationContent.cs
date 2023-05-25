using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics.CharacterCreation
{
    public class TORCharacterCreationContent : CharacterCreationContentBase
    {
        private List<CharacterCreationOption> _options;
        private readonly int _maxStageNumber = 3;
        private bool _isFemale = false;

        public TORCharacterCreationContent()
        {
            try
            {
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_cc_options.xml";
                XmlSerializer ser = new XmlSerializer(typeof(List<CharacterCreationOption>));
                _options = ser.Deserialize(File.OpenRead(path)) as List<CharacterCreationOption>;
            }
            catch (Exception)
            {
                TORCommon.Log("Failed to open tor_cc_options.xml for character creation.", NLog.LogLevel.Error);
                throw;
            }
            ExtendedInfoManager.Instance.ClearInfo(Hero.MainHero);
        }

        public override TextObject ReviewPageDescription => new TextObject("{=!}You prepare to enter The Old World! Here is your character. Click finish if you are ready, or go back to make changes.", null);

        public override IEnumerable<Type> CharacterCreationStages
        {
            get
            {
                yield return typeof(CharacterCreationCultureStage);
                yield return typeof(CharacterCreationFaceGeneratorStage);
                yield return typeof(CharacterCreationGenericStage);
                yield return typeof(CharacterCreationOptionsStage);
                yield return typeof(CharacterCreationBannerEditorStage);
                yield return typeof(CharacterCreationClanNamingStage);
                yield return typeof(CharacterCreationReviewStage);
                yield break;
            }
        }

        protected override void OnInitialized(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            AddMenus(characterCreation);
        }

        private void AddMenus(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation characterCreation)
        {
            //stages
            CharacterCreationMenu stage1Menu = new CharacterCreationMenu(new TextObject("{=!}Origin", null), new TextObject("{=!}Choose your family's background...", null), new CharacterCreationOnInit(OnMenuInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
            CharacterCreationMenu stage2Menu = new CharacterCreationMenu(new TextObject("{=!}Growth", null), new TextObject("{=!}Teenage years...", null), new CharacterCreationOnInit(OnMenuInit), CharacterCreationMenu.MenuTypes.MultipleChoice);
            CharacterCreationMenu stage3Menu = new CharacterCreationMenu(new TextObject("{=!}Profession", null), new TextObject("{=!}Your starting profession...", null), new CharacterCreationOnInit(OnMenuInit), CharacterCreationMenu.MenuTypes.MultipleChoice);

            for (int i = 1; i <= _maxStageNumber; i++)
            {
                List<string> cultures = new List<string>();
                _options.ForEach(x =>
                {
                    if (x.StageNumber == i && !cultures.Contains(x.Culture))
                    {
                        cultures.Add(x.Culture);
                    }
                });
                foreach (var culture in cultures)
                {
                    CharacterCreationCategory category = new CharacterCreationCategory();
                    switch (i)
                    {
                        case 1:
                            category = stage1Menu.AddMenuCategory(delegate ()
                            {
                                return GetSelectedCulture().StringId == culture;
                            });
                            break;
                        case 2:
                            category = stage2Menu.AddMenuCategory(delegate ()
                            {
                                return GetSelectedCulture().StringId == culture;
                            });
                            break;
                        case 3:
                            category = stage3Menu.AddMenuCategory(delegate ()
                            {
                                return GetSelectedCulture().StringId == culture;
                            });
                            break;
                        default:
                            break;
                    }

                    var relevantOptions = _options.FindAll(x => x.StageNumber == i && x.Culture.Equals(culture));
                    foreach (var option in relevantOptions)
                    {
                        var effectedSkills = new MBList<SkillObject>();
                        foreach (var skillId in option.SkillsToIncrease)
                        {
                            effectedSkills.Add(Skills.All.FirstOrDefault(x => x.StringId == skillId));
                        }
                        CharacterAttribute attribute = Attributes.All.Where(x => x.StringId == option.AttributeToIncrease.ToLower()).FirstOrDefault();
                        category.AddCategoryOption(new TextObject("{=!}" + option.OptionText), effectedSkills, attribute, FocusToAdd, SkillLevelToAdd, AttributeLevelToAdd, null, delegate (TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation charInfo)
                        {
                            OnOptionSelected(charInfo, option.Id);
                        },
                        delegate (TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation charInfo)
                        {
                            OnOptionFinalize(charInfo, option.Id);
                        },
                        new TextObject("{=!}" + option.OptionFlavourText));
                    }
                }
            }

            characterCreation.AddNewMenu(stage1Menu);
            characterCreation.AddNewMenu(stage2Menu);
            characterCreation.AddNewMenu(stage3Menu);
        }

        private void OnMenuInit(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation charInfo)
        {
            charInfo.IsPlayerAlone = true;
            charInfo.HasSecondaryCharacter = false;
            charInfo.ClearFaceGenMounts();
            _isFemale = CharacterObject.PlayerCharacter.IsFemale;
        }

        private void OnOptionSelected(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation charInfo, string optionId)
        {
            var selectedOption = _options.Find(x => x.Id == optionId);
            charInfo.ClearFaceGenPrefab();
            int race = 0;
            Hero.MainHero.UpdatePlayerGender(_isFemale);
            if (selectedOption.OptionText == "Vampiric Nobility")
            {
                race = FaceGen.GetRaceOrDefault("vampire");
            }
            else if(selectedOption.OptionText == "Damsel of the Lady" && !CharacterObject.PlayerCharacter.IsFemale)
            {
                Hero.MainHero.UpdatePlayerGender(true);
            }
            UpdateVisuals(race, charInfo);
            UpdateEquipment(selectedOption, charInfo);
            
        }

        private void UpdateVisuals(int race, TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation charInfo)
        {
            List<FaceGenChar> list = new List<FaceGenChar>();
            BodyProperties bodyProperties = CharacterObject.PlayerCharacter.GetBodyProperties(CharacterObject.PlayerCharacter.Equipment, -1);
            list.Add(new FaceGenChar(bodyProperties, race, new Equipment(), CharacterObject.PlayerCharacter.IsFemale));
            charInfo.ChangeFaceGenChars(list);
            CharacterObject.PlayerCharacter.Race = race;
        }
        private void UpdateEquipment(CharacterCreationOption selectedOption, TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation charInfo)
        {
            List<Equipment> list = new List<Equipment>();
            Equipment equipment = null;
            try
            {
                equipment = MBObjectManager.Instance.GetObject<MBEquipmentRoster>(selectedOption.EquipmentSetId).DefaultEquipment;
                if (equipment == null) MBObjectManager.Instance.CreateObject<MBEquipmentRoster>(selectedOption.EquipmentSetId);
            }
            catch (NullReferenceException)
            {
                TORCommon.Log("Attempted to read characterobject " + selectedOption.EquipmentSetId + " in Character Creation, but no such entry exists in XML. Falling back to default.", NLog.LogLevel.Error);
                throw;
            }
            if (equipment != null)
            {
                if (equipment.IsValid && !equipment.IsEmpty())
                {
                    list.Add(equipment);
                    charInfo.ChangeCharactersEquipment(list);
                    PlayerStartEquipment = equipment;
                    CharacterObject.PlayerCharacter.Equipment.FillFrom(PlayerStartEquipment);
                }
            }
        }

        private void OnOptionFinalize(TaleWorlds.CampaignSystem.CharacterCreationContent.CharacterCreation charInfo, string id)
        {
            Hero.MainHero.AddAttribute("AbilityUser");
            var selectedOption = _options.Find(x => x.Id == id);
            if (selectedOption.OptionText == "Magister Apprentice" || selectedOption.OptionText == "Damsel of the Lady")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("Dart");
                Hero.MainHero.AddKnownLore("MinorMagic");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
            }
            if (selectedOption.OptionText == "Priest Acolyte")
            {
                Hero.MainHero.AddAttribute("Priest");
                Hero.MainHero.AddAbility("HealingAOE");
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_sigmar"), 60);
            }
            else if (selectedOption.OptionText == "Novice Necromancer")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAttribute("Necromancer");
                Hero.MainHero.AddAbility("SummonSkeleton");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 25);
            }
            else if (selectedOption.OptionText == "Vampiric Nobility")
            {
                Hero.MainHero.AddAttribute("Vampire");
                Hero.MainHero.AddAttribute("Necromancer");
                Hero.MainHero.AddReligiousInfluence(ReligionObject.All.FirstOrDefault(x => x.StringId == "cult_of_nagash"), 60);
            }
        }

        public override void OnCharacterCreationFinalized()
        {
            CultureObject culture = CharacterObject.PlayerCharacter.Culture;
            Vec2 position2D = default(Vec2);

            switch (culture.StringId)
            {
                case "empire":
                    position2D = new Vec2(1450.97f, 991.37f);
                    break;
                case "khuzait":
                    position2D = new Vec2(1617.54f, 969.70f);
                    break;
                case "vlandia":
                    position2D = new Vec2(998.96f, 830.02f);
                    break;
                default:
                    position2D = new Vec2(1420.97f, 981.37f);
                    break;
            }
            MobileParty.MainParty.Position2D = position2D;
            MapState mapState;
            if ((mapState = (GameStateManager.Current.ActiveState as MapState)) != null)
            {
                mapState.Handler.ResetCamera(true, true);
                mapState.Handler.TeleportCameraToMainParty();
            }
            SetHeroAge(25);
            if (Hero.MainHero.IsSpellCaster()) PromptChooseLore();
            if (Hero.MainHero.IsVampire()) PromptChooseBloodline();
        }

        protected void SetHeroAge(float age)
        {
            Hero.MainHero.SetBirthDay(CampaignTime.YearsFromNow(-age));
        }

        private void PromptChooseLore()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            var lores = LoreObject.GetAll();
            foreach (var item in lores)
            {
                if (item.ID != "MinorMagic" && !item.DisabledForCultures.Contains(CharacterObject.PlayerCharacter.Culture.StringId) && !Hero.MainHero.GetExtendedInfo().HasKnownLore(item.ID)) list.Add(new InquiryElement(item, item.Name, null));
            }
            var inquirydata = new MultiSelectionInquiryData("Choose Lore", "Choose a lore to specialize in.", list, false, 1, "Confirm", "Cancel", OnChooseLore, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private void OnChooseLore(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as LoreObject;
            var info = Hero.MainHero.GetExtendedInfo();
            if (choice != null)
            {
                Hero.MainHero.AddKnownLore(choice.ID);
                if (info.SpellCastingLevel < SpellCastingLevel.Entry) Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned lore: " + choice.Name), 0, CharacterObject.PlayerCharacter);
            }
            InformationManager.HideInquiry();
        }

        private void OnCancel(List<InquiryElement> obj)
        {
            MBInformationManager.AddQuickInformation(new TextObject("You MUST choose."));
        }

        private void PromptChooseBloodline()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            list.Add(new InquiryElement("generic_vampire", "Von Carstein Vampire", null));
            list.Add(new InquiryElement("blood_knight", "Blood Knight", null));
            var inquirydata = new MultiSelectionInquiryData("Choose Bloodline", "Choose your vampiric bloodline.", list, false, 1, "Confirm", "Cancel", OnChooseBloodline, OnCancel);
            MBInformationManager.ShowMultiSelectionInquiry(inquirydata, true);
        }

        private void OnChooseBloodline(List<InquiryElement> obj)
        {
            var choice = obj[0].Identifier as string;
            if(choice == "generic_vampire")
            {
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddAbility("NagashGaze");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.AddKnownLore("Necromancy");
                Hero.MainHero.AddKnownLore("DarkMagic");
                var skill = Hero.MainHero.GetSkillValue(TORSkills.SpellCraft);
                Hero.MainHero.HeroDeveloper.SetInitialSkillLevel(TORSkills.SpellCraft, Math.Max(skill, 25));
                Hero.MainHero.HeroDeveloper.AddPerk(TORPerks.SpellCraft.EntrySpells);
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned Necromancy and Dark Magic"), 0, CharacterObject.PlayerCharacter);
            }
            //TODO add careers here
        }
    }
}
