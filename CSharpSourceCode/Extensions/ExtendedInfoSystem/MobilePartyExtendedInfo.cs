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
}
