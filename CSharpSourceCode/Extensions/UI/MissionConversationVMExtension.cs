using System.Globalization;
using SandBox.Conversation.MissionLogics;
using SandBox.View.Conversation;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.TwoDimension;
using TOR_Core.CampaignMechanics.CustomResources;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI;

[ViewModelExtension(typeof(MissionConversationVM))]
public class MissionConversationVMExtension: BaseViewModelExtension
{
    private string  _customResourceValue;
    
    private Sprite _customResourceSprite;
    
    public MissionConversationVMExtension(ViewModel vm) : base(vm)
    {
        RefreshValues();

        Campaign.Current.ConversationManager.ConversationContinued += OnConversationContinued;      //For what ever reason the update on the the refresh values is only called very rarely
    }
    public override void RefreshValues()
    {


        var resource = Hero.MainHero.GetCultureSpecificCustomResource();
        
        CustomResourceValue = Hero.MainHero.GetCultureSpecificCustomResourceValue().ToString();
        
        CustomResourceSprite = UIResourceManager.SpriteData.GetSprite(resource.LargeIconName);
        
    }

    public void OnConversationContinued()
    {
        RefreshValues();
    }

    public override void OnFinalize()
    {
        Campaign.Current.ConversationManager.ConversationContinued -= OnConversationContinued;
        base.OnFinalize();
        
    }

    [DataSourceProperty]
    public Sprite CustomResourceSprite
    {
        get
        {
            return this._customResourceSprite;
        }
        set
        {
            if (value != this._customResourceSprite)
            {
                this._customResourceSprite = value;
                _vm.OnPropertyChangedWithValue(value, "CustomResourceSprite");
            }
        }
    }
    
    [DataSourceProperty]
    public string CustomResourceValue
    {
        get
        {
            return this._customResourceValue;
        }
        set
        {
            if (value != this._customResourceValue)
            {
                this._customResourceValue = value;
                _vm.OnPropertyChangedWithValue(value, "CustomResourceValue");
            }
        }
    }
    
    
    
    
    
    
}