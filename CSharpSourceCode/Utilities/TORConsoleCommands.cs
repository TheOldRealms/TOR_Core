using Helpers;
using SandBox.View.Map;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;
using TOR_Core.AbilitySystem;
using TOR_Core.BattleMechanics.TriggeredEffect;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Ink;
using TOR_Core.Items;
using TOR_Core.Missions;
using TOR_Core.Quests;
using FaceGen = TaleWorlds.Core.FaceGen;

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
                Campaign.Current.MainParty.Position = NavigationHelper.FindReachablePointAroundPosition(engineerquest.TargetParty.Position, MobileParty.NavigationType.Default, 1f);

                return " *puff*... there he is!";
            }
            return "Engineer Quest is not active \n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("whereAreAICompanions", "tor")]
        public static string ShowCompanionPosition(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (CampaignCheats.CheckHelp(arguments))
            {
                string helpInfo = "Will attempt to find and describe the state of every AI companion. Expect a delay as it iterates through every hero and lordParty.";
                return helpInfo;
            }

            string result = "";

            //Witch Hunter quest givers and town magic trainers are Special, but they also are Supporters of a clan leader whereas AICompanions aren't
            var aiCompanions = Hero.FindAll(x => x.IsSpecial && x.SupporterOf == null);
            //a companion only has the AICompanion attribute if they are still alive, the dictionary entry is removed on death
            var aliveAiCompanions = aiCompanions.Where(x => x.IsAICompanion());
            result = result.Add("Alive companions : " + aliveAiCompanions.Count().ToString());
            foreach (var companion in aliveAiCompanions)
            {
                var partResult = "";
                partResult = partResult.Add(companion.Name.ToString() + "'s state is " + companion.HeroState.ToString() + ".", false);
                if (companion.HeroDeveloper == null)
                {
                    partResult = partResult.Add(" HeroDev is null.", false);
                }

                if (companion.CurrentSettlement != null)
                {
                    partResult = partResult.Add(companion.Name.ToString() + " " + " is currently in " + companion.CurrentSettlement);
                    result += partResult;
                    continue;
                }

                if (companion.PartyBelongedTo != null)
                {
                    partResult = partResult.Add(companion.Name.ToString() + " " + " is part of  " + companion.PartyBelongedTo.LeaderHero.Name + " Party" + companion.PartyBelongedTo.GetPosition2D);
                    result += partResult;
                    continue;
                }

                if (companion.CurrentSettlement == null && companion.PartyBelongedTo == null)
                {
                    partResult = partResult.Add(companion.Name.ToString() + " " + " is nowhere to be found.");
                    result += partResult;
                    continue;
                }
            }

            var deadAiCompanions = aiCompanions.Where(x => !aliveAiCompanions.Contains(x));

            //unsure how to set up lambas in a single line to put it into an enumerable
            //var partiesForDeads = MobileParty.AllLordParties.Where(x => x.GetMemberHeroes().Exists(y => y.)
            //if i'm checking all of these anyways, would it be worthwhile to create a tuple so that each hero can have a list of parties they are part of in case there are multiple, then I can use the tuple after to read out the parties that are in when printing? Do I expect to have multiple parties that a single hero is supposed to be in?
            List<MobileParty> lordParties = new();
            foreach (var lordParty in MobileParty.AllLordParties)
            {
                foreach (var deadHero in deadAiCompanions)
                {
                    if (lordParty.MemberRoster.Contains(deadHero.CharacterObject))
                    {
                        lordParties.Add(lordParty);
                    }
                }
            }

            result = result.Add("");
            result = result.Add("Dead special heroes : " + deadAiCompanions.Count().ToString());
            result = result.Add("Parties containing a dead specHero : " + lordParties.Count().ToString());

            foreach (var deadCompanion in deadAiCompanions)
            {
                string deadCompanionStates = "\n";
                deadCompanionStates = deadCompanionStates.Add(deadCompanion.Name.ToString() + "'s state is " + deadCompanion.HeroState.ToString() + ".", false);
                var deadHerosParties = lordParties.Where(x => x.MemberRoster.Contains(deadCompanion.CharacterObject));
                foreach (var party in deadHerosParties)
                {
                    if (party.Owner != null)
                    {
                        deadCompanionStates = deadCompanionStates.Add(" Party owned by : " + party.Owner.Name.ToString() + ".", false);
                    }

                    if (party.LeaderHero == null) { deadCompanionStates = deadCompanionStates.Add(" Leader is null.", false); }

                    else { deadCompanionStates = deadCompanionStates.Add(" LeaderHero is " + party.LeaderHero.Name.ToString() + ".", false); }
                }
                result += deadCompanionStates;
            }
            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("set_alliance", "tor")]
        public static string SetAlliance(List<string> strings)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            string usage = "tor.set_alliance [Kingdom1] | [Kingdom2]\nCreates an alliance between two kingdoms. Use kingdom StringIds (e.g., 'empire', 'bretonnia').";

            if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings))
                return usage;

            List<string> separatedNames = CampaignCheats.GetSeparatedNames(strings, true);
            if (separatedNames.Count != 2)
                return usage;

            string kingdom_str1 = separatedNames[0].ToLower().Replace(" ", "");
            string kingdom_str2 = separatedNames[1].ToLower().Replace(" ", "");

            Kingdom faction1 = null;
            Kingdom faction2 = null;

            foreach (var kingdom in Campaign.Current.Kingdoms)
            {
                if (kingdom_str1 == kingdom.StringId.ToLower())
                {
                    faction1 = kingdom;
                }
                if (kingdom_str2 == kingdom.StringId.ToLower())
                {
                    faction2 = kingdom;
                }
            }

            if (faction1 == null)
                return "Kingdom not found: " + kingdom_str1 + "\n" + usage;

            if (faction2 == null)
                return "Kingdom not found: " + kingdom_str2 + "\n" + usage;

            if (faction1 == faction2)
                return "Cannot create alliance with self.\n" + usage;

            if (faction1.IsAtWarWith(faction2))
                return "Cannot create alliance - kingdoms are at war. Use tor.declare_peace first.\n";

            if (faction1.IsAllyWith(faction2))
                return faction1.Name + " and " + faction2.Name + " are already allies.\n";

            // Use the clean alliance method that removes native Call to War decisions
            // This is preferred for TOR since we use our own HonorAllianceDecision system
            faction1.SetAlliance(faction2);

            return "Alliance created between " + faction1.Name + " and " + faction2.Name + ".\n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("break_alliance", "tor")]
        public static string BreakAlliance(List<string> strings)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            string usage = "tor.break_alliance [Kingdom1] | [Kingdom2]\nBreaks an alliance between two kingdoms.";

            if (CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckParameters(strings, 1) || CampaignCheats.CheckHelp(strings))
                return usage;

            List<string> separatedNames = CampaignCheats.GetSeparatedNames(strings, true);
            if (separatedNames.Count != 2)
                return usage;

            string kingdom_str1 = separatedNames[0].ToLower().Replace(" ", "");
            string kingdom_str2 = separatedNames[1].ToLower().Replace(" ", "");

            Kingdom faction1 = null;
            Kingdom faction2 = null;

            foreach (var kingdom in Campaign.Current.Kingdoms)
            {
                if (kingdom_str1 == kingdom.StringId.ToLower())
                {
                    faction1 = kingdom;
                }
                if (kingdom_str2 == kingdom.StringId.ToLower())
                {
                    faction2 = kingdom;
                }
            }

            if (faction1 == null)
                return "Kingdom not found: " + kingdom_str1 + "\n" + usage;

            if (faction2 == null)
                return "Kingdom not found: " + kingdom_str2 + "\n" + usage;

            if (!faction1.IsAllyWith(faction2))
                return faction1.Name + " and " + faction2.Name + " are not allies.\n";

            // Use the IAllianceCampaignBehavior to break the alliance
            var allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
            if (allianceBehavior != null)
            {
                allianceBehavior.EndAlliance(faction1, faction2);
                return "Alliance broken between " + faction1.Name + " and " + faction2.Name + ".\n";
            }

            return "Error: Could not find alliance behavior.\n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("list_kingdoms", "tor")]
        public static string ListKingdoms(List<string> strings)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (CampaignCheats.CheckHelp(strings))
                return "Lists all kingdoms with their StringIds for use with alliance/war commands.";

            string result = "Available Kingdoms:\n";
            foreach (var kingdom in Campaign.Current.Kingdoms.OrderBy(k => k.StringId))
            {
                string allies = kingdom.AlliedKingdoms.Count > 0
                    ? " [Allies: " + string.Join(", ", kingdom.AlliedKingdoms.Select(a => a.StringId)) + "]"
                    : "";
                result += $"  {kingdom.StringId} - {kingdom.Name}{allies}\n";
            }
            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("test_alliance_decision", "tor")]
        public static string TestAllianceDecision(List<string> strings)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            string usage = "tor.test_alliance_decision [AllyKingdom] | [AttackerKingdom]\nManually dispatches a HonorAllianceDecision to test the UI.";

            if (CampaignCheats.CheckHelp(strings) || CampaignCheats.CheckParameters(strings, 0) || CampaignCheats.CheckParameters(strings, 1))
                return usage;

            var playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == null)
                return "Player must be in a kingdom.\n";

            List<string> separatedNames = CampaignCheats.GetSeparatedNames(strings, true);
            if (separatedNames.Count != 2)
                return usage;

            string allyStr = separatedNames[0].ToLower().Replace(" ", "");
            string attackerStr = separatedNames[1].ToLower().Replace(" ", "");

            Kingdom ally = Campaign.Current.Kingdoms.FirstOrDefault(k => k.StringId.ToLower() == allyStr);
            Kingdom attacker = Campaign.Current.Kingdoms.FirstOrDefault(k => k.StringId.ToLower() == attackerStr);

            if (ally == null)
                return "Ally kingdom not found: " + allyStr + "\n";
            if (attacker == null)
                return "Attacker kingdom not found: " + attackerStr + "\n";

            string result = "Debug info:\n";
            result += $"  Player kingdom: {playerKingdom.Name}\n";
            result += $"  Player ruling clan: {playerKingdom.RulingClan?.Name}\n";
            result += $"  Player clan: {Clan.PlayerClan?.Name}\n";
            result += $"  Is allied with {ally.Name}: {playerKingdom.IsAllyWith(ally)}\n";
            result += $"  {ally.Name} at war with {attacker.Name}: {ally.IsAtWarWith(attacker)}\n";
            result += $"  Player at war with {attacker.Name}: {playerKingdom.IsAtWarWith(attacker)}\n";

            var decision = new TOR_Core.CampaignMechanics.Diplomacy.HonorAllianceDecision(
                playerKingdom.RulingClan, ally, attacker);

            result += $"  Decision.IsAllowed(): {decision.IsAllowed()}\n";
            result += $"  Decision.Kingdom: {decision.Kingdom?.Name}\n";

            decision.IsEnforced = true;
            result += $"  Decision.IsEnforced: {decision.IsEnforced}\n";
            result += $"  Decision.NeedsPlayerResolution: {decision.NeedsPlayerResolution}\n";

            if (!decision.IsAllowed())
            {
                decision.CanMakeDecision(out var reason, true);
                result += $"  CanMakeDecision reason: {reason}\n";
                return result + "\nDecision not allowed - check conditions above.\n";
            }

            playerKingdom.AddDecision(decision, true);
            result += "\nDecision dispatched! Check if election UI appears.\n";

            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("add_enchantment_blueprint", "tor")]
        public static string AddEnchantmentBlueprint(List<string> arguments)
        {
            if (Campaign.Current == null) return "Function only available when playing in campaign mode.";
            var trait = "";
            var hero = Hero.MainHero;
            if (arguments.Count >= 3)
            {
                return "either use 1 or 2 arguments. Just 1 argument : Main hero learns blueprint. 2 Arguments : hero with the name learns blueprint";
            }
            if (arguments.Count == 1)
            {
                trait = arguments[0];
            }
            if (arguments.Count == 2)
            {
                hero = null;
                var potentialHeroes = Campaign.Current.AliveHeroes.Where(x => x.Name.ToString() == arguments[0]).ToList();

                if (!potentialHeroes.Any())
                {
                    return "no Hero with the given Name could be found";
                }

                foreach (var potentialHero in potentialHeroes)
                {
                    if (hero.PartyBelongedTo == MobileParty.MainParty)
                    {
                        if (hero.Name == potentialHero.Name)
                        {
                            hero = potentialHero;
                            break;
                        }
                    }

                    if (hero.Clan != Clan.PlayerClan && hero.Clan.Kingdom != Hero.MainHero.Clan.Kingdom) continue;
                    if (hero.Name != potentialHero.Name) continue;

                    hero = potentialHero;
                    break;

                }

                trait = arguments[1];
            }

            if (hero == null)
            {
                return "no Hero with the given Name could be found in Clan or Kingdom";
            }

            var obj = ItemTrait.All.FirstOrDefault(x => x.ItemTraitStringId == trait);
            if (obj == null)
            {
                return ("There exists no trait with the id " + trait);
            }

            hero.AddEnchantmentBlueprint(trait);


            return "Blueprint added: " + trait + "to " + hero.Name;
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

            Agent target = null;
            int damage;
            if (int.TryParse(arguments[0], out damage))
            {
                target = Mission.Current.MainAgent;
            }
            else
            {
                if (arguments.Count == 2 && int.TryParse(arguments[1], out damage))
                {
                    target = Mission.Current.Agents.FirstOrDefault(x => x.Name == arguments[0]);
                }
            }

            if (target == null) return "Couldn't find agent";
            target.ApplyDamage(damage, target.Position);
            return "Damaged " + target.Name + " with " + damage + "\n";

        }

        [CommandLineFunctionality.CommandLineArgumentFunction("add_blessing", "tor")]
        public static string AddBlessingToPlayer(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            var deityName = arguments[0];
            var religion = ReligionObject.All.FirstOrDefault(x => x.DeityName.ToString().Equals(deityName, StringComparison.OrdinalIgnoreCase));
            if (religion != null)
            {
                if (Hero.MainHero.PartyBelongedTo == null) return "not in a party";
                var blessingText = religion.BlessingEffectName;
                if (blessingText == null) return "blessing description not found";
                Hero.MainHero.PartyBelongedTo.AddBlessingToParty(religion.StringId);

                return string.Format("Player now has the {0}. \n", blessingText);
            }
            else return "No religion with the deity name '" + deityName + "' found. \n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("add_devotion", "tor")]
        public static string AddDevotionToPlayer(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (CampaignCheats.CheckHelp(arguments))
                return "Usage: tor.add_devotion [DeityName] [Amount]\nAdds the specified amount of devotion to the player for the given religion.\n";

            if (arguments == null || arguments.Count < 2)
                return "Incorrect arguments. Usage: tor.add_devotion [DeityName] [Amount]\n";

            var deityName = arguments[0];
            if (!int.TryParse(arguments[1], out int amount))
                return "Amount must be a valid integer.\n";

            var religion = ReligionObject.All.FirstOrDefault(x => x.DeityName.ToString().Equals(deityName, StringComparison.OrdinalIgnoreCase));
            if (religion == null)
            {
                religion = ReligionObject.All.FirstOrDefault(x => x.StringId.Equals(deityName, StringComparison.OrdinalIgnoreCase));
            }

            if (religion != null)
            {
                Hero.MainHero.AddReligiousInfluence(religion, amount);
                var currentLevel = Hero.MainHero.GetDevotionLevelForReligion(religion);
                return string.Format("Added {0} devotion to {1}. Current devotion level: {2}.\n", amount, religion.DeityName, currentLevel);
            }
            else
            {
                return "No religion with the deity name or id '" + deityName + "' found.\n";
            }
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("add_career", "tor")]
        public static string AddCareerToPlayer(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            var careerId = arguments[0];
            var career = TORCareers.All.FirstOrDefault(x => x.StringId == careerId);
            if (career != null)
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

            if (arguments == null || arguments.Count == 0)
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

        [CommandLineFunctionality.CommandLineArgumentFunction("reload_config", "tor")]
        public static string ReloadTorConfig(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            TORConfig.ReadConfig();

            return "TOR config reloaded. \n";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("open_test_mission", "tor")]
        public static string OpenTestMission(List<string> arguments)
        {
            var template = MBObjectManager.Instance.GetObject<PartyTemplateObject>("chaos_cultists");
            TorMissionManager.OpenQuestMission("tor_test_scene_for_stuff", template, 9, null, false);
            return "Scene opened.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("change_player_race", "tor")]
        public static string ChangeRace(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (arguments == null || arguments.Count == 0)
            {
                return "Argument cannot be null. Pass in the race id. \n";
            }

            var raceId = arguments[0];
            var raceIndex = FaceGen.GetRaceOrDefault(raceId);

            if (raceIndex == -1)
                return "race is not existent";

            Hero.MainHero.CharacterObject.Race = raceIndex;

            return string.Format("Successfully changed player race to {0}.", raceId);
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("add_player_attribute", "tor")]
        public static string AddPlayerAttribute(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (arguments == null || arguments.Count == 0)
            {
                return "Argument cannot be null. Pass in attribute to add. \n";
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
            if (arguments.Count != 2) return string.Format("Incorrect arguments. Usage is \"tor.add_custom_resource ResourceName amount\" ");

            string resourceId = arguments[0];
            int amount = 0;
            if (int.TryParse(arguments[1], out amount))
            {
                var resource = CustomResourceManager.GetResourceObject(resourceId);
                if (resource != null)
                {
                    Hero.MainHero.AddCustomResource(resourceId, amount);
                    return string.Format("Successfully added {0} {1} to main hero.", amount.ToString(), resource.Name);
                }
                return string.Format("Custom resource with id {0} not found.", resourceId);
            }

            return string.Format("Incorrect arguments. Usage is \"tor.add_custom_resource ResourceName amount\" ");
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
                var patch = Campaign.Current.MapSceneWrapper.GetMapPatchAtPosition(MobileParty.MainParty.Position);
                return $"Battle scene index at position ({MobileParty.MainParty.Position.ToString()}) is: {patch.sceneIndex}";
            }
            else return "Function only available while on the world map.";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("print_map_position_data", "tor")]
        public static string PrintMapPositionData(List<string> arguments)
        {
            if (CampaignCheats.CheckHelp(arguments))
            {
                return "\nPrints out x, y, z position of the main party on the campaign map, the battle scene index, and possible scene names.\n";
            }

            var campaign = Campaign.Current;
            if (campaign == null) return "\nFunction only available when playing in campaign mode.\n";
            if (!campaign.GameStarted) return "\nFunction only available when a game is started.\n";
            if (campaign.MapSceneWrapper == null) return "\nMap scene wrapper is null.\n";

            string result = "\nCampaign Position Data\n";

            var mainParty = MobileParty.MainParty;
            result = result.Add("Main party position as (x, y, z) : " + mainParty.GetPositionAsVec3().ToString());


            var mapPatch = campaign.MapSceneWrapper.GetMapPatchAtPosition(mainParty.Position);
            result = result.Add("Battle scene index is " + mapPatch.sceneIndex);


            var possibleScenes = GameSceneDataManager.Instance.SingleplayerBattleScenes.WhereQ((SingleplayerBattleSceneData scene) => scene.MapIndices.Contains(mapPatch.sceneIndex)).ToMBList();
            var scenesConcatenated = "";
            possibleScenes.ForEach(x => scenesConcatenated = scenesConcatenated.Add("\n" + x.SceneID, false));
            //scenesConcatenated.TrimEnd([' ', '|']);
            result = result.Add("Battle scenes at position : " + scenesConcatenated);


            return result;
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("print_mission_position_data", "tor")]
        public static string PrintMissionPositionData(List<string> arguments)
        {
            if (CampaignCheats.CheckHelp(arguments))
            {
                return "\nPrints out x, y, z position of the main agent (or the camera if dead), and the battle scene name.\n";
            }

            var mission = Mission.Current;
            if (mission == null) return "\nFunction only available when a mission is open.\n";

            string result = "\nMission Position Data\n";

            result = result.Add("Scene name : " + mission.SceneName);


            if (mission.MainAgent?.IsActive() == true) //I think the Z value is the terrain height of the agent at their feet, so I'm leaving the cases split for the moment.
            {
                result = result.Add("Player position as (x, y, z) : " + mission.MainAgent.Position.ToString());
            }
            else //player dead, use camera instead
            {
                var missionScreen = ScreenManager.TopScreen as MissionScreen;
                if (missionScreen == null)
                {
                    result = result.Add("Mission screen null, that's not good.");
                }
                else
                {
                    result = result.Add("Camera position with (x, y, z) : " + missionScreen.CombatCamera.Position.ToString());
                }
            }

            return result;
        }

        /* Sly : Finish in a future commit. I want to be able to visualize distances from a position by drawing circles of specific radii.
        [CommandLineFunctionality.CommandLineArgumentFunction("add_circle_to_player_party", "tor")]
        public static string AddCircleToPlayerParty(List<string> arguments)
        {
            if (CampaignCheats.CheckHelp(arguments))
            {
                return "\nDraws a decal around the player party with the radius of choice.\n";
            }

            
            MapScene mapScene = Campaign.Current.MapSceneWrapper as MapScene;
            _markerEntity = GameEntity.CreateEmpty(mapScene.Scene, true);
            _markerEntity.Name = "PartyMeasurementDecal+" + arguments[0];
            _markerDecal = Decal.CreateDecal();
            if (_markerDecal != null && _markerEntity != null)
            {
                Material resource = Material.GetFromResource("decal_city_circle_a");
                _markerDecal.SetMaterial(resource);
                mapScene.Scene.AddDecalInstance(_markerDecal, "editor_set", false);
                _markerEntity.AddComponent(_markerDecal);
            }


            if (_markerEntity == null) CreateVisuals();
            if (_markerEntity != null)
            {
                MatrixFrame frame = new MatrixFrame(Mat3.Identity, PartyVisualManager.Current.GetVisualOfParty(Settlement.Party).GetVisualPosition());
                frame.Scale(new Vec3(32, 32, 1));
                _markerEntity.SetGlobalFrame(frame);
                _markerDecal.SetFactor1Linear(4281663744U);
                _markerEntity.SetVisibilityExcludeParents(shouldShow);
            }
            _isMarkerShown = shouldShow;

            
            return "";
        }
        */

        [CommandLineFunctionality.CommandLineArgumentFunction("finalize_quest", "tor")]
        public static string FinalizeQuest(List<string> arguments)
        {
            if (!CampaignCheats.CheckCheatUsage(ref CampaignCheats.ErrorType))
                return CampaignCheats.ErrorType;

            if (Campaign.Current == null) return "Function only available when playing in campaign mode.";

            if (arguments.Count == 0)
                return "Please provide a quest ID. Usage: tor.finalize_quest <questId>\n";

            string questId = arguments[0];

            // Only allow specific quests that support this completion method
            var supportedQuests = new[] { "OrcBossQuest1", "OrcBossQuest2" };
            if (!supportedQuests.Contains(questId))
                return $"Quest '{questId}' cannot be finalized this way. Supported quests: {string.Join(", ", supportedQuests)}\n";

            // Find the quest by ID
            var quest = Campaign.Current.QuestManager.Quests.FirstOrDefault(q => q.StringId == questId);
            if (quest == null)
                return $"Quest with ID '{questId}' is not active.\n";

            // Complete all tasks by setting progress to max
            foreach (var entry in quest.JournalEntries)
            {
                entry.UpdateCurrentProgress(Int32.MaxValue);
            }

            return $"Quest '{questId}' requirements completed! Quest will finalize on next hourly tick.\n";
        }

        private static string AggregateOutput(string topicHeader, List<string> matchedSpells) =>
            matchedSpells.Aggregate(
                $"\n{topicHeader}\n",
                (current, spell) =>
                    $"{current}{spell}\n"
            );
    }
}