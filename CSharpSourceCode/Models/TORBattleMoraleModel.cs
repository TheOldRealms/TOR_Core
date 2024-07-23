using Helpers;
using SandBox.GameComponents;
using System;
using System.Runtime.CompilerServices;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.Extensions;

namespace TOR_Core.Models
{
    public class TORBattleMoraleModel : SandboxBattleMoraleModel
    {
        public override bool CanPanicDueToMorale(Agent agent)
        {
            var leader = agent.GetPartyLeaderCharacter();
            if(leader != null && leader.GetPerkValue(TORPerks.GunPowder.SteelTerror) && agent.HasAttribute("ArtilleryCrew"))
            {
                return false;
            }
            if (agent.IsUndead() || agent.IsUnbreakable() || agent.IsTreeSpirit() || agent.Origin is SummonedAgentOrigin) return false;
            else return base.CanPanicDueToMorale(agent);
        }

        public override float GetEffectiveInitialMorale(Agent agent, float baseMorale)
        {
            if (agent.Origin is SummonedAgentOrigin) return baseMorale;
            else return base.GetEffectiveInitialMorale(agent, baseMorale);
        }

        public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentIncapacitated(Agent affectedAgent, AgentState affectedAgentState, Agent affectorAgent, in KillingBlow killingBlow)
        {
            var result = base.CalculateMaxMoraleChangeDueToAgentIncapacitated(affectedAgent, affectedAgentState, affectorAgent, killingBlow);
            if(killingBlow.WeaponClass == (int)WeaponClass.Boulder || killingBlow.WeaponClass == (int)WeaponClass.Cartridge)
            {
                var leader = affectorAgent.GetPartyLeaderCharacter();
                if(leader != null && leader.GetPerkValue(TORPerks.GunPowder.SteelTerror))
                {
                    ExplainedNumber num = new ExplainedNumber(result.affectedSideMaxMoraleLoss);
                    PerkHelper.AddPerkBonusFromCaptain(TORPerks.GunPowder.SteelTerror, leader, ref num);
                    result.affectedSideMaxMoraleLoss = num.ResultNumber;
                }
            }
            return result;
        }
    }
}
