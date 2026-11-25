using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORVolunteerModel : DefaultVolunteerModel
    {
        public override int MaxVolunteerTier => 6;

        public override CharacterObject GetBasicVolunteer(Hero sellerHero)
        {
            var settlement = sellerHero.CurrentSettlement;

            if ((bool)(settlement?.IsRoRSettlement()) && IsHeroRoRCapable(sellerHero))
            {
                var template = RORManager.GetTemplateFor(settlement.StringId);
                if (template != null)
                {

                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>(template.BaseTroopId);

                    if (troop != null)
                    {
                        if (troop.Culture == settlement.Culture)
                        {
                            if (troop.IsUndead())
                            {
                                var count = 0;

                                var undeadRoRMaximum = 3;
                                if (settlement.IsVillage)
                                {
                                    undeadRoRMaximum = 1;
                                }

                                for (int i = 0; i < settlement.Notables.Count; i++)
                                {
                                    count++;
                                    if (settlement.Notables[i] == sellerHero)
                                    {
                                        if (count <= undeadRoRMaximum)
                                        {
                                            return troop;
                                        }

                                        return base.GetBasicVolunteer(sellerHero);
                                    }
                                }
                            }
                            return troop;
                        }
                    }
                }
            }

            if (settlement.Culture.StringId == TORConstants.Cultures.EONIR)
            {
                if (settlement.IsTown && settlement.IsTorLithanel())
                {
                    return settlement.Culture.EliteBasicTroop;
                }

                if (settlement.IsVillage)
                {
                    return settlement.Culture.BasicTroop;
                }
            }

            return base.GetBasicVolunteer(sellerHero);
        }

        private bool IsHeroRoRCapable(Hero sellerHero)
        {
            return sellerHero.Occupation == Occupation.Artisan ||
                    sellerHero.Occupation == Occupation.Merchant ||
                    sellerHero.Occupation == Occupation.Headman ||
                    sellerHero.Occupation == Occupation.RuralNotable;
        }

        public override float GetDailyVolunteerProductionProbability(
            Hero hero,
            int index,
            Settlement settlement)
        {
            var value = base.GetDailyVolunteerProductionProbability(hero, index, settlement);

            if (settlement.Owner.Clan != null && settlement.Owner.Clan.Kingdom != null && settlement.Owner.Clan.Kingdom.FactionsAtWarWith.Count > 0)
            {
                value *= 2;
            }

            return value;

        }

        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            if (GetBasicVolunteer(sellerHero).IsUndead())
            {
                if (!buyerHero.IsNecromancer() || !buyerHero.PartyBelongedTo.GetMemberHeroes().Any(x => x.IsNecromancer()))
                {
                    return -1;
                }
            }

            var value = base.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);

            if (buyerHero.IsEnlisted()) //make sure only the player is affected!
            {
                return -1;
            }

            if (buyerHero == Hero.MainHero)
            {
                var model = Campaign.Current.Models.GetHiringCompatibilityModel();
                if (model == null) return value;

                if (!model.CanPlayerHireTroopFromSeller(buyerHero, sellerHero))
                {
                    return -1;
                }
            }

            return value;
        }
    }
}