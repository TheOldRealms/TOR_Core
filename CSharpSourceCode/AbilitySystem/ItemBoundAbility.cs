using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;

namespace TOR_Core.AbilitySystem
{
    public class ItemBoundAbility : Ability
    {
        private int _chargeNum = 0;

        public ItemBoundAbility(AbilityTemplate template) : base(template) { }

        public void SetChargeNum(int amount)
        {
            _chargeNum = amount;
        }

        public override bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
        {
            if(_chargeNum <= 0) 
            {
                disabledReason = new TextObject("{=!}No more artillery pieces in inventory");
                return true;
            }
            if(Mission.Current.GetArtillerySlotsLeftForTeam(casterAgent.Team) <= 0)
            {
                disabledReason = new TextObject("{=!}Party cannot field more artillery pieces");
                return true;
            }
            return base.IsDisabled(casterAgent, out disabledReason);
        }

        protected override void DoCast(Agent casterAgent)
        {
            base.DoCast(casterAgent);
            _chargeNum--;
        }

        public int GetRemainingCharges()
        {
            return _chargeNum;
        }
    }
}
