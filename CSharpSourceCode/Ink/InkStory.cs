using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Helpers;
using Ink;
using Ink.Runtime;
using psai.net;
using SandBox;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Inventory;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TOR_Core.Audio;
using TOR_Core.CampaignMechanics.CustomEvents;
using TOR_Core.CampaignMechanics.TORCustomSettlement;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Missions;
using TOR_Core.Utilities;
using Object = Ink.Runtime.Object;
using Path = Ink.Runtime.Path;

namespace TOR_Core.Ink
{
    public class InkStory
    {
        private Story _story;
        private List<string> _currentTags;
        public string Title;
        public readonly CustomEventFrequency Frequency;
        public readonly bool IsDevelopmentVersion;
        public readonly string StringId;
        public readonly int Cooldown;
        private CachedSoundInstance _currentAudio;

        public InkStory(string id, string file)
        {
            if (File.Exists(file))
            {
                StringId = id;
                using (var reader = File.OpenText(file))
                {
                    var ink = reader.ReadToEnd();
                    var compiler = new Compiler(ink);
                    _story = compiler.Compile();
                    //_story.allowExternalFunctionFallbacks = true;
                }
                
                if (Title == null) Title = "Invalid title, bad tag settings";
                if (!Enum.TryParse<CustomEventFrequency>(GetValueOfGlobalTag("frequency"), out Frequency))
                {
                    Frequency = CustomEventFrequency.Invalid;
                }
                if (!bool.TryParse(GetValueOfGlobalTag("development"), out IsDevelopmentVersion))
                {
                    IsDevelopmentVersion = true;
                }
                if (!int.TryParse(GetValueOfGlobalTag("cooldown"), out Cooldown))
                {
                    Cooldown = 300;
                }
            }
        }

        public void CleanUp()
        {
            if(_currentAudio != null)
            {
                _currentAudio.Remove();
            }
            MBMusicManager.Current.UnpauseMusicManagerSystem();
            MBMusicManager.Current.ActivateCampaignMode();
        }

        public void SetTitle()
        {
            var overrideText = new TextObject();
            if (GameTexts.TryGetText("inky_"+StringId, out overrideText, "Title"))
            {
                if (!overrideText.ToString().IsEmpty())
                {
                    Title = overrideText.ToString();
                    return;
                }
            }
            Title = GetValueOfGlobalTag ("title");
        }

      
        private string GetValueOfGlobalTag(string tag)
        {
            if (_story == null || _story.globalTags == null || _story.globalTags.Count < 1) return null;
            foreach(var item in _story.globalTags)
            {
                if (item.ToLowerInvariant().Contains(tag.ToLowerInvariant()))
                {
                    return item.Split(':').Last();
                }
            }
            return null;
        }

        public void ChooseChoice(int index) => _story.ChooseChoiceIndex(index);
        public string GetLine()
        {
            string line = RetrieveText();
            if (string.IsNullOrWhiteSpace(line) && _story.canContinue)
            {
                _story.Continue();
                line += RetrieveText();
            }
            return line;
        }
        public bool HasChoices() => _story.currentChoices.Count > 0;
        public List<Choice> GetChoices() => _story.currentChoices.ToList();
        public bool IsOver() => !_story.canContinue && !HasChoices();
        public bool HasTag(string tag) => _story.currentTags.Contains(tag);

        public string RetrieveText()
        {
            var line = _story.currentText;
            _currentTags = _story.currentTags;
            foreach (var tag in _currentTags)
            {
                if (tag.StartsWith ("STR_")){
                    TextObject overrideText = new TextObject();
                    if (GameTexts.TryGetText("inky_"+StringId, out overrideText, tag))
                    {
                        if (!overrideText.ToString().IsEmpty())
                        {
                            line = overrideText.ToString();
                            return line;
                        }
                    }
                }
            }
            
            return line;
        }

        public bool Continue(out string line)
        {
            if (_story.canContinue)
            {
                _story.Continue();
                line = GetLine();
                return true;
            }
            else
            {
                line = string.Empty;
                return false;
            }
        }
        
        public string GetInitialIllustration()
        {
            var illustration = GetValueOfGlobalTag ("illustration");
            if (illustration != null)
            {
                return "InkStories\\Illustrations\\" + illustration.Trim();
            }

            return "";
        }

        public string GetCurrentIllustration()
        {
            var tags = _story.currentTags;
            foreach(var tag in tags)
            {
                if (tag.Contains("illustration:"))
                {
                    var splits = tag.Split(new char[] { ':' });
                    var name = splits[splits.Length - 1].Trim();
                    return "InkStories\\Illustrations\\" + name;
                }
            }
            return null;
        }

        public string GetVariable(string varName)
        {
            if (_story.variablesState.GlobalVariableExistsWithName(varName))
            {
                TextObject overrideText = new TextObject();
                var variableState = _story.variablesState[varName].ToString();
                var combinedVariableID = varName+"_"+variableState;
                if (GameTexts.TryGetText ("inky_" + StringId, out overrideText, combinedVariableID))
                {
                    if (!overrideText.ToString().IsEmpty())
                    {
                        return overrideText.ToString();
                    }
                }
               
                return variableState;
            }
            else return varName + " not found";
        }

        public void SetVariable(string varName, object varValue)
        {
            if (_story.variablesState.GlobalVariableExistsWithName(varName))
            {
                _story.variablesState[varName] = varValue;
            }
        }

        public void Reset()
        {
            _story.ResetState();
            
            if(!_story.TryGetExternalFunction ("SetTextVariable", out _))
            {
                _story.BindExternalFunction<string,string> ("SetTextVariable", SetTextVariable, false);
            }
            if(!_story.TryGetExternalFunction ("SetPlayerSkillChance", out _))
            {
                _story.BindExternalFunction<string,string> ("SetPlayerSkillChance", SetPlayerSkillChance, true);
            }
            if(!_story.TryGetExternalFunction ("SetPartySkillChance", out _))
            {
                _story.BindExternalFunction<string,string> ("SetPartySkillChance", SetPartySkillChance, true);
            }
            if(!_story.TryGetExternalFunction ("SetPlayerAttributeChance", out _))
            {
                _story.BindExternalFunction<string,string> ("SetPlayerAttributeChance", SetPlayerAttributeChance, true);
            }
            if(!_story.TryGetExternalFunction ("SetPartyAttributeChance", out _))
            {
                _story.BindExternalFunction<string,string> ("SetPartyAttributeChance", SetPartyAttributeChance, true);
            }
            if(!_story.TryGetExternalFunction("GiveWinds", out _))
            {
                _story.BindExternalFunction("GiveWinds", (int number) => Hero.MainHero.AddWindsOfMagic(number), false);
            }
            if (!_story.TryGetExternalFunction("GiveGold", out _))
            {
                _story.BindExternalFunction("GiveGold", (int number) => Hero.MainHero.ChangeHeroGold(number), false);
            }
            if (!_story.TryGetExternalFunction("GetPlayerSkillValue", out _))
            {
                _story.BindExternalFunction<string>("GetPlayerSkillValue", GetPlayerSkillValue, true);
            }
            if (!_story.TryGetExternalFunction("GetPartySkillValue", out _))
            {
                _story.BindExternalFunction<string>("GetPartySkillValue", GetPartySkillValue, true);
            }
            if (!_story.TryGetExternalFunction("GetPlayerAttributeValue", out _))
            {
                _story.BindExternalFunction<string>("GetPlayerAttributeValue", GetPlayerAttributeValue, true);
            }
            if (!_story.TryGetExternalFunction("GetPartyAttributeValue", out _))
            {
                _story.BindExternalFunction<string>("GetPartyAttributeValue", GetPartyAttributeValue, true);
            }
            if (!_story.TryGetExternalFunction("GiveSkillExperience", out _))
            {
                _story.BindExternalFunction<string, int>("GiveSkillExperience", GiveSkillExperience, false);
            }
            if (!_story.TryGetExternalFunction("GetPlayerPersonalityTraitValue", out _))
            {
                _story.BindExternalFunction<string>("GetPlayerPersonalityTraitValue", GetPlayerPersonalityTraitValue, true);
            }
            if (!_story.TryGetExternalFunction("PartyHasVampire", out _))
            {
                _story.BindExternalFunction<bool>("PartyHasVampire", PartyHasVampire, true);
            }
            if (!_story.TryGetExternalFunction("PartyHasNecromancer", out _))
            {
                _story.BindExternalFunction<bool>("PartyHasNecromancer", PartyHasNecromancer, true);
            }
            if (!_story.TryGetExternalFunction("PartyHasSpellcaster", out _))
            {
                _story.BindExternalFunction<bool>("PartyHasSpellcaster", PartyHasSpellcaster, true);
            }
            if (!_story.TryGetExternalFunction("DoesPartyKnowSchoolOfMagic", out _))
            {
                _story.BindExternalFunction<bool, string>("DoesPartyKnowSchoolOfMagic", DoesPartyKnowSchoolOfMagic, true);
            }
            if (!_story.TryGetExternalFunction("GetNearestSettlement", out _))
            {
                _story.BindExternalFunction<string>("GetNearestSettlement", GetNearestSettlement, true);
            }
            if (!_story.TryGetExternalFunction("GetRandomNotableFromNearestSettlement", out _))
            {
                _story.BindExternalFunction<string>("GetRandomNotableFromNearestSettlement", GetRandomNotableFromNearestSettlement, true);
            }
            if (!_story.TryGetExternalFunction("GetRandomNotableFromSpecificSettlement", out _))
            {
                _story.BindExternalFunction<string>("GetRandomNotableFromSpecificSettlement", GetRandomNotableFromSpecificSettlement, true);
            }
            if (!_story.TryGetExternalFunction("ChangePartyTroopCount", out _))
            {
                _story.BindExternalFunction<string, int>("ChangePartyTroopCount", ChangePartyTroopCount, false);
            }
            if (!_story.TryGetExternalFunction("GiveItem", out _))
            {
                _story.BindExternalFunction<string, int>("GiveItem", GiveItem, false);
            }
            if (!_story.TryGetExternalFunction("ChangeRelations", out _))
            {
                _story.BindExternalFunction<string, int>("ChangeRelations", ChangeRelations, false);
            }
            if (!_story.TryGetExternalFunction("AddTraitInfluence", out _))
            {
                _story.BindExternalFunction<string, int>("AddTraitInfluence", AddTraitInfluence, false);
            }
            if (!_story.TryGetExternalFunction("HealPartyToFull", out _))
            {
                _story.BindExternalFunction("HealPartyToFull", HealPartyToFull, false);
            }
            if (!_story.TryGetExternalFunction("GetTotalPartyMemberCount", out _))
            {
                _story.BindExternalFunction("GetTotalPartyMemberCount", GetTotalPartyMemberCount, true);
            }
            if (!_story.TryGetExternalFunction("GetTotalPartyWoundedCount", out _))
            {
                _story.BindExternalFunction("GetTotalPartyWoundedCount", GetTotalPartyWoundedCount, true);
            }
            if (!_story.TryGetExternalFunction("MakePartyDisorganized", out _))
            {
                _story.BindExternalFunction("MakePartyDisorganized", MakePartyDisorganized, false);
            }
            if (!_story.TryGetExternalFunction("OpenDuelMission", out _))
            {
                _story.BindExternalFunction("OpenDuelMission", OpenDuelMission, false);
            }
            if (!_story.TryGetExternalFunction("OpenCultistLairMission", out _))
            {
                _story.BindExternalFunction<string>("OpenCultistLairMission", OpenCultistLairMission, false);
            }
            if (!_story.TryGetExternalFunction("GetPlayerHasCustomTag", out _))
            {
                _story.BindExternalFunction<string>("GetPlayerHasCustomTag", GetPlayerHasCustomTag, true);
            }
            if (!_story.TryGetExternalFunction("SetPlayerCustomTag", out _))
            {
                _story.BindExternalFunction<string>("SetPlayerCustomTag", SetPlayerCustomTag, false);
            }
            if (!_story.TryGetExternalFunction("OpenInventoryAsTrade", out _))
            {
                _story.BindExternalFunction("OpenInventoryAsTrade", OpenInventoryAsTrade, false);
            }
            if (!_story.TryGetExternalFunction("IsNight", out _))
            {
                _story.BindExternalFunction("IsNight", IsNight, true);
            }
            if (!_story.TryGetExternalFunction("HasEnoughGold", out _))
            {
                _story.BindExternalFunction<int>("HasEnoughGold", HasEnoughGold, true);
            }
            if (!_story.TryGetExternalFunction("PlayMusic", out _))
            {
                _story.BindExternalFunction<string>("PlayMusic", PlayMusic, false);
            }
            if (!_story.TryGetExternalFunction("GiveMiracleItem", out _))
            {
                _story.BindExternalFunction("GiveMiracleItem", GiveMiracleItem, false);
            }
            if (!_story.TryGetExternalFunction("ResetRaiderSites", out _))
            {
                _story.BindExternalFunction("ResetRaiderSites", ResetRaiderSites, false);
            }
        }

       

        private void SetTextVariable(string variableName, string variant)
        {
            var textID = "inky_" + StringId;
            var variable = GetVariable (variableName);
            var variableVariant = variableName +"_"+ variant;

            if (!GameTexts.TryGetText (textID, out var resultText, variableVariant) && ( resultText==null|| resultText.Value==""))
            {
                resultText = new TextObject ("{=!}"+variable);
            }
            
            GameTexts.SetVariable ("inky_"+variableName,resultText);
            
        }

        private void SetPlayerSkillChance(string skillname, string skillChance)
        {
        
            var idChance = "inky_Player_skill_CheckChance";
            GameTexts.SetVariable (idChance,skillChance);

            var skillText = TORTextHelper.GetTextObjectOfSkillId (skillname);
            GameTexts.SetVariable ("inky_skill_check_skill_name",skillText);
            var skillCheckResultText = GameTexts.FindText ("inky_player_skill_check_result_template") ;
            
            GameTexts.SetVariable ("inky_player_skill_check_result_"+skillname,skillChance);
        }
        private void SetPartySkillChance(string skillname, string skillChance)
        {
        
            var idChance = "inky_Party_skill_CheckChance";
            
            GameTexts.SetVariable (idChance,skillChance);
            
            var skillText = TORTextHelper.GetTextObjectOfSkillId (skillname);
            GameTexts.SetVariable ("inky_skill_check_skill_name",skillText);
            var skillCheckResultText = GameTexts.FindText ("inky_party_skill_check_result_template") ;
            GameTexts.SetVariable("inky_party_skill_check_result_"+skillname,skillCheckResultText.ToString());
        }
        
        private void SetPlayerAttributeChance(string attribute, string attributeChance)
        {
            var idChance = "inky_Player_attribute_CheckChance";
            GameTexts.SetVariable (idChance,attributeChance);
            var attributeText = TORTextHelper.GetTextObjectOfAttribute (attribute);
            GameTexts.SetVariable ("inky_attribute_check_attribute_name",attributeText);
            var attributeCheckResultText = GameTexts.FindText ("inky_player_attribute_check_result_template") ;
            GameTexts.SetVariable ("inky_player_attribute_skill_check_result_" + attribute, attributeCheckResultText);
        }
        
        private void SetPartyAttributeChance(string attribute, string attributeChance)
        {
            var idChance = "inky_Party_attribute_CheckChance";
            GameTexts.SetVariable (idChance,attributeChance);
            var attributeText = TORTextHelper.GetTextObjectOfAttribute (attribute);
            GameTexts.SetVariable ("inky_attribute_check_attribute_name",attributeText);
            var attributeCheckResultText = GameTexts.FindText ("inky_party_attribute_check_result_template") ;
            GameTexts.SetVariable ("inky_party_attribute_check_result_" + attribute, attributeCheckResultText);
        }

        private void ResetRaiderSites()
        {
            foreach (var site in Settlement.All.Where(x => x.SettlementComponent is BaseRaiderSpawnerComponent))
            {
                var component = site.SettlementComponent as BaseRaiderSpawnerComponent;
                component.IsActive = true;
            }
        }

        private void GiveMiracleItem()
        {
            bool gaveItem = false;
            var religion = Hero.MainHero.GetDominantReligion();
            var inventory = MobileParty.MainParty.ItemRoster;
            foreach (var item in religion.ReligiousArtifacts)
            {
                bool found = false;
                for (int i = 0; i < inventory.Count; i++)
                {
                    var itemInventory = inventory.GetItemAtIndex(i);
                    if (item.StringId == itemInventory.StringId)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;
                else
                {
                    inventory.Add(new ItemRosterElement(item, 1));
                    gaveItem = true;
                    break;
                }
            }
            if(!gaveItem)
            {
                inventory.Add(new ItemRosterElement(religion.ReligiousArtifacts.TakeRandom(1).FirstOrDefault(), 1));
            }
        }

        private void PlayMusic(string songName)
        {
            _currentAudio = TORAudioManager.CreateSoundInstance(songName, false, 1);
            if(_currentAudio != null )
            {
                MBMusicManager.Current.DeactivateCurrentMode();
                MBMusicManager.Current.PauseMusicManagerSystem();
                _currentAudio.Play();
            }
        }

        private object HasEnoughGold(int amount) => Hero.MainHero.Gold >= amount;

        private object IsNight() => CampaignTime.Now.IsNightTime;

        private void OpenCultistLairMission(string missionName)
        {
            var template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_cultists");
            TorMissionManager.OpenQuestMission(missionName, template, 5, (playerVictory) => SetVariable("DealtWithCultists", playerVictory));
        }

        private void OpenInventoryAsTrade()
        {
            AccessTools.Field(typeof(InventoryManager), "_currentMode").SetValue(InventoryManager.Instance, InventoryMode.Trade);
            var logic = new InventoryLogic(null);
            ItemRoster roster = new ItemRoster();
            var items = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x => (x.HasWeaponComponent || x.HasArmorComponent) && x.StringId.StartsWith("tor_") && !x.NotMerchandise);
            var foods = MBObjectManager.Instance.GetObjectTypeList<ItemObject>().Where(x => x.HasFoodComponent);
            var selectedItems = items.TakeRandom(20);
            var selectedFoods = foods.TakeRandom(5);
            foreach (var item in selectedItems)
            {
                roster.Add(new ItemRosterElement(item, 1));
            }
            foreach (var food in selectedFoods)
            {
                roster.Add(new ItemRosterElement(food, MBRandom.RandomInt(1, 10)));
            }
            AccessTools.Field(typeof(InventoryManager), "_inventoryLogic").SetValue(InventoryManager.Instance, logic);
            logic.Initialize(roster, PartyBase.MainParty.ItemRoster, PartyBase.MainParty.MemberRoster, true, true, CharacterObject.PlayerCharacter, InventoryManager.InventoryCategoryType.All, new InkFakeMarketData(), true);

            InventoryState inventoryState = Game.Current.GameStateManager.CreateState<InventoryState>();
            inventoryState.InitializeLogic(logic);
            Game.Current.GameStateManager.PushState(inventoryState, 0);
            inventoryState.Handler.FilterInventoryAtOpening(InventoryManager.InventoryCategoryType.All);
        }

        private void SetPlayerCustomTag(string tag) => Hero.MainHero.AddAttribute(tag);

        private object GetPlayerHasCustomTag(string tag) => Hero.MainHero.HasAttribute(tag);

        private void OpenDuelMission()
        {
            TorMissionManager.OpenDuelMission((playerVictory) => SetVariable("PlayerWin", playerVictory));
        }

        private void MakePartyDisorganized() => MobileParty.MainParty.SetDisorganized(true);
        private object GetTotalPartyMemberCount() => MobileParty.MainParty.MemberRoster.TotalManCount;
        private object GetTotalPartyWoundedCount() => MobileParty.MainParty.MemberRoster.TotalWounded;

        private void HealPartyToFull()
        {
            var roster = MobileParty.MainParty.MemberRoster;
            for(int i = 0; i < roster.Count; i++)
            {
                var character = roster.GetCharacterAtIndex(i);
                if (character.IsHero && character.HeroObject != null)
                {
                    character.HeroObject.HitPoints = character.HeroObject.MaxHitPoints;
                }
                else if (character.IsRegular)
                {
                    int wounded = roster.GetElementWoundedNumber(i);
                    if (wounded > 0)
                    {
                        roster.AddToCountsAtIndex(i, 0, -wounded, 0, true);
                    }
                }
            }
        }

        private object GetPartyAttributeValue(string attributeName)
        {
            CharacterAttribute attribute = MBObjectManager.Instance.GetObject<CharacterAttribute>(attributeName.ToLowerInvariant());
            if (attribute != null) return (float)MobileParty.MainParty.GetHighestAttributeValue(attribute);
            else return 0f;
        }

        private object GetPlayerAttributeValue(string attributeName)
        {
            CharacterAttribute attribute = MBObjectManager.Instance.GetObject<CharacterAttribute>(attributeName.ToLowerInvariant());
            if (attribute != null) return (float)Hero.MainHero.GetAttributeValue(attribute);
            else return 0f;
        }

        private object GetRandomNotableFromSpecificSettlement(string settlementName)
        {
            var settlement = Settlement.FindFirst(x => x.Name.ToString() == settlementName);
            if (settlement == null)
            {
                return "ERROR: settlement with given name does not exist.";
            }
            else
            {
                if (settlement.Notables != null && settlement.Notables.Count > 0) return settlement.Notables.GetRandomElementInefficiently().Name.ToString();
                else return "ERROR: settlement does not have any notables";
            }
        }

        private void AddTraitInfluence(string traitname, int amount)
        {
            var trait = MBObjectManager.Instance.GetObject<TraitObject>(x=>x.StringId == traitname);
            if (trait == null)
            {
                TORCommon.Say(string.Format("ERROR, trait with id: {0} does not exist!", trait));
                return;
            }
            else
            {
                Campaign.Current.PlayerTraitDeveloper.AddTraitXp(trait, amount);
            }
        }

        private void ChangeRelations(string heroName, int amount)
        {
            var hero = Hero.FindFirst(x => x.Name.ToString() == heroName);
            if (hero == null)
            {
                TORCommon.Say(string.Format("ERROR, hero with name: {0} does not exist!", hero));
                return;
            }
            else
            {
                ChangeRelationAction.ApplyPlayerRelation(hero, amount);
            }
        }

        private void GiveItem(string itemId, int amount)
        {
            var item = MBObjectManager.Instance.GetObject<ItemObject>(itemId);
            if (item == null)
            {
                TORCommon.Say(string.Format("ERROR, item with ID: {0} does not exist!", item));
                return;
            }
            else
            {
                MobileParty.MainParty.Party.ItemRoster.AddToCounts(item, amount);
            }
        }

        private void ChangePartyTroopCount(string troopId, int count)
        {
            var troop = MBObjectManager.Instance.GetObject<CharacterObject>(troopId);
            int limit = MobileParty.MainParty.Party.PartySizeLimit;
            int current = MobileParty.MainParty.MemberRoster.Count;
            if(troop == null)
            {
                TORCommon.Say(string.Format("ERROR, troop with ID: {0} does not exist!", troopId));
                return;
            }
            if(limit >= current + count)
            {
                MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
            }
            else TORCommon.Say("ERROR, Party maximum size exceeded.");
        }

        private object GetRandomNotableFromNearestSettlement(string settlementType)
        {
            Settlement settlement = null;
            if(settlementType.ToLowerInvariant() == "town")
            {
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 50f, x => x.IsTown);
            }
            else if (settlementType.ToLowerInvariant() == "village")
            {
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 50f, x => x.IsVillage);
            }
            if (settlement != null && settlement.Notables != null && settlement.Notables.Count > 0) return settlement.Notables.GetRandomElementInefficiently().Name.ToString();
            return "ERROR: invalid settlement type";
        }

        private object GetNearestSettlement(string settlementType)
        {
            Settlement settlement = null;
            if (settlementType.ToLowerInvariant() == "town")
            {
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 150f, x => x.IsTown);
            }
            else if (settlementType.ToLowerInvariant() == "village")
            {
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 150f, x => x.IsVillage);
            }
            else if (settlementType.ToLowerInvariant() == "castle")
            {
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 150f, x => x.IsCastle);
            }
            if (settlement != null) return settlement.Name.ToString();
            return "ERROR: invalid settlement type";
        }

        private object PartyHasVampire(bool playeronly)
        {
            return playeronly ? Hero.MainHero.IsVampire() : MobileParty.MainParty.HasVampire();
        }

        private object PartyHasNecromancer(bool playeronly)
        {
            return playeronly ? Hero.MainHero.IsNecromancer() : MobileParty.MainParty.HasNecromancer();
        }

        private object PartyHasSpellcaster(bool playeronly)
        {
            return playeronly ? Hero.MainHero.IsSpellCaster() : MobileParty.MainParty.HasSpellcaster();
        }

        private object DoesPartyKnowSchoolOfMagic(bool playeronly, string lorename)
        {
            return playeronly ? Hero.MainHero.HasKnownLore(lorename) : MobileParty.MainParty.HasKnownLore(lorename);
        }

        private object GetPlayerPersonalityTraitValue(string traitname)
        {
            var trait = DefaultTraits.Personality.FirstOrDefault(x => x.StringId == traitname);
            if (trait != null) return Hero.MainHero.GetTraitLevel(trait);
            else return 0;
        }

        private object GetPlayerSkillValue(string skillname)
        {
            SkillObject skill = Skills.All.FirstOrDefault(x => x.StringId == skillname);
            if (skill != null) return (float)Hero.MainHero.GetSkillValue(skill);
            else return 0f;
        }

        private object GetPartySkillValue(string skillname)
        {
            SkillObject skill = Skills.All.FirstOrDefault(x => x.StringId == skillname);
            if (skill != null) return (float)MobileParty.MainParty.GetHighestSkillValue(skill);
            else return 0f;
        }

        private void GiveSkillExperience(string skillname, int amount)
        {
            SkillObject skill = Skills.All.FirstOrDefault(x => x.StringId == skillname);
            if(skill != null) Hero.MainHero.AddSkillXp(skill, amount);
        }

        public string getChoiceText(Choice choice)
        {
            var choiceLine = "";
            var pathExitID = GetPathExitID (choice.targetPath);
            var choiceID = choice.targetPath.ToString().Split('.').FirstOrDefault() + "_c" + pathExitID;
            var stringId = "{=inky_" + StringId +"_"+ choiceID+"}";
            stringId=stringId.ToLowerInvariant();
            var overrideText = new TextObject (stringId).ToString();
            
            if (!overrideText.IsEmpty())
            {
                choiceLine = overrideText;
            }
            
            if (choiceLine.IsEmpty())
            {
                choiceLine = choice.text;
            }
            

            return choiceLine;

        }

        private string GetPathExitID(Path path)
        {
            var pathExitID = path.ToString().Split ('.').FirstOrDefault (x => x.StartsWith ("c-"))?.Replace("c-","");
            return pathExitID;
        }
    }
}
