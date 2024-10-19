using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Missions;
using TOR_Core.Quests;

namespace TOR_Core.Utilities
{
    public class TORConsoleCommands
    {
        private static List<string> torSpellNames = AbilityFactory.GetAllSpellNamesAsList();
        
        //TODO currently disabled due to missing Engineer Quest
        [CommandLineFunctionality.CommandLineArgumentFunction("whereisgoswin", "tor")]
        public static string TeleportPlayerToQuestParty(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;
            var engineerquest = EngineerQuest.GetCurrentActiveIfExists();
            if (engineerquest != null)
            {
                Campaign.Current.MainParty.Position2D =MobilePartyHelper.FindReachablePointAroundPosition(engineerquest.TargetParty.Position2D,0f);

                return " *puff*... there he is!";
            }
            return "Engineer Quest is not active \n";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("whereAreAICompanions", "tor")]
        public static string ShowCompanionPosition(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            string result = "";

            var aiCompanions = Campaign.Current.AliveHeroes.Where(x => x.IsAICompanion());
            

            foreach (var companion in aiCompanions)
            {
                var partResult = "";
                if (companion.CurrentSettlement != null)
                {
                    partResult = partResult.Add(companion.Name.ToString() + " " + " is currently in " + companion.CurrentSettlement);
                    result+=partResult;
                    continue;
                }

                if (companion.PartyBelongedTo != null)
                {
                    partResult=partResult.Add(companion.Name.ToString() + " " + " is part of  " + companion.PartyBelongedTo.LeaderHero.Name + " Party"+ companion.PartyBelongedTo.GetPosition2D);
                    result+=partResult;
                    continue;
                }

                if (companion.CurrentSettlement == null && companion.PartyBelongedTo == null)
                {
                    partResult= partResult.Add(companion.Name.ToString() + " " + " is nowhere to be found.");
                    result+=partResult;
                    continue;
                }
            }
            return result;
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("declare_peace", "tor")]
        public static string DeclarePeace(List<string> strings)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;
            string str1 = "campaign.declare_peace [Faction1] | [Faction2]";
            if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings))
                return str1;
            List<string> separatedNames = CampaignCheats.GetSeparatedNames(strings, "|");
            if (separatedNames.Count != 2)
                return str1;
            string kingdom_str1 = separatedNames[0].ToLower().Replace(" ", "");
            string kingdom_str2 = separatedNames[1].ToLower().Replace(" ", "");
            Kingdom faction1 = null;
            Kingdom faction2 =  null;
            foreach (var kingdom in Campaign.Current.Kingdoms)
            {
                if (kingdom_str1 == kingdom.StringId)
                {
                    faction1 = kingdom;
                }
                if (kingdom_str2 == kingdom.StringId)
                {
                    faction2= kingdom;
                }
            }
            
            if (faction1 != null && faction2 != null)
            {
                MakePeaceAction.Apply(faction1, faction2);
                return "Peace declared between " + (object) faction1.Name + " and " + (object) faction2.Name;
            }
            return faction1 == null ? "Faction is not found: " + kingdom_str1 + "\n" + str1 : "Faction is not found: " + kingdom_str2;
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("list_spells", "tor")]
        public static string ListSpells(List<string> argumentNames) =>
            AggregateOutput("Available spells are:", torSpellNames);

        [CommandLineFunctionality.CommandLineArgumentFunction("list_player_spells", "tor")]
        public static string ListPlayerSpells(List<string> argumentNames) =>
            !CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType)
                ? CampaignCheats.ErrorType
                : AggregateOutput("Player got these spells:", Hero.MainHero.GetExtendedInfo().AllAbilities);

        [CommandLineFunctionality.CommandLineArgumentFunction("add_spells_to_player", "tor")]
        public static string AddSpells(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            var matchedArguments = new List<string>();
            var newSpells = new List<string>();
            var knownSpells = new List<string>();

            foreach (var argument in arguments)
            foreach (var torSpell in torSpellNames)
                if (string.Equals(torSpell, argument, StringComparison.CurrentCultureIgnoreCase))
                {
                    matchedArguments.Add(torSpell);

                    if (Hero.MainHero.HasAbility(torSpell))
                        knownSpells.Add(torSpell);
                    else
                    {
                        Hero.MainHero.AddAbility(torSpell);
                        newSpells.Add(torSpell);
                    }
                }

            if (newSpells.Count > 0)
                MakePlayerSpellCaster(null);

            return FormatAddedSpellsOutput(matchedArguments, knownSpells, newSpells);
        }

        private static string FormatAddedSpellsOutput(List<string> matchedArguments, List<string> knownSpells,
            List<string> newSpells) =>
            AggregateOutput("Matched spells:", matchedArguments) +
            AggregateOutput("Already known spells in request:", knownSpells) +
            AggregateOutput("Added spells :", newSpells
            );

        [CommandLineFunctionality.CommandLineArgumentFunction("make_player_necromancer", "tor")]
        public static string MakePlayerNecromancer(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (!Hero.MainHero.IsNecromancer())
                Hero.MainHero.AddAttribute("Necromancer");

            return MakePlayerSpellCaster(null) + "Player is necromancer now.\n ";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("make_player_spell_caster", "tor")]
        public static string MakePlayerSpellCaster(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (!Hero.MainHero.IsSpellCaster())
                Hero.MainHero.AddAttribute("SpellCaster");

            if (!Hero.MainHero.IsAbilityUser())
                Hero.MainHero.AddAttribute("AbilityUser");

            return "Player is spell caster now. \n";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("damage_agent", "tor")]
        public static string DamageAgent(List<string> arguments)
        {
            if (Campaign.Current != null)
            {
                if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                    return CampaignCheats.ErrorType;
            }
        
            if (Mission.Current == null)
                return "not in mission";
            
            Agent target=null;
            int damage;
            if (int.TryParse(arguments[0], out damage))
            {
                target = Mission.Current.MainAgent;
            }
            else
            {
                if (arguments.Count==2&&int.TryParse(arguments[1], out damage))
                {
                    target= Mission.Current.Agents.FirstOrDefault(x => x.Name == arguments[0]);
                }
            }

            if (target == null) return "Couldn't find agent";
            target.ApplyDamage(damage,target.Position);
            return "Damaged "+target.Name+" with "+ damage+ "\n";

        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("add_blessing", "tor")]
        public static string AddBlessingToPlayer(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            var religionId = arguments[0];
            var religion = ReligionObject.All.FirstOrDefault(x => x.StringId == religionId);
            if(religion != null)
            {
                if (Hero.MainHero.PartyBelongedTo == null) return "not in a party";
                var blessingText = religion.BlessingEffectName;
                if (blessingText == null) return "blessing description not found";
                Hero.MainHero.PartyBelongedTo.AddBlessingToParty(religion.StringId);
                
                return string.Format("Player now has the {0}. \n", blessingText); 
            }
            else return "No religion with the given argument found. \n";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("add_career", "tor")]
        public static string AddCareerToPlayer(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            var careerId = arguments[0];
            var career = TORCareers.All.FirstOrDefault(x => x.StringId == careerId);
            if(career != null)
            {
                Hero.MainHero.AddCareer(career);
                return string.Format("Player now has {0} career. \n", career.StringId); 
            }
            else return "No career with the given argument found. \n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("open_ink_story", "tor")]
        public static string OpenInkStory(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if(arguments == null || arguments.Count == 0)
            {
                return "Argument cannot be null. Pass in the name of the story to open. \n";
            }

            var storyName = arguments[0];
            var story = InkStoryManager.GetStory(storyName);
            if (story == null) return "No story found with the specified name. \n";
            InkStoryManager.OpenStory(storyName);

            return "Story opened. \n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("reload_ink_stories", "tor")]
        public static string ReloadInkStories(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            InkStoryManager.ReloadStories();

            return "Ink Stories reloaded. \n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("open_test_mission", "tor")]
        public static string OpenTestMission(List<string> arguments)
        {
            var template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_cultists");
            TorMissionManager.OpenQuestMission("tor_test_scene_for_stuff", template, 9, null, false);
            return "Scene opened.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("add_player_attribute", "tor")]
        public static string AddPlayerAttribute(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (arguments == null || arguments.Count == 0)
            {
                return "Argument cannot be null. Pass in the name of the story to open. \n";
            }

            var attribute = arguments[0];
            Hero.MainHero.AddAttribute(attribute);

            return string.Format("Successfully added attribute: {0} to player.", attribute);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("reload_animation_triggers", "tor")]
        public static string ReloadAnimationTriggers(List<string> arguments)
        {
            AnimationTriggerManager.ReloadAnimationTriggers();
            
            return string.Format("Successfully reloaded animation triggers");
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("add_custom_resource", "tor")]
        public static string AddCustomResource(List<string> arguments)
        {
            if(arguments.Count!= 2) return string.Format("Incorrect arguments. Usage is \"tor.add_custom_resource resourcename amount\" ");
            
            string resourceId = arguments[0];
            int amount = 0;
            if(int.TryParse(arguments[1], out amount))
            {
                var resource = CustomResourceManager.GetResourceObject(resourceId);
                if(resource != null)
                {
                    Hero.MainHero.AddCustomResource(resourceId, amount);
                    return string.Format("Successfully added {0} {1} to main hero.", amount.ToString(), resource.Name);
                }
                return string.Format("Custom resource with id {0} not found.", resourceId);
            }

            return string.Format("Incorrect arguments. Usage is \"tor.add_custom_resource resourcename amount\" ");
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("trigger_fatal_crash", "tor")]
        public static string TriggerFatalCrash(List<string> arguments)
        {
            TORTests.Instance.TriggerCorruptedMemoryStateException();
            return "Should crash before this gets returned.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("print_map_patch_data", "tor")]
        public static string PrintMapPatchData(List<string> arguments)
        {
            if (Campaign.Current == null) return "Function only available when playing in campaign mode.";

            if (ScreenManager.TopScreen is MapScreen && Campaign.Current.MapSceneWrapper != null)
            {
                var patch = Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position2D);
                return $"Battle scene index at position ({MobileParty.MainParty.Position2D.ToString()}) is: {patch.sceneIndex}";
            }
            else return "Function only available while on the world map.";
        }

        private static string AggregateOutput(string topicHeader, List<string> matchedSpells) =>
            matchedSpells.Aggregate(
                $"\n{topicHeader}\n",
                (current, spell) =>
                    $"{current}{spell}\n"
            );
    }
}