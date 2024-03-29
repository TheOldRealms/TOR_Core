using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Religion;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORDiplomacyModel : DefaultDiplomacyModel
    {
        public override float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationChange)
        {
            var baseValue =  base.GetRelationIncreaseFactor(hero1, hero2, relationChange);
            var values = new ExplainedNumber(baseValue);

            var playerHero = hero1.IsHumanPlayerCharacter || hero2.IsHumanPlayerCharacter ? (hero1.IsHumanPlayerCharacter ? hero1 : hero2) : null;
            if (playerHero == null) return baseValue;

            var conversationHero = !hero1.IsHumanPlayerCharacter || !hero2.IsHumanPlayerCharacter ? (!hero1.IsHumanPlayerCharacter ? hero1 : hero2) : null;
            if (playerHero.HasAnyCareer())
            {
                var choices = playerHero.GetAllCareerChoices();

                if (choices.Contains("CourtleyPassive1"))
                {
                    if (baseValue > 0)
                    {
                        var choice = TORCareerChoices.GetChoice("CourtleyPassive1");
                        if (choice != null)
                        {
                            var value = choice.Passive.InterpretAsPercentage ? choice.Passive.EffectMagnitude / 100 : choice.Passive.EffectMagnitude;
                            values.AddFactor(value);
                        }
                    }
                }
                
                if (choices.Contains("JustCausePassive4"))
                {
                    if (baseValue > 0)
                    {
                        if (conversationHero != null && conversationHero.Culture.StringId == "vlandia")
                        {
                            var choice = TORCareerChoices.GetChoice("JustCausePassive4");
                            if (choice != null)
                            {
                                var value = choice.Passive.InterpretAsPercentage ? choice.Passive.EffectMagnitude / 100 : choice.Passive.EffectMagnitude;
                                values.AddFactor(value);
                            } 
                        }
                    }
                }
            }
            return values.ResultNumber;
        }
        
        public override float GetScoreOfDeclaringWar(IFaction factionDeclaresWar, IFaction factionDeclaredWar, IFaction evaluatingClan, out TextObject warReason)
        {
            float scoreOfDeclaringWar = base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out warReason);
            float determineEffectOfReligion = DetermineEffectOfReligion(factionDeclaresWar, factionDeclaredWar, (Clan)evaluatingClan);
            //TORCommon.Say("War support Score: " + scoreOfDeclaringWar + " + Religion effect:" + determineEffectOfReligion);
            return scoreOfDeclaringWar + determineEffectOfReligion;
        }


        private float DetermineEffectOfReligion(IFaction factionDeclaresWar, IFaction factionToDeclareWarOn, IFaction evaluatingClan)
        {
            var kingdomHeroes = factionDeclaresWar.Heroes;

            float religionValue = 0f;

            foreach (var hero in kingdomHeroes)
            {
                var otherSideHeroes = factionToDeclareWarOn.Heroes;
                foreach (var enemy in otherSideHeroes)
                {
                    foreach (var religion in ReligionObject.All)
                    {
                        if (hero.GetDevotionLevelForReligion(religion) == DevotionLevel.None)
                            continue;
                        foreach (var comparedToReligion in ReligionObject.All)
                        {
                            religionValue += DeterminePositiveEffect(hero, religion, enemy, comparedToReligion);
                            religionValue += DetermineNegativeEffect(hero, religion, enemy, comparedToReligion);
                        }
                    }
                }
            }

            return religionValue; // / factionToDeclareWarOn.Heroes.Count;
        }

        public static float DeterminePositiveEffect(Hero hero, ReligionObject religion, Hero enemy, ReligionObject comparedToReligion)
        {
            if (religion.HostileReligions.Contains(comparedToReligion))
                return 0;

            var value = 0;
            var heroDevotion = hero.GetDevotionLevelForReligion(religion);
            var enemyDevotion = enemy.GetDevotionLevelForReligion(comparedToReligion);

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

        private float DetermineNegativeEffect(Hero hero, ReligionObject religion, Hero enemy, ReligionObject comparedToReligion)
        {
            var value = 0;

            var heroDevotion = hero.GetDevotionLevelForReligion(religion);
            var enemyDevotion = enemy.GetDevotionLevelForReligion(comparedToReligion);

            if (heroDevotion == DevotionLevel.None) //If a hero is not devoted at all, he should also not care about any hostility.
                return value;

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