using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
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
                if(template != null)
                {
                    
                    var troop = MBObjectManager.Instance.GetObject<CharacterObject>(template.BaseTroopId);
                    
                    if (troop != null)
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

            if (settlement.Culture.StringId == TORConstants.Cultures.EONIR)
            {
                if (settlement.IsTown)
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

            if (settlement.Owner.Clan != null && settlement.Owner.Clan.Kingdom != null && settlement.Owner.Clan.Kingdom.GetNumActiveKingdomWars() > 0)
            {
                value *= 2;
            }

            return value;

        }
        
        public override int MaximumIndexHeroCanRecruitFromHero(Hero buyerHero, Hero sellerHero, int useValueAsRelation = -101)
        {
            if (GetBasicVolunteer(sellerHero).IsUndead())
            {
                if (!buyerHero.IsNecromancer() || !buyerHero.PartyBelongedTo.GetMemberHeroes().Any(x=> x.IsNecromancer()))
                {
                    return -1;
                }
            }
            
            var value = base.MaximumIndexHeroCanRecruitFromHero(buyerHero, sellerHero, useValueAsRelation);

            if (buyerHero.IsEnlisted())
            {
                return -1;
            }
            
            return value;
        }
    }
}
