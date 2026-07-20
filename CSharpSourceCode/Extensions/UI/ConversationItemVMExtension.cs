using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TOR_Core.Extensions.UI;

[ViewModelExtension(typeof(ConversationItemVM), "RefreshValues")]
public class ConversationItemVMExtension : BaseViewModelExtension
{
    public ConversationItemVMExtension(ViewModel vm) : base(vm)
    {
    }

    public override void RefreshValues()
    {
        if (_vm is not ConversationItemVM conversationItemVM) return;

        var curOptions = Campaign.Current?.ConversationManager?.CurOptions;
        if (curOptions == null || curOptions.Count == 0) return;

        var index = conversationItemVM.Index;
        if (index < 0 || index >= curOptions.Count) return;

        var option = curOptions[index];
        if (string.IsNullOrEmpty(option.Id)) return;

        var character = CharacterObject.OneToOneConversationCharacter;
        if(character==null) 
            character = Hero.MainHero.CharacterObject;

        var stringID = option.Text.GetID();
        if (string.IsNullOrEmpty(stringID)) return;

        var matchingText = Campaign.Current.ConversationManager.FindMatchingTextOrNull(stringID, character);
        if (matchingText != null)
        {
            if (option.Text.Attributes?.Count > 0)
            {
                var attributeTransferText = new TextObject(matchingText.Value, option.Text.Attributes);
                matchingText = attributeTransferText;//Sly : why do TextObjects not have anything for transferring the attributes? You can copy a text object which transfers them, but not just assigning the attributes directly.
            }
            conversationItemVM.ItemText = matchingText.ToString();
        }
    }
}
