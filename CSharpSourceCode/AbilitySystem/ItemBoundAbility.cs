using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class ItemBoundAbility : Ability
    {
        private int _chargeNum = 0;
        private int _chargeMaximum = 0;

        public ItemBoundAbility(AbilityTemplate template) : base(template) { }

        public void SetChargeNum(int amount, bool affectMaximum = false)
        {
            _chargeNum = amount;
            
            if (affectMaximum) _chargeMaximum = amount;

            if (_chargeNum > _chargeMaximum && !affectMaximum) _chargeNum = _chargeMaximum;//being granted additional charges during the mission doesn't increase past the max set during AbilityComponent creation.
        }

        public override bool IsDisabled(Agent casterAgent, out TextObject disabledReason)
        {
            if (_chargeNum <= 0)
            {
                disabledReason = TORTextHelper.GetTextObject("tor_no_artillery_inventory", "No more artillery pieces in inventory");//TODO : needs to be updated for a generic string as artillery is no longer the only itembound ability
                return true;
            }
            // Anvil of Doom doesn't use artillery slots
            if (Template.AbilityEffectType == AbilityEffectType.ArtilleryPlacement && Mission.Current.GetArtillerySlotsLeftForTeam(casterAgent.Team) <= 0)
            {
                disabledReason = TORTextHelper.GetTextObject("tor_no_artillery_slots", "Party cannot field more artillery pieces");
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

        public int GetMaximumCharges()
        {
            return _chargeMaximum;
        }
    }
}