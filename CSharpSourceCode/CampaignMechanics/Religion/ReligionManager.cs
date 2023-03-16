using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TOR_Core.CampaignMechanics.Religion
{
    public class ReligionManager
    {
        private static ReligionManager instance;

        private ReligionManager() { }

        public static void Initialize()
        {
            instance = new ReligionManager();
        }
    }
}
