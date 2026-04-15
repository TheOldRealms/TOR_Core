using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TOR_Core.Extensions;
using TOR_Core.Quests.Careers;

namespace TOR_Core.Quests
{
    public static class TORQuestHelper
    {

        public static void StartCareerQuest(string questPath)
        {
            StartCareerQuestwithQuestPath(questPath);
        }
        private static void StartCareerQuestwithQuestPath(string questPath)
        {
            if (questPath == null)
                return;
            var hero = Hero.MainHero;
            CampaignTime time = CampaignTime.YearsFromNow(1000);
            var id = questPath.Split('.').LastOrDefault();
            var reward = 0;
            var type = Type.GetType("TOR_Core." + questPath);
            if (Campaign.Current.QuestManager.Quests.Any(x => x.StringId == id && x.IsOngoing))
            {
                return;
            }

            if (Activator.CreateInstance(type, [id, hero, time, reward]) is QuestBase quest)
            {
                quest.StartQuest();
            }

        }

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


        public delegate bool HeroFunction(Hero hero);

        public static T GetCurrentQuest<T>(string id, bool checkForExistence, HeroFunction heroRetrievalFunction, out bool existent) where T : QuestBase
        {
            existent = false;

            if (checkForExistence)
            {
                var quest = GetCurrentActiveIfExists<T>();
                if (quest != null)
                {
                    existent = true;
                    return quest;
                }
            }
            var hero = Hero.OneToOneConversationHero;
            if (hero == null || !heroRetrievalFunction(hero))
            {
                hero = Hero.AllAliveHeroes.FirstOrDefault(x => heroRetrievalFunction(x));
            }

            var result = (T)Activator.CreateInstance(typeof(T), id, hero, CampaignTime.YearsFromNow(10000), 100);
            return result;
        }


        public static EngineerQuest GetNewEngineerQuest(bool checkForExisting)
        {
            if (checkForExisting)
            {
                var quest = GetCurrentActiveIfExists<EngineerQuest>();
                if (quest != null) return quest;
            }
            var hero = Hero.OneToOneConversationHero;
            if (hero == null || !hero.IsMasterEngineer())
            {
                hero = Hero.AllAliveHeroes.Where(x => x.IsMasterEngineer()).TakeRandom(1).FirstOrDefault();
            }


            return new EngineerQuest("engineerQuest", hero, CampaignTime.DaysFromNow(1000),
                100
            );
        }

        public static SpecializeLoreQuest GetNewSpecializeLoreQuest(bool checkForExisting)
        {
            if (checkForExisting)
            {
                var quest = GetCurrentActiveIfExists<SpecializeLoreQuest>();
                if (quest != null) return quest;
            }
            var hero = Hero.OneToOneConversationHero;
            if (hero == null || !hero.IsSpellTrainer())
            {
                hero = Hero.AllAliveHeroes.Where(x => x.IsSpellTrainer()).TakeRandom(1).FirstOrDefault();
            }
            return new SpecializeLoreQuest("practicemagic", hero, CampaignTime.DaysFromNow(1000), 100);
        }
    }
}