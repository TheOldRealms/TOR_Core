using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ink;
using Ink.Runtime;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

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

        public void Reset()
        {
            _story.ResetState();
            foreach(var skill in Skills.All)
            {
                SetVariable(skill.StringId.ToLowerInvariant(), (float)Hero.MainHero.GetSkillValue(skill));
            }
            foreach (var attribute in Attributes.All)
            {
                SetVariable(attribute.StringId.ToLowerInvariant(), (float)Hero.MainHero.GetAttributeValue(attribute));
            }
            if(!_story.TryGetExternalFunction("GiveWinds", out _))
            {
                _story.BindExternalFunction("GiveWinds", (int number) => Hero.MainHero.AddWindsOfMagic(number));
            }
            if (!_story.TryGetExternalFunction("GiveGold", out _))
            {
                _story.BindExternalFunction("GiveGold", (int number) => Hero.MainHero.ChangeHeroGold(number));
            }
        }
        public void ChooseChoice(int index) => _story.ChooseChoiceIndex(index);
        public string GetLine() => _story.currentText;
        public bool HasChoices() => _story.currentChoices.Count > 0;
        public List<Choice> GetChoices() => _story.currentChoices.ToList();
        public bool IsOver() => !_story.canContinue && !HasChoices();
        public bool HasTag(string tag) => _story.currentTags.Contains(tag);
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
    }
}
