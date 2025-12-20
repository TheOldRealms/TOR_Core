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

        InventoryScreenHelper.CloseScreen(true);//Sly : resets hero's equipment due to a bug in the native logic that sets the "prior equipment" to the basic char object's sets when inventory is opened despite equipment for heroes stored on the HeroObject....
        //passing true causes the inventory to be reset which makes use of the "prior equipment" because it cancels all transactions and resets to the previous equipment

        campaignBehavior.OpenStory(story);
    }
}