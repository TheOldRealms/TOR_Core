﻿using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;
using TOR_Core.AbilitySystem;
using TOR_Core.Extensions;
using TOR_Core.Ink;
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
        
        
        [CommandLineFunctionality.CommandLineArgumentFunction("list_spells", "tor")]
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

        private static string AggregateOutput(string topicHeader, List<string> matchedSpells) =>
            matchedSpells.Aggregate(
                $"\n{topicHeader}\n",
                (current, spell) =>
                    $"{current}{spell}\n"
            );
    }
}