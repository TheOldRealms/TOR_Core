using System.Collections.Generic;
using System.ComponentModel;
using TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia.Pages;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.UI;
[ViewModelExtension(typeof(GroupedOptionCategoryVM))]
public class OptionsVMExtension : BaseViewModelExtension
{
    private bool _containsTORGameplayOptions;

    private string _tORResetText;

    public OptionsVMExtension(ViewModel vm) : base(vm)
    {
        var t = vm as OptionsVM;
    }

    public override void RefreshValues()
    {
        base.RefreshValues();

        if (this._vm is GroupedOptionCategoryVM)
        {
            var t = this._vm as GroupedOptionCategoryVM;

            if (t.Name.Contains("Gameplay"))
            {
                ContainsTORGameplayOptions = true;

                TORResetText = "Reset TOR Settings";
                
                
            }
          //  var m = t.GetActiveCategory();
            

            
        }
        

    }

    public override void OnFinalize()
    {
        base.OnFinalize();

        TORConfig.UpdateConfiguraiton();
        
    }

    
    [DataSourceProperty]
    public bool ContainsTORGameplayOptions
    {
        get
        {
            return _containsTORGameplayOptions;
        }
        set
        {
            if (value != _containsTORGameplayOptions)
            {
                _containsTORGameplayOptions = value;
                _vm.OnPropertyChangedWithValue(value, "ContainsTORGameplayOptions");
            }
        }
    }
    
    
    private void ExecuteResetToTORDefault()
    {
        List<InquiryData> data = new List<InquiryData>();

        var title = "Reset TOR Settings";
        var explaination = "This will reset all TOR related  settings to default.";
        var inquiry = new InquiryData(title.ToString(),
            explaination.ToString(),
            true, 
            true, 
            "Accept", "Abort", ()=> TORConfig.SetDefaultValues(),null);
        InformationManager.ShowInquiry(inquiry);
    }
    
    
    [DataSourceProperty]
    public string TORResetText
    {
        get
        {
            return _tORResetText;
        }
        set
        {
            if (value != _tORResetText)
            {
                _tORResetText = value;
                _vm.OnPropertyChangedWithValue(value, "TORResetText");
            }
        }
    }
}