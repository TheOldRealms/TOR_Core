using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.SaveSystem;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class MobilePartyExtendedInfo
    {
        [SaveableField(0)] public string CurrentBlessingStringId = null;
        [SaveableField(1)] public int CurrentBlessingRemainingDuration = -1;
    }

    public class MobilePartyExtendedInfoDefiner : SaveableTypeDefiner
    {
        public MobilePartyExtendedInfoDefiner() : base(1_533_177) { }
        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(MobilePartyExtendedInfo), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, MobilePartyExtendedInfo>));
        }
    }
}
