using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.CampaignMechanics.Career;
using TOR_Core.Extensions;
using TOR_Core.Quests;

namespace TOR_Core.Utilities
{
    public class TORConsoleCommands
    {
        private static List<string> torSpellNames = AbilityFactory.GetAllSpellNamesAsList();
        private static List<string> torCareerAbilities = AbilityFactory.GetAllCareerAbilityNamesAsList();
        
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
        
        
        [CommandLineFunctionality.CommandLineArgumentFunction("list_careerAbilities", "tor")]
        public static string ListCareerAbilities(List<string> argumentNames) =>
            AggregateOutput("Available careerSkills:", torCareerAbilities);
        
        
        
        public static string ListSpells(List<string> argumentNames) =>
            AggregateOutput("Available spells are:", torSpellNames);

        [CommandLineFunctionality.CommandLineArgumentFunction("list_player_spells", "tor")]
        public static string ListPlayerSpells(List<string> argumentNames) =>
            !CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType)
                ? CampaignCheats.ErrorType
                : AggregateOutput("Player got these spells:", Hero.MainHero.GetExtendedInfo().AllAbilites);

        
        
        [CommandLineFunctionality.CommandLineArgumentFunction("add_spells_to_player", "tor")]
        public static string AddSpells(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            var matchedArguments = new List<string>();
            var newSpells = new List<string>();
            var knownSpells = new List<string>();

            foreach (var argument in arguments)
            {
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
                
                //TODO Testing stuff, will be merged in ability below
                /*foreach (var torSpell in torCareerAbilities)           
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
                    }*/
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
        
        
        [CommandLineFunctionality.CommandLineArgumentFunction("selectNode", "tor")]
        public static string selectNode(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;
            
            if (!(Game.Current.GameType is Campaign)) return "Current Campaign Mode is not Campaign";

            var text="";
            var careerBase = CampaignBehaviorBase.GetCampaignBehavior<CareerCampaignBase>();
            if (careerBase.SelectNode(arguments[0], out text))
            {
                return $"Chose {arguments[0]}. ";
            }
            else
            {
                return $"Couldn't Choose {arguments[0]}. {text} ";
            }
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("get_all_nodes", "tor")]
        public static string GetAllNodes(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;
            
            var State = arguments[0];
            
            var valid = TreeNodeState.TryParse(arguments[0], out TreeNodeState state);
            if (!valid) return "Not a valid node state";
            
            var careerBase = CampaignBehaviorBase.GetCampaignBehavior<CareerCampaignBase>();

            var list = careerBase.GetAllNodes(state);

            string concat ="";
            foreach (var element in list)
            {
                concat = concat.Add(element+", ",false);
            }
            
            return concat + $" are {state}";
        }
        
        [CommandLineFunctionality.CommandLineArgumentFunction("change_Career", "tor")]
        public static string ChangeCareer(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;
            
            if (!(Game.Current.GameType is Campaign)) return "Current Campaign Mode is not Campaign";
            if (arguments.Count == 0)
                return "Please select a Career";
            
            var valid = CareerId.TryParse(arguments[0], out CareerId career);
            if (!valid) return "Not a valid career";
            var careerBase = CampaignBehaviorBase.GetCampaignBehavior<CareerCampaignBase>();
            careerBase.SelectCareer(career);
            return $"Chose {career} as new career. ";
        }

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
        
        

        private static string AggregateOutput(string topicHeader, List<string> matchedSpells) =>
            matchedSpells.Aggregate(
                $"\n{topicHeader}\n",
                (current, spell) =>
                    $"{current}{spell}\n"
            );
    }
}