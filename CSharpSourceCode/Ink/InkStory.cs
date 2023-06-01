using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ink;
using Ink.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Ink
{
    public class InkStory
    {
        private Story _story;

        public InkStory(string inkFilePath)
        {
            if (File.Exists(inkFilePath))
            {
                using (var reader = File.OpenText(inkFilePath))
                {
                    var ink = reader.ReadToEnd();
                    var compiler = new Compiler(ink);
                    _story = compiler.Compile();
                    //_story.allowExternalFunctionFallbacks = true;
                }
            }
        }

        public void ChooseChoice(int index) => _story.ChooseChoiceIndex(index);
        public string GetLine()
        {
            string line = _story.currentText;
            if (string.IsNullOrWhiteSpace(line) && _story.canContinue)
            {
                _story.Continue();
                line += GetLine();
            }
            return line;
        }
        public bool HasChoices() => _story.currentChoices.Count > 0;
        public List<Choice> GetChoices() => _story.currentChoices.ToList();
        public bool IsOver() => !_story.canContinue && !HasChoices();
        public bool HasTag(string tag) => _story.currentTags.Contains(tag);

        public bool Continue(out string line)
        {
            if (_story.canContinue)
            {
                line = _story.Continue();
                return true;
            }
            else
            {
                line = string.Empty;
                return false;
            }
        }

        public string GetIllustration()
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
                return _story.variablesState[varName].ToString();
            }
            else return varName + " not found";
        }

        private void SetVariable(string varName, object varValue)
        {
            if (_story.variablesState.GlobalVariableExistsWithName(varName))
            {
                _story.variablesState[varName] = varValue;
            }
        }

        public void Reset()
        {
            _story.ResetState();
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
            if (!_story.TryGetExternalFunction("ChangeTraitValue", out _))
            {
                _story.BindExternalFunction<string, int>("ChangeTraitValue", ChangeTraitValue, false);
            }
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

        private void ChangeTraitValue(string traitname, int amount)
        {
            var trait = MBObjectManager.Instance.GetObject<TraitObject>(x=>x.StringId == traitname);
            if (trait == null)
            {
                TORCommon.Say(string.Format("ERROR, trait with id: {0} does not exist!", trait));
                return;
            }
            else
            {
                var value = Hero.MainHero.GetTraitLevel(trait);
                Hero.MainHero.SetTraitLevel(trait, value + amount);
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
            if(troop == null)
            {
                TORCommon.Say(string.Format("ERROR, troop with ID: {0} does not exist!", troopId));
                return;
            }
            else if(limit >= limit + count)
            {
                MobileParty.MainParty.MemberRoster.AddToCounts(troop, count);
            }
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
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 50f, x => x.IsTown);
            }
            else if (settlementType.ToLowerInvariant() == "village")
            {
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 50f, x => x.IsVillage);
            }
            else if (settlementType.ToLowerInvariant() == "castle")
            {
                settlement = TORCommon.FindNearestSettlement(MobileParty.MainParty, 50f, x => x.IsCastle);
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
    }
}
