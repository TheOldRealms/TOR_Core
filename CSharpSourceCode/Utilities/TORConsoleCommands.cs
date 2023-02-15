using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
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

        private static string AggregateOutput(string topicHeader, List<string> matchedSpells) =>
            matchedSpells.Aggregate(
                $"\n{topicHeader}\n",
                (current, spell) =>
                    $"{current}{spell}\n"
            );
    }
}