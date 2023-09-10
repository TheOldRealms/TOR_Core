using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAIRecruitmentCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, TORRecruitmentBehavior);
        }

        public override void SyncData(IDataStore dataStore)
        {
        }

        private void TORRecruitmentBehavior(Hero recruiter, Settlement settlement, Hero recruitmentSource, CharacterObject troop, int amount)
        {
            if(recruiter==null) return;
            if (recruiter == Hero.MainHero) return;
            
            if (recruiter.CharacterObject.IsBloodDragon())
            {
                if (troop.StringId == "tor_vc_vampire_newblood") return;
                for (int i = 0; i < amount; i++)
                {
                    var random = MBRandom.RandomFloat;
                    if ((!troop.IsBasicTroop && random > 0.25f)||random > 0.75f)
                    {
                        var bloodKnightInitate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_bd_blooddragon_initiate");
                        recruiter.PartyBelongedTo.Party.AddMember(bloodKnightInitate, 1);
                    }
                }
                if(recruitmentSource!=null)
                    recruitmentSource.SetPersonalRelation(recruiter, recruitmentSource.GetBaseHeroRelation(recruiter)-1);
                recruiter.PartyBelongedTo.Party.AddMember(troop, -amount);
            }
        }
    }
}