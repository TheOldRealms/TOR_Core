using Helpers;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;
using TOR_Core.CharacterDevelopment;
using TOR_Core.CharacterDevelopment.CareerSystem;
using TOR_Core.Extensions;
using static TOR_Core.Utilities.TORConstants;

namespace TOR_Core.Models
{
    public class TORBattleMoraleModel : SandboxBattleMoraleModel
    {
        public override bool CanPanicDueToMorale(Agent agent)
        {
            var leader = agent.GetPartyLeaderCharacter();
            if (leader != null && leader.GetPerkValue(TORPerks.GunPowder.SteelTerror) && agent.HasAttribute(CharacterAttributes.ARTILLERY_CREW))
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
            var hasPerk = affectorAgent.GetOriginMobileParty()?.LeaderHero?.GetPerkValue(TORPerks.GunPowder.SteelTerror);
            //bulky : unsure if there's a way to condense the WeaponClass check
            if (hasPerk == true && (killingBlow.WeaponRecordWeaponFlags.HasAnyFlag(WeaponFlags.AffectsArea | WeaponFlags.AffectsAreaBig)) && (killingBlow.WeaponClass == (int)WeaponClass.Boulder || killingBlow.WeaponClass == (int)WeaponClass.Cartridge || killingBlow.WeaponClass == (int)WeaponClass.Stone))
            {
                //AddPerkBonusForParty has an immediate null check for the party and the partyLeader
                MobileParty affectorParty = affectorAgent.GetOriginMobileParty();

                ExplainedNumber num = new ExplainedNumber(result.affectedSideMaxMoraleLoss);
                //primary bonus is morale damage
                PerkHelper.AddPerkBonusForParty(TORPerks.GunPowder.SteelTerror, affectorParty, true, ref num);
                result.affectedSideMaxMoraleLoss = num.ResultNumber;
            }

            // Apply career passive morale damage bonuses (e.g., Ulrican kills causing extra morale damage)
            if (Hero.MainHero?.HasAnyCareer() == true)
            {
                var moraleDamageBonus = new ExplainedNumber(result.affectedSideMaxMoraleLoss);
                CareerHelper.ApplyCombatCareerPassives(Hero.MainHero, ref moraleDamageBonus, PassiveEffectType.MoraleDamageToEnemyOnKill, affectorAgent, affectedAgent);
                result.affectedSideMaxMoraleLoss = moraleDamageBonus.ResultNumber;
            }

            return result;
        }
    }
}