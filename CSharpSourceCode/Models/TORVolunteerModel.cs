﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics.RegimentsOfRenown;
using TOR_Core.Extensions;

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
                    if(troop != null) return troop;
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
    }
}