using SandBox.CampaignBehaviors;
using SandBox.Issues;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Issues;

namespace TOR_Core.Utilities
{
    public static class TORGameStarterHelper
    {
        public static void CleanCampaignStarter(CampaignGameStarter starter)
        {
            starter.RemoveBehaviors<BackstoryCampaignBehavior>();
            starter.RemoveBehaviors<InitialChildGenerationCampaignBehavior>();
            starter.RemoveBehaviors<PartyUpgraderCampaignBehavior>();
            starter.RemoveBehaviors<RetirementCampaignBehavior>();
            starter.RemoveBehaviors<DynamicBodyCampaignBehavior>();
            starter.RemoveBehaviors<FactionDiscontinuationCampaignBehavior>();
            starter.RemoveBehaviors<KingdomDecisionProposalBehavior>();
            starter.RemoveBehaviors<RebellionsCampaignBehavior>();
            starter.RemoveBehaviors<SallyOutsCampaignBehavior>();

            var issues = starter.CampaignBehaviors.Where(x => x.GetType().FullName.Contains("Issue")).ToList();
            foreach(var issue in issues)
            {
                starter.RemoveBehavior(issue);
            }
        }

        public static void AddVerifiedIssueBehaviors(CampaignGameStarter starter)
        {
            starter.AddBehavior(new IssuesCampaignBehavior());
            starter.AddBehavior(new ArmyNeedsSuppliesIssueBehavior());
            starter.AddBehavior(new ArtisanCantSellProductsAtAFairPriceIssueBehavior());
            starter.AddBehavior(new ArtisanOverpricedGoodsIssueBehavior());
            starter.AddBehavior(new BettingFraudIssueBehavior());
            starter.AddBehavior(new ExtortionByDesertersIssueBehavior());
            starter.AddBehavior(new FamilyFeudIssueBehavior());
            starter.AddBehavior(new GangLeaderNeedsToOffloadStolenGoodsIssueBehavior());
            starter.AddBehavior(new HeadmanNeedsGrainIssueBehavior());
            starter.AddBehavior(new HeadmanNeedsToDeliverAHerdIssueBehavior());
            starter.AddBehavior(new HeadmanVillageNeedsDraughtAnimalsIssueBehavior());
            starter.AddBehavior(new LandLordTheArtOfTheTradeIssueBehavior());
            starter.AddBehavior(new LordNeedsHorsesIssueBehavior());
            starter.AddBehavior(new LordsNeedsTutorIssueBehavior());
            starter.AddBehavior(new LordWantsRivalCapturedIssueBehavior());
            starter.AddBehavior(new NearbyBanditBaseIssueBehavior());
            starter.AddBehavior(new NotableWantsDaughterFoundIssueBehavior());
            starter.AddBehavior(new ProdigalSonIssueBehavior());
            starter.AddBehavior(new RaidAnEnemyTerritoryIssueBehavior());
            starter.AddBehavior(new RivalGangMovingInIssueBehavior());
            starter.AddBehavior(new ScoutEnemyGarrisonsIssueBehavior());
            starter.AddBehavior(new TheConquestOfSettlementIssueBehavior());
            starter.AddBehavior(new VillageNeedsToolsIssueBehavior());
        }
    }
}
