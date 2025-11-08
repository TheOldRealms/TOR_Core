using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ViewModelCollection.Conversation;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TOR_Core.Extensions.UI;

[ViewModelExtension(typeof(MissionConversationVM), "Refresh")]
public class MissionConversationVMExtension: BaseViewModelExtension
{
    private string  _customResourceValue;
    
    private Sprite _customResourceSprite;
    
    public MissionConversationVMExtension(ViewModel vm) : base(vm)
    {
        RefreshValues();
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

    [DataSourceProperty]
    public Sprite CustomResourceSprite
    {
        get
        {
            return _customResourceSprite;
        }
        set
        {
            if (value != _customResourceSprite)
            {
                _customResourceSprite = value;
                _vm.OnPropertyChangedWithValue(value, "CustomResourceSprite");
            }
        }
    }
    
    [DataSourceProperty]
    public string CustomResourceValue
    {
        get
        {
            return _customResourceValue;
        }
        set
        {
            if (value != _customResourceValue)
            {
                _customResourceValue = value;
                _vm.OnPropertyChangedWithValue(value, "CustomResourceValue");
            }
        }
    }
}