using System.Drawing;
using TaleWorlds.CampaignSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.Models.Diplomacy.Aggression
{
    public static class ReligiousAggressionCalculator
    {
        public static float DetermineEffectOfReligion(IFaction faction1, IFaction faction2)
        {
            var kingdomHeroes = faction1.Heroes;
            var otherSideHeroes = faction2.Heroes;

            float religionValue = 0f;

            foreach (var hero in kingdomHeroes)
            {
                var heroReligion = hero.GetDominantReligion();
                if (heroReligion == null || !heroReligion.IsReady || !heroReligion.IsInitialized) continue;
                var heroDevotionLevel = hero.GetDevotionLevelForReligion(heroReligion);
                if(heroDevotionLevel == DevotionLevel.None) continue;

                foreach (var enemy in otherSideHeroes)
                {
                    var enemyReligion = enemy.GetDominantReligion();
                    if (enemyReligion == null || !enemyReligion.IsReady || !enemyReligion.IsInitialized) continue;
                    var enemyDevotionLevel = enemy.GetDevotionLevelForReligion(enemyReligion);
                    if (enemyDevotionLevel == DevotionLevel.None) continue;

                    religionValue += DeterminePositiveEffect(hero, heroReligion, heroDevotionLevel, enemy, enemyReligion, enemyDevotionLevel);
                    religionValue += DetermineNegativeEffect(hero, heroReligion, heroDevotionLevel, enemy, enemyReligion, enemyDevotionLevel);
                }
            }

            return religionValue; // / factionToDeclareWarOn.Heroes.Count;
        }

        // Max Value: 100
        // Min Value: 0
        private static float DeterminePositiveEffect(Hero hero, ReligionObject religion, DevotionLevel heroDevotion, Hero enemy, ReligionObject comparedToReligion, DevotionLevel enemyDevotion)
        {
            if (religion.HostileReligions.Contains(comparedToReligion))
                return 0;

            var value = 0;

            var shareAffinity = religion.Affinity == comparedToReligion.Affinity;
            var isSame = religion.Name == comparedToReligion.Name;

            if (isSame)
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                        {
                            value += 100;
                        }
                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 25;
                        else
                            value += 50;
                        break;
                    case DevotionLevel.Follower:
                        if (enemyDevotion != DevotionLevel.Fanatic)
                            value += 10;
                        break;
                    case DevotionLevel.None:
                        break;
                }

            }
            else if (shareAffinity)
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 10;
                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 25;
                        else
                            value += 50;
                        break;
                    case DevotionLevel.Follower:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 0;
                        else
                            value += 25;
                        break;
                    case DevotionLevel.None:
                        break;
                }
            }
            return value;
        }

        // Max Score = 0
        // Min Score = -125
        private static float DetermineNegativeEffect(Hero hero, ReligionObject religion, DevotionLevel heroDevotion, Hero enemy, ReligionObject comparedToReligion, DevotionLevel enemyDevotion)
        {
            var value = 0;

            var shareAffinity = religion.Affinity == comparedToReligion.Affinity;
            var isSame = religion.Name == comparedToReligion.Name;

            if (isSame) //The Person is only hostile if he is fanatic about his religion, and thinks other don't follow enough
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.None)
                        {
                            value += -20;
                        }
                        break;
                }
            }
            else if (shareAffinity)
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        switch (enemyDevotion)
                        {
                            case DevotionLevel.Fanatic:
                                value += -50;
                                break;
                            case DevotionLevel.Follower:
                            case DevotionLevel.Devoted:
                                value += -20;
                                break;
                        }

                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += -30;
                        else
                            value += -15;
                        break;
                    case DevotionLevel.Follower:
                        switch (enemyDevotion)
                        {
                            case DevotionLevel.Fanatic:
                                value += -30;
                                break;
                            case DevotionLevel.Follower:
                            case DevotionLevel.Devoted:
                                value += -20;
                                break;
                        }

                        break;
                    case DevotionLevel.None:
                        break;
                }
            }
            else // hostile and neutral gods.
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                        {
                            value += -100;
                        }
                        break;
                    case DevotionLevel.Devoted:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += -50;
                        else
                            value += -25;
                        break;
                    case DevotionLevel.Follower:
                        switch (enemyDevotion)
                        {
                            case DevotionLevel.Fanatic:
                                value += -30;
                                break;
                            case DevotionLevel.Follower:
                            case DevotionLevel.Devoted:
                                value += -20;
                                break;
                        }
                        ;
                        break;
                    case DevotionLevel.None:
                        break;
                }
            }

            if (enemyDevotion == DevotionLevel.None) return value;

            if (religion.HostileReligions.Contains(comparedToReligion))
            {
                value += -25; //additional base value if the god is hostile, irrespective about the state of religion.
            }

            return value;
        }
    }
}
