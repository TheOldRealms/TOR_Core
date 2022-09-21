using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.Core;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Quests
{
    public static class TORQuestHelper
    {
        public static T GetCurrentActiveIfExists<T>() where T : QuestBase
        {
            QuestBase returnvalue = null;
            if (Campaign.Current.QuestManager.Quests.Any(x => x is T && x.IsOngoing))
            {
                returnvalue = Campaign.Current.QuestManager.Quests.FirstOrDefault(x => x is T && x.IsOngoing);
            }
            return (T)returnvalue;
        }

        public static bool IsQuestActive<T>() where T : QuestBase
        {
            return Campaign.Current.QuestManager.Quests.Any(x => x is T && x.IsOngoing);
        }

        public static EngineerQuest GetNewEngineerQuest(bool checkForExisting)
        {
            if (checkForExisting)
            {
                var quest = GetCurrentActiveIfExists<EngineerQuest>();
                if (quest != null) return quest;
            }
            var hero = Hero.OneToOneConversationHero;
            if(hero == null || !hero.IsMasterEngineer())
            {
                hero = Hero.AllAliveHeroes.Where(x => x.IsMasterEngineer()).TakeRandom(1).FirstOrDefault();
            }

            string questName = "Runaway Parts";
            string DisplayNameForCultistParty = "Runaway thiefs";
            string nameOfCultistLeader ="Part Thief Leader";
            string cultistLeaderTemplate = "tor_bw_cultist_lord_0";
            string cultistPartyTemplate = "broken_wheel";
            string cultistFaction="forest_bandits";
            string nameOfRogueEngineer = "Goswin";
            string RogueEngineerPartyTemplate = "tor_engineerquesthero";
            string rogueEngineerFaction = "..."; //TODO

            return new EngineerQuest("engineerQust", hero, CampaignTime.DaysFromNow(1000),
                100,
                questName,
                DisplayNameForCultistParty,
                cultistLeaderTemplate,
                cultistPartyTemplate,
                cultistFaction,
                RogueEngineerPartyTemplate,
                RogueEngineerPartyTemplate,
                rogueEngineerFaction);
        }

        public static SpecializeLoreQuest GetNewSpecializeLoreQuest(bool checkForExisting)
        {
            if (checkForExisting)
            {
                var quest = GetCurrentActiveIfExists<SpecializeLoreQuest>();
                if (quest != null) return quest;
            }
            var hero = Hero.OneToOneConversationHero;
            if(hero == null || !hero.IsSpellTrainer())
            {
                hero = Hero.AllAliveHeroes.Where(x => x.IsSpellTrainer()).TakeRandom(1).FirstOrDefault();
            }
            return new SpecializeLoreQuest("practicemagic", hero, CampaignTime.DaysFromNow(1000), 100);
        }
    }
}
