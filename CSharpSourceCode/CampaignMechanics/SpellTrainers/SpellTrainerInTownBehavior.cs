using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Locations;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TOR_Core.AbilitySystem.SpellBook;
using TOR_Core.AbilitySystem.Spells;
using TOR_Core.CampaignMechanics.SkillBooks;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Quests;

namespace TOR_Core.CampaignMechanics.SpellTrainers
{
    public class SpellTrainerInTownBehavior : CampaignBehaviorBase
    {
        private readonly string _empireTrainerId = "tor_spelltrainer_empire_0";
        private readonly string _vampireTrainerId = "tor_spelltrainer_vc_0";
        private string _testResult = "";
        private Dictionary<string, string> _settlementToTrainerMap = new Dictionary<string, string>();
        private readonly float _testSuccessChance = 1f;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.GameMenuOpened.AddNonSerializedListener(this, OnGameMenuOpened);
            CampaignEvents.BeforeMissionOpenedEvent.AddNonSerializedListener(this, OnBeforeMissionStart);
            CampaignEvents.PerkOpenedEvent.AddNonSerializedListener(this, OnPerkPicked);
        }

        private void OnBeforeMissionStart() => SpawnTrainerIfNeeded();
        private void OnGameMenuOpened(MenuCallbackArgs obj) => SpawnTrainerIfNeeded();
        private void SpawnTrainerIfNeeded()
        {
            if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTown && !IsTrainerInCollege(Settlement.CurrentSettlement))
            {
                SpawnTrainerInCollege(Settlement.CurrentSettlement, true);
            }
        }

        private bool IsTrainerInCollege(Settlement settlement)
        {
            var location = settlement.LocationComplex.GetLocationWithId("house_1");
            var trainer = GetTrainerForTown(settlement);
            if (trainer != null)
            {
                return location.GetLocationCharacter(trainer) != null;
            }
            else return false;
        }

        private Hero GetTrainerForTown(Settlement settlement)
        {
            Hero hero = null;
            var heroId = "";
            if (_settlementToTrainerMap.TryGetValue(settlement.StringId, out heroId))
            {
                hero = Hero.FindFirst(x => x.StringId == heroId);
            }
            return hero;
        }

        private void SpawnTrainerInCollege(Settlement settlement, bool forceSpawn)
        {
            var trainer = GetTrainerForTown(settlement);
            var collegeloc = settlement.LocationComplex.GetLocationWithId("house_1");
            if (trainer != null && collegeloc != null)
            {

                if (forceSpawn)
                {
                    LocationCharacter locationCharacter = new LocationCharacter(new AgentData(new SimpleAgentOrigin(trainer.CharacterObject, -1, null, default(UniqueTroopDescriptor))).Monster(Game.Current.DefaultMonster), new LocationCharacter.AddBehaviorsDelegate(SandBoxManager.Instance.AgentBehaviorManager.AddWandererBehaviors), "npc_common", true, LocationCharacter.CharacterRelations.Neutral, null, true, false, null, false, false, true);
                    collegeloc.AddCharacter(locationCharacter);
                }
                else
                {
                    var locChar = settlement.LocationComplex.GetLocationCharacterOfHero(trainer);
                    var currentloc = settlement.LocationComplex.GetLocationOfCharacter(trainer);
                    if (currentloc != collegeloc) settlement.LocationComplex.ChangeLocation(locChar, currentloc, collegeloc);
                }
            }
        }

        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            foreach (var settlement in Settlement.All)
            {
                if (settlement.IsTown)
                {
                    CreateTrainer(settlement);
                }
            }
        }

        private void CreateTrainer(Settlement settlement)
        {
            CharacterObject template = null;
            if (settlement.Culture.StringId == "khuzait") template = MBObjectManager.Instance.GetObject<CharacterObject>(_vampireTrainerId);
            else template = MBObjectManager.Instance.GetObject<CharacterObject>(_empireTrainerId);

            if (template != null)
            {
                var hero = HeroCreator.CreateSpecialHero(template, settlement, null, null, 50);
                hero.SupporterOf = settlement.OwnerClan;
                hero.SetName(new TextObject(hero.FirstName.ToString() + " " + template.Name.ToString()), hero.FirstName);
                HeroHelper.SpawnHeroForTheFirstTime(hero, settlement);
                _settlementToTrainerMap.Add(settlement.StringId, hero.StringId);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter obj)
        {
            AddDialogs(obj);
            ForceLevelsForAllHeroes();
        }

        private void ForceLevelsForAllHeroes()
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                if (hero.IsSpellCaster() && hero != Hero.MainHero && hero.Occupation == Occupation.Lord)
                {
                    var info = hero.GetExtendedInfo();
                    if (info == null || info.SpellCastingLevel == SpellCastingLevel.Master) continue;
                    hero.SetSpellCastingLevel(SpellCastingLevel.Master);
                }
            }
        }

        private void OnPerkPicked(Hero hero, PerkObject perk)
        {
            var info = hero.GetExtendedInfo();
            if (hero.IsSpellCaster() && info != null)
            {
                if(perk == TORPerks.SpellCraft.EntrySpells)
                {
                    if (info.SpellCastingLevel < SpellCastingLevel.Entry)
                        hero.SetSpellCastingLevel(SpellCastingLevel.Entry);
                }
                else if (perk == TORPerks.SpellCraft.AdeptSpells)
                {
                    if (info.SpellCastingLevel < SpellCastingLevel.Adept)
                        hero.SetSpellCastingLevel(SpellCastingLevel.Adept);
                }
                else if (perk == TORPerks.SpellCraft.MasterSpells)
                {
                    if (info.SpellCastingLevel < SpellCastingLevel.Master)
                        hero.SetSpellCastingLevel(SpellCastingLevel.Master);
                }
            }
        }

        private void OpenScrollShop()
        {
            ItemRoster roster = new ItemRoster();

            MBObjectManager.Instance.GetObjectTypeList<ItemObject>()
                .Where(item => TORSkillBookCampaignBehavior.Instance.IsSkillBook(item))
                .ToList()
                .ForEach(item => roster.Add(new ItemRosterElement(item, MBRandom.RandomInt(1, 5))));

            InventoryManager.OpenScreenAsTrade(roster, Settlement.CurrentSettlement.Town);
        }

        private void AddDialogs(CampaignGameStarter obj)
        {
            obj.AddDialogLine("trainer_start", "start", "choices", "Do I know you? What do you need, be quick I am a busy.", spelltrainerstartcondition, null, 200, null);
            obj.AddPlayerLine("trainer_test", "choices", "magictest", "{TEST_QUESTION}", magictestcondition, null, 200, null);
            obj.AddDialogLine("trainer_testoutcome", "magictest", "testoutcome", "{TEST_PROMPT}", filltextfortestprompt, determinetestoutcome, 200, null);
            obj.AddDialogLine("trainer_testresult", "testoutcome", "start", "{TEST_RESULT}", testresultcondition, null, 200, null);
            obj.AddPlayerLine("trainer_learnspells", "choices", "openbook", "I have come seeking further knowledge.", () => MobileParty.MainParty.HasSpellCasterMember(), null, 200, null);
            obj.AddPlayerLine("trainer_scrollShop", "choices", "start", "Do you sell any scrolls?", null, OpenScrollShop, 200, null);
            obj.AddDialogLine("trainer_afterlearnspells", "openbook", "start", "Hmm, come then. I will teach you what I can.", null, openbookconsequence, 200, null);
            obj.AddPlayerLine("trainer_specialize", "choices", "specializelore", "{SPECALIZE_QUESTION}", specializelorecondition, null, 200, null);
            obj.AddDialogLine("trainer_chooselore", "specializelore", "start", "{SPECIALIZE_PROMPT}.", fillchooseloretext, chooseloreconsequence, 200, null);
            obj.AddPlayerLine("trainer_playergoodbye", "choices", "saygoodbye", "Farewell Magister.", null, null, 200, null);
            obj.AddDialogLine("trainer_goodbye", "saygoodbye", "close_window", "Hmm, yes. Farewell.", null, null, 200, null);
        }

        private bool spelltrainerstartcondition()
        {
            var partner = CharacterObject.OneToOneConversationCharacter;
            if (partner != null && partner.HeroObject != null && partner.HeroObject.IsSpellTrainer()) return true;
            else return false;
        }

        private bool magictestcondition()
        {
            var flag = false;
            flag = !Hero.MainHero.IsVampire() && !Hero.MainHero.IsSpellCaster() && !Hero.MainHero.HasAttribute("Priest") && _testResult == "";
            if (flag)
            {
                string text = "";
                var culture = Hero.OneToOneConversationHero.Culture.StringId;
                switch (culture)
                {
                    case "empire":
                        {
                            text = "I have come seeking knowledge, I wish to learn the arcane arts. Can you help me?";
                            break;
                        }
                    case "khuzait":
                        {
                            text = "I need the power to escape death, to rule over this world as something more. Can you teach me the ways of your power?";
                            break;
                        }
                    default:
                        text = "You shouldn't see this.";
                        break;
                }
                MBTextManager.SetTextVariable("TEST_QUESTION", text);

            }
            return flag;
        }

        private bool filltextfortestprompt()
        {
            string text = "";
            var culture = Hero.OneToOneConversationHero.Culture.StringId;
            switch (culture)
            {
                case "empire":
                    {
                        text = "Hmm. To understand the Winds of magic you must have the aethyric senses. Let me perform an experiment on you to determine your potential... (30% Chance)";
                        break;
                    }
                case "khuzait":
                    {
                        text = "I can sense the you might have some grasp on the Winds of magic. Let me subject you to an examination to see your potential...  (30% Chance)";
                        break;
                    }
                default:
                    text = "You shouldn't see this.";
                    break;
            }
            MBTextManager.SetTextVariable("TEST_PROMPT", text);
            return true;
        }

        private void determinetestoutcome()
        {
            var rng = MBRandom.RandomFloatRanged(0, 1);
            if (rng <= _testSuccessChance) _testResult = "success";
            else _testResult = "failure";
        }

        private bool testresultcondition()
        {
            var result = "Error.";
            if (_testResult == "success")
            {
                result = "Hmm...interesting. It would seem you do have an aptitude, perhaps even potential.";
                Hero.MainHero.AddAttribute("AbilityUser");
                Hero.MainHero.AddAttribute("SpellCaster");
                Hero.MainHero.AddKnownLore("MinorMagic");
                Hero.MainHero.SetSpellCastingLevel(SpellCastingLevel.Minor);
                var quest = TORQuestHelper.GetNewSpecializeLoreQuest(true);
                if (quest != null) quest.StartQuest();
            }
            else if (_testResult == "failure")
            {
                result = "Pah, it is beyond you. Begone before you waste more of my time.";
            }
            MBTextManager.SetTextVariable("TEST_RESULT", result);
            return true;
        }

        private void openbookconsequence()
        {
            var state = Game.Current.GameStateManager.CreateState<SpellBookState>();
            state.IsTrainerMode = true;
            state.TrainerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
            Game.Current.GameStateManager.PushState(state);
        }

        private bool specializelorecondition()
        {
            var quest = TORQuestHelper.GetCurrentActiveIfExists<SpecializeLoreQuest>();
            var partnerCulture = CharacterObject.OneToOneConversationCharacter.Culture.StringId;
            if (Hero.MainHero.IsVampire() && partnerCulture == "empire") return false;
            var info = Hero.MainHero.GetExtendedInfo();
            var possibleLores = new List<LoreObject>();
            foreach (var item in LoreObject.GetAll())
            {
                if (item.ID != "MinorMagic" &&
                    !item.DisabledForTrainersWithCultures.Contains(partnerCulture) &&
                    !info.HasKnownLore(item.ID) &&
                    !(item.IsRestrictedToVampires && !Hero.MainHero.IsVampire())) possibleLores.Add(item);
            }
            bool flag = false;
            if (quest != null && possibleLores.Count > 0)
            {
                flag = info.KnownLores.Count == 1 && info.KnownLores[0].ID == "MinorMagic" && quest.Task1Complete;
                if (!flag && Hero.MainHero.IsVampire() && quest.Task1Complete) flag = true;
            }
            else if (Hero.MainHero.IsVampire() && possibleLores.Count > 0 && info.SpellCastingLevel == SpellCastingLevel.Master) flag = true;
            if (flag)
            {
                string text = "";
                var culture = Hero.OneToOneConversationHero.Culture.StringId;
                switch (culture)
                {
                    case "empire":
                        {
                            text = "I am ready to join the colleges and become a true wizard of the Empire.";
                            break;
                        }
                    case "khuzait":
                        {
                            text = "My Lord, I beseech you to teach me your dark magic. I have learned all that I can on my own and would be a most loyal apprentice to you.";
                            if (Hero.MainHero.IsVampire())
                            {
                                text = "I can feel my dark power continuing to grow, teach me more lest I find myself a new 'tutor'. Hurry on to find your Grimoire, lest I grow thirsty in your absence...";
                            }
                            break;
                        }
                    default:
                        text = "You shouldn't see this.";
                        break;
                }
                MBTextManager.SetTextVariable("SPECALIZE_QUESTION", text);
            }
            return flag;
        }

        private bool fillchooseloretext()
        {
            string text = "";
            var culture = Hero.OneToOneConversationHero.Culture.StringId;
            switch (culture)
            {
                case "empire":
                    {
                        text = "You have proven yourself and have a strong aptitude for the wind of magic, which college will you be joining? You can only dedicate yourself to one, choose wisely.";
                        break;
                    }
                case "khuzait":
                    {
                        text = "You have potential Dark One, serve me and I will teach you all that I know in time. Come let me consult my grimoire.";
                        if (Hero.MainHero.IsVampire()) text = "Yes sire! Right away my good and merciful lord.";
                        break;
                    }
                default:
                    text = "You shouldn't see this.";
                    break;
            }
            MBTextManager.SetTextVariable("SPECIALIZE_PROMPT", text);
            return true;
        }

        private void chooseloreconsequence()
        {
            List<InquiryElement> list = new List<InquiryElement>();
            var lores = LoreObject.GetAll();
            foreach (var item in lores)
            {
                if (item.ID != "MinorMagic" && !item.DisabledForTrainersWithCultures.Contains(CharacterObject.OneToOneConversationCharacter.Culture.StringId) && !Hero.MainHero.GetExtendedInfo().HasKnownLore(item.ID)) list.Add(new InquiryElement(item, item.Name, null));
            }
            var inquirydata = new MultiSelectionInquiryData("Choose Lore", "Choose a lore to specialize in.", list, true, 1, "Confirm", "Cancel", OnChooseLore, OnCancelLore);
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
                MBInformationManager.AddQuickInformation(new TextObject("Successfully learned lore: " + choice.Name));
            }
            InformationManager.HideInquiry();
        }

        private void OnCancelLore(List<InquiryElement> obj)
        {
            InformationManager.HideInquiry();
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_trainerToSettlementMap", ref _settlementToTrainerMap);
        }
    }
}
