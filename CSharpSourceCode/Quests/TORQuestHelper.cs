using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
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
