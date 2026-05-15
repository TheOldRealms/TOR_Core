using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Library;

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
            conversationItemVM.ItemText = matchingText.ToString();
        }
    }
}
