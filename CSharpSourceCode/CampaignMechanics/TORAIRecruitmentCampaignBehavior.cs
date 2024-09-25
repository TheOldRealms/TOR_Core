using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TOR_Core.CampaignMechanics;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.Models
{
    public class TORAIRecruitmentCampaignBehavior : CampaignBehaviorBase
    {
        private CharacterObject _skeleton;
        
        private CharacterObject _raider;
        private CharacterObject _wraith;

        private const int UndeadCountVillages = 5;
        private const int UndeadCountTowns = 20;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Initialize);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, AddUndeadToPartyOnEnteringSettlement);
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, TORRecruitmentBehavior);
            
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this,DailyTickEvents);
        }

        private void DailyTickEvents(MobileParty party)
        {

            if (party.IsLordParty && !party.IsMainParty && party.LeaderHero != null )
            {
                var clan = party.LeaderHero.Clan;
                if (clan!=null && clan.IsCastleFaction() && clan.Kingdom!=null && !clan.Kingdom.Settlements.AnyQ(x=> x.IsTown))
                {
                    if (party.LeaderHero.Culture.StringId == TORConstants.Cultures.SYLVANIA ||
                        party.LeaderHero.Culture.StringId == TORConstants.Cultures.MOUSILLON)
                    {
                        if (party.LimitedPartySize > party.MemberRoster.TotalManCount + UndeadCountTowns)
                        {
                            var count = party.LeaderHero.HasAttribute("BloodDragon") ? UndeadCountVillages : UndeadCountTowns;
                            party.MemberRoster.AddToCounts(_skeleton, 0, false, count);

                            if (party.ActualClan.StringId.Contains("necrarch"))
                            {
                                party.MemberRoster.AddToCounts(_wraith, 3, false, 0);

                                party.Party.MemberRoster.AddXpToTroop(100, _skeleton);
                                party.Party.MemberRoster.AddXpToTroop(100, _wraith);

                            }

                        }
                        return;
                    }

                    if (party.LeaderHero.Culture.StringId == TORConstants.Cultures.CHAOS)
                    {
                        party.MemberRoster.AddToCounts(_raider, 15);
                    }
                }
            }
        }
        

        private void Initialize(CampaignGameStarter obj)
        {
            _skeleton = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_skeleton");
            _raider = MBObjectManager.Instance.GetObject<CharacterObject>("tor_chaos_norscan_raider");
            _wraith = MBObjectManager.Instance.GetObject<CharacterObject>("tor_vc_cairn_wraith");
        }


        private void AddUndeadToPartyOnEnteringSettlement(MobileParty party, Settlement settlement, Hero hero)
        {
            if (party == null || settlement == null || hero == null || !hero.IsNecromancer() || hero.CharacterObject.IsPlayerCharacter || settlement.IsHideout) return;
            if (party.MemberRoster.TotalManCount < party.Party.PartySizeLimit)
            {
                if (_skeleton != null)
                {
                    var number = settlement.IsVillage ? UndeadCountVillages : UndeadCountTowns;
                    party.MemberRoster.AddToCounts(_skeleton, Math.Min(number, party.Party.PartySizeLimit - party.MemberRoster.TotalManCount));
                }
            }
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