using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics;
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
            if (recruiter == null) return;
            if (recruiter == Hero.MainHero) return;

            if (recruiter.CharacterObject.IsBloodDragon())
            {
                if (troop.StringId == "tor_vc_vampire_newblood") return;
                for (int i = 0; i < amount; i++)
                {
                    var random = MBRandom.RandomFloat;
                    if (( !troop.IsBasicTroop && random > 0.25f ) || random > 0.75f)
                    {
                        var bloodKnightInitate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_bd_blooddragon_initiate");
                        recruiter.PartyBelongedTo.Party.AddMember(bloodKnightInitate, 1);
                    }
                }
                
                recruiter.PartyBelongedTo.Party.AddMember(troop, -amount);
            }


            if (recruiter.HasAttribute("Everchosen"))
            {
                CharacterObject replacement = null;
   

                if (troop.IsEliteTroop())
                {
                    replacement = MBObjectManager.Instance.GetObject<CharacterObject>("tor_chaos_undivided_warrior");
                }
                else
                {
                    replacement = MBObjectManager.Instance.GetObject<CharacterObject>("tor_chaos_aspiring_warrior");
                }
                
                recruiter.PartyBelongedTo.Party.AddMember(troop, -amount);
                recruiter.PartyBelongedTo.Party.AddMember(replacement, amount );
            }

            if (troop.IsEliteTroop() && recruiter.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                CharacterObject replacement = null;
               if(recruiter.HasAttribute("Bergerac"))
               {
                   replacement = MBObjectManager.Instance.GetObject<CharacterObject>("tor_ror_bergerac_ranger");
               }
               
               if(recruiter.HasAttribute("PeasantKnight"))
               {
                   replacement = MBObjectManager.Instance.GetObject<CharacterObject>("tor_ror_peasant_squight");
               }

               if (replacement != null)
               {
                   recruiter.PartyBelongedTo.Party.AddMember(replacement,amount);
                   var currentNumber = recruiter.PartyBelongedTo.Party.MemberRoster.GetTroopCount(troop);
                   recruiter.PartyBelongedTo.Party.AddMember(troop, MBMath.ClampInt(-amount,-currentNumber,0));
               }

            }

            if (recruiter.CharacterObject.IsBrassKeepLord())
            {
                recruiter.PartyBelongedTo.Party.AddMember(troop, -amount);

                if (troop.IsEliteTroop())
                {
                    var random = MBRandom.RandomFloat;
                    var chaosKnight = random > 0.5f
                        ? MBObjectManager.Instance.GetObject<CharacterObject>("tor_chaos_nurgle_warrior")
                        : MBObjectManager.Instance.GetObject<CharacterObject>("tor_chaos_pugulist");
                    if (chaosKnight != null)
                    {
                        recruiter.PartyBelongedTo.Party.AddMember(chaosKnight,amount);
                        recruiter.PartyBelongedTo.Party.AddMember(troop, -amount);
                    }
              
                }
                else
                {
                    var raider = MBObjectManager.Instance.GetObject<CharacterObject>("tor_chaos_norscan_raider");
                    recruiter.PartyBelongedTo.Party.AddMember(raider,amount);
                }
            }
            
            

            if (recruiter.IsLord && troop.Culture.StringId == TORConstants.Cultures.MOUSILLON && recruiter.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                var mousillonEquivalent = TORRecruitmentHelpers.GetMousillonEquivalent(troop);
                if (mousillonEquivalent != null)
                {
                    recruiter.PartyBelongedTo.Party.AddMember(mousillonEquivalent, amount);
                    recruiter.PartyBelongedTo.Party.AddMember(troop, -amount);
                }
            }

            if (recruiter.IsLord && troop.Culture.StringId == TORConstants.Cultures.BRETONNIA && recruiter.Culture.StringId == TORConstants.Cultures.MOUSILLON)
            {
                var bretonniaEquivalent = TORRecruitmentHelpers.GetBretonnianEquivalent(troop);
                if (bretonniaEquivalent != null)
                {
                    recruiter.PartyBelongedTo.Party.AddMember(bretonniaEquivalent, amount);
                    recruiter.PartyBelongedTo.Party.AddMember(troop, -amount);
                }
            }
        }
    }
}