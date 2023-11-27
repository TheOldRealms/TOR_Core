using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Localization;

namespace TOR_Core.CampaignMechanics.CustomResources
{
    public class CustomResource
    {
        public string StringId { get; private set; }
        public string Name { get; private set; }
        public TextObject LocalizedName { get; private set; }
        public string Description { get; private set; }
        public TextObject LocalizedDescription { get; private set; }
        public string IconName { get; private set; }

        public CustomResource(string id, string name, string description, string icon)
        {
            StringId = id;
            Name = name;
            Description = description;
            IconName = icon;
            LocalizedName = new TextObject("{=resname_" + StringId + "}" + Name);
            LocalizedDescription = new TextObject("{=resdesc_" + StringId + "}" + Description);
        }
    }
}
