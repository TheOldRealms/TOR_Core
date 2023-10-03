using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace TOR_Core.CampaignMechanics.Invasions
{
    public class InvasionForce
    {
        private List<Clan> _clans;
        private List<MobileParty> _parties;

        private InvasionForce()
        {
            _clans = new List<Clan>();
            _parties = new List<MobileParty>();
        }

        public static InvasionForce Create()
        {
            return new InvasionForce();
        }
    }
}
