using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TOR_Core.BattleMechanics.CustomArenaModes;
using TOR_Core.CampaignMechanics.Assimilation;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORTournamentModel : DefaultTournamentModel
    {
        public override TournamentGame CreateTournament(Town town)
        {
            var culture = AssimilationCampaignBehavior.GetOriginalCultureForSettlement(town.Settlement);
            if (culture != null)
            {
                if (culture.StringId == TORConstants.Cultures.BRETONNIA || culture.StringId == TORConstants.Cultures.MOUSILLON)
                {
                    return new JoustTournamentGame(town);
                }
                if (culture.StringId == TORConstants.Cultures.ASRAI)
                {
                    return new ArcheryContestTournamentGame(town);
                }
                if (culture.StringId == TORConstants.Cultures.GREENSKIN)
                {
                    return new BrawlTournamentGame(town);
                }
            }
            return base.CreateTournament(town);
        }

        public override float GetTournamentStartChance(Town town)
        {
            //return 1f; //DEBUG
            if (town.Settlement.SiegeEvent != null)
            {
                return 0f;
            }

            if (Math.Abs(town.StringId.GetHashCode() % 3) != CampaignTime.Now.GetWeekOfSeason)
            {
                return 0f;
            }

            return 0.1f * (float)(town.Settlement.Parties.Count((MobileParty x) => x.IsLordParty)) + 0.05f * (float)(town.Settlement.HeroesWithoutParty.WhereQ(x => x.IsWanderer).Count());
        }

        public override MBList<ItemObject> GetRegularRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
        {
            //Sly : if the culture has no items, that should be looked into. Not a fan of building a 2nd list to store every other culture's items.
            MBList<ItemObject> mBList = new MBList<ItemObject>();
            MBList<ItemObject> mBList2 = new MBList<ItemObject>();
            foreach (ItemObject item in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
            {
                if (!item.NotMerchandise && (item.Tier == ItemObject.ItemTiers.Tier3 || item.Tier == ItemObject.ItemTiers.Tier4) && (item.IsWeapon() || item.IsMountable || item.ArmorComponent != null || item.IsShield()) && !item.IsCraftedByPlayer && item.IsTorItem())
                {
                    if (item.Culture == town.Culture)
                    {
                        mBList.Add(item);
                    }
                    else
                    {
                        mBList2.Add(item);
                    }
                }
            }

            //Sly : banners left out because native will collect all banners even unrelated cultures

            if (mBList.IsEmpty())
            {
                return mBList2;
            }

            return mBList;
        }

        public override MBList<ItemObject> GetEliteRewardItems(Town town, int regularRewardMinValue, int regularRewardMaxValue)
        {
            MBList<ItemObject> mBList = new MBList<ItemObject>();
            foreach (ItemObject item in Game.Current.ObjectManager.GetObjectTypeList<ItemObject>())
            {
                if (!item.NotMerchandise && item.Culture == town.Culture && item.Tier > ItemObject.ItemTiers.Tier4 && (item.IsWeapon() || item.IsMountable || item.ArmorComponent != null || item.IsShield()) && !item.IsCraftedByPlayer && item.IsTorItem())
                {
                    mBList.Add(item);
                }
            }

            if (mBList.IsEmpty())
            {
                TORCommon.Log("TORTournamentModel : no t5 or 6 item found belonging to the " + town.Culture.StringId + " culture. They get the first item object instead.", NLog.LogLevel.Warn);
                mBList.Add(Game.Current.ObjectManager.GetFirstObject<ItemObject>());
            }

            return mBList;
        }
    }
}