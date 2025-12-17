using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects.Usables;
using TOR_Core.Utilities;

namespace TOR_Core.BattleMechanics.Artillery
{
    public class CannonBallPile : SiegeMachineStonePile
    {
        public override TextObject GetActionTextForStandingPoint(UsableMissionObject usableGameObject)
        {
            return TORTextHelper.GetTextObject("tor_cannonball_pile", "Cannonball Pile");
        }

        public override TextObject GetDescriptionText(WeakGameEntity weakGameEntity)
        {
            return TORTextHelper.GetTextObject("tor_cannonball_pile", "Cannonball Pile");
        }
    }
}