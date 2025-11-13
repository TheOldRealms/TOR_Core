using Helpers;
using TaleWorlds.CampaignSystem;
using TOR_Core.Ink;

namespace TOR_Core.Items.InventoryUseScripts;

public class StartInkStoryScript : BaseInventoryUseScript
{
    public StartInkStoryScript(string[] arguments) : base(arguments)
    {

        if (InventoryScreenHelper.GetActiveInventoryState().InventoryMode != InventoryScreenHelper.InventoryMode.Default) return;

        var inkId = arguments[0];

        var story = InkStoryManager.GetStory(inkId);

        if (story == null) return;

        var campaignBehavior = Campaign.Current.GetCampaignBehavior<InkStoryCampaignBehavior>();

        InventoryScreenHelper.CloseScreen(true);

        campaignBehavior.OpenStory(story);
    }
}