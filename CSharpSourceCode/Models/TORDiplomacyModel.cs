using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.Localization;
using TOR_Core.CampaignMechanics.Diplomacy.Mechanics.Aggression;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORDiplomacyModel : DefaultDiplomacyModel
    {
        public override float GetRelationIncreaseFactor(Hero hero1, Hero hero2, float relationChange)
        {
            var baseValue = base.GetRelationIncreaseFactor(hero1, hero2, relationChange);
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
                        if (conversationHero != null && conversationHero.Culture.StringId == TORConstants.BRETONNIA_CULTURE)
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
            var torScoreOfDeclaringWar = TORAggressionCalculator.TorAggressionScore(factionDeclaresWar, factionDeclaredWar);

            var nativeScoreOfDeclaringWar = base.GetScoreOfDeclaringWar(factionDeclaresWar, factionDeclaredWar, evaluatingClan, out warReason);

            /// Applying extra multiplers to increase wars
            torScoreOfDeclaringWar *= TORConfig.DeclareWarScoreMultiplierTor;
            nativeScoreOfDeclaringWar *= TORConfig.DeclareWarScoreMultiplierNative;

            //TORCommon.Say($"Declaring war between {factionDeclaresWar.Name} and {factionDeclaredWar.Name}\n\t\t" +
            //    $"Native Score: {nativeScoreOfDeclaringWar},\tTOR Score: {torScoreOfDeclaringWar}");

            return nativeScoreOfDeclaringWar + torScoreOfDeclaringWar;
        }

        public override float GetScoreOfDeclaringPeace(IFaction factionDeclaresPeace, IFaction factionDeclaredPeace, IFaction evaluatingClan, out TextObject peaceReason)
        {
            // Chaos really shouldn't be allowed to make peace
            if (evaluatingClan.Culture.StringId == "chaos_culture")
            {
                peaceReason = new TextObject("Chaos will fight to the death!");
                return float.MinValue;
            }

            var nativeScoreOfDeclaringPeace = base.GetScoreOfDeclaringPeace(factionDeclaresPeace, factionDeclaredPeace, evaluatingClan, out peaceReason);
            var torScore = TORAggressionCalculator.TorAggressionScore(factionDeclaresPeace, factionDeclaredPeace);

            /// Apply same multipliers as declaration of war to try to them in agreement as much as possible
            nativeScoreOfDeclaringPeace *= TORConfig.DeclareWarScoreMultiplierNative;
            torScore *= TORConfig.DeclareWarScoreMultiplierTor / TORConfig.DeclarePeaceMultiplier;

            //TORCommon.Say($"Declaring peace between {factionDeclaresPeace.Name} and {factionDeclaredPeace.Name}\n\t\t" +
            //    $"Native Score: {nativeScoreOfDeclaringPeace},\tTOR Score: {torScore}");

            return nativeScoreOfDeclaringPeace - torScore;
        }

        public override float GetScoreOfMercenaryToJoinKingdom(Clan mercenaryClan, Kingdom kingdom)
        {
            var score = base.GetScoreOfMercenaryToJoinKingdom(mercenaryClan, kingdom);

            if (kingdom == null) return score;
            if (mercenaryClan == null) return score;

            if (kingdom.Culture.StringId == TORConstants.BRETONNIA_CULTURE && mercenaryClan.Culture.StringId != TORConstants.BRETONNIA_CULTURE)
            {
                score = -10000;
            }

            if (mercenaryClan.StringId == "tor_dog_clan_hero_curse" && kingdom.Culture.StringId == TORConstants.SYLVANIA_CULTURE || kingdom.Culture.StringId == "mousillon" || kingdom.Culture.StringId == TORConstants.BRETONNIA_CULTURE)
            {
                score = -10000;
            }

            return score;
        }
    }
}