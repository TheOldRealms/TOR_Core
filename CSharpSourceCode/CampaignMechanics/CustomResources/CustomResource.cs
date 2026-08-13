using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Localization;
using TOR_Core.Extensions;

namespace TOR_Core.CampaignMechanics.CustomResources
{
    public class CustomResource
    {
        public string StringId { get; private set; }
        public TextObject Name { get; private set; }
        public TextObject Description { get; private set; }
        public string SmallIconName { get; private set; }
        public string LargeIconName { get; private set; }
        public List<string> Cultures { get; private set; }

        private TextToolTipFunction _toolTipFunction;
        public delegate List<TooltipProperty> TextToolTipFunction();

        public CustomResource(string id, string icon, string associatedCulturesId = null, TextToolTipFunction function = null)
        {
            associatedCulturesId ??= "neutral_culture";
            StringId = id; //eg. ForestHarmony
            Name = GameTexts.FindText("tor_custom_resource_name", id);
            Description = GameTexts.FindText("tor_custom_resource_description", id);
            SmallIconName = icon;
            LargeIconName = icon.Replace("_45", "_100");
            if (function != null)
            {
                _toolTipFunction = function;
            }

            Cultures = [associatedCulturesId];
        }

        public CustomResource(string id, string icon, string[] associatedCulturesIds)
        {
            associatedCulturesIds ??= ["neutral_culture"];
            StringId = id;
            Name = GameTexts.FindText("tor_custom_resource_name", id);
            Description = GameTexts.FindText("tor_custom_resource_description", id);
            SmallIconName = icon;
            LargeIconName = icon.Replace("_45", "_100");
            Cultures = [.. associatedCulturesIds];
        }

        public List<TooltipProperty> GetCustomTooltipDescription()
        {
            return _toolTipFunction != null ? _toolTipFunction.Invoke() : [];
        }

        public string GetCustomResourceIconAsText(bool useLarge = false)
        {
            return string.Format("<img src=\"{0}\"/>", useLarge ? LargeIconName : SmallIconName);
        }


        public float GetCustomResourceGeneralizedFactor()
        {
            var model = Campaign.Current.Models.GetCustomResourceModel();

            if (model == null)
                return 1f;

            return model.GetFactorForGeneralizedCosts(this);

        }
    }
}