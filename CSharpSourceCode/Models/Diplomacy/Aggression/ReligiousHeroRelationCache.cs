using System;
using System.Collections.Generic;
using System.Drawing;
using TaleWorlds.CampaignSystem;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.Extensions;

namespace TOR_Core.Models.Diplomacy.Aggression
{
    public class ReligiousHeroRelationCache
    {
        private static readonly Lazy<ReligiousHeroRelationCache> _lazy = new Lazy<ReligiousHeroRelationCache>(() => new ReligiousHeroRelationCache());
        public static ReligiousHeroRelationCache Instance
        {
            get
            {
                return _lazy.Value;
            }
        }

        protected Dictionary<(MBGUID, MBGUID), HeroRelationshipModel> _relationshipCache;
        private ReligiousHeroRelationCache()
        {
            _relationshipCache = new Dictionary<(MBGUID, MBGUID), HeroRelationshipModel>();
        }

        public int GetRelationship(Hero hero1, Hero hero2)
        {
            HeroRelationshipModel currentInfo = null;
            if (_relationshipCache.ContainsKey((hero1.Id, hero2.Id)))
            {
                currentInfo = _relationshipCache[(hero1.Id, hero2.Id)];
            }
            else if (_relationshipCache.ContainsKey((hero2.Id, hero1.Id)))
            {
                currentInfo = _relationshipCache[(hero2.Id, hero1.Id)];
            }
            else
            {
                currentInfo = new HeroRelationshipModel(hero1, hero2, GenerateRelationshipScore(hero1, hero2));
            }
            bool needUpdate = currentInfo.HeroesDevotion[hero1.Id] != hero1.GetDevotionToDominantReligion() ||
                    currentInfo.HeroesDevotion[hero2.Id] != hero2.GetDevotionToDominantReligion();

            if (needUpdate)
            {
                currentInfo.HeroesDevotion[hero1.Id] = hero1.GetDevotionToDominantReligion();
                currentInfo.HeroesDevotion[hero2.Id] = hero2.GetDevotionToDominantReligion();
                currentInfo.RelationshipLevel = GenerateRelationshipScore(hero1, hero2);
            }

            return currentInfo.RelationshipLevel;
        }

        protected int GenerateRelationshipScore(Hero hero1, Hero hero2)
        {
            var religion = hero1.GetDominantReligion();
            var comparedToReligion = hero2.GetDominantReligion();
            var heroDevotion = hero1.GetDevotionToDominantReligion();
            var enemyDevotion = hero2.GetDevotionToDominantReligion();

            //if (religion.HostileReligions.Contains(comparedToReligion))
            //    return 0;
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
                    case DevotionLevel.None: //The Person is only hostile if he is fanatic about his religion, and thinks other don't follow enough
                        value += -20;
                        break;
                }

            }
            else if (shareAffinity)
            {
                switch (heroDevotion)
                {
                    case DevotionLevel.Fanatic:
                        // Positive effect
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += 10;

                        // Negative effect
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
                            value += -5;//postive only: 25;
                        else
                            value += 35;//postive only: 50;
                        break;
                    case DevotionLevel.Follower:
                        if (enemyDevotion == DevotionLevel.Fanatic)
                            value += -30; //postive only: 0;
                        else if (enemyDevotion == DevotionLevel.None)
                            value += 25;
                        else
                            value += -20;
                        break;
                    //case DevotionLevel.None:
                    //    break;
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

            if (religion.HostileReligions.Contains(comparedToReligion))
            {
                value += -25; //additional base value if the god is hostile, irrespective about the state of religion.
            }

            return value;
        }
    }
}
