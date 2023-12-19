using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics.CustomResources
{
    public class CustomResource
    {
        public string StringId { get; private set; }
        public string Name { get; private set; }
        public TextObject LocalizedName { get; private set; }
        public string Description { get; private set; }
        public TextObject LocalizedDescription { get; private set; }
        public string SmallIconName { get; private set; }
        public string LargeIconName { get; private set; }
        public string Culture { get; private set; }

        public CustomResource(string id, string name, string description, string icon, string associatedCultureId = "neutral_culture")
        {
            StringId = id;
            Name = name;
            Description = description;
            SmallIconName = icon;
            LargeIconName = icon.Replace("_45", "_100");
            LocalizedName = new TextObject("{=resname_" + StringId + "}" + Name);
            LocalizedDescription = new TextObject("{=resdesc_" + StringId + "}" + Description);
            Culture = associatedCultureId;
        }

        public string GetCustomResourceIconAsText(bool useLarge = false)
        {
            return string.Format("<img src=\"{0}\"/>", useLarge ? LargeIconName : SmallIconName);
        }
    }
}
