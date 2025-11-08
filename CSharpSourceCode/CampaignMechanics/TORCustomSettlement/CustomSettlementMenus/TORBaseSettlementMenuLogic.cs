using TaleWorlds.CampaignSystem;

namespace TOR_Core.CampaignMechanics.TORCustomSettlement.CustomSettlementMenus;

public abstract class TORBaseSettlementMenuLogic
{
    public TORBaseSettlementMenuLogic(CampaignGameStarter campaignGameStarter)
    {
        AddSettlementMenu(campaignGameStarter);
    }
    protected abstract void AddSettlementMenu(CampaignGameStarter campaignGameStarter);
    
    protected CampaignTime _startWaitTime;

    protected int numberOfTroopsFromInteraction;
}