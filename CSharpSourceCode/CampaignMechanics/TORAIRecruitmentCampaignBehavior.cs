using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;
using TOR_Core.Extensions;
using TOR_Core.Utilities;

namespace TOR_Core.CampaignMechanics
{
    public class TORAIRecruitmentCampaignBehavior : CampaignBehaviorBase
    {
        private CharacterObject _skeleton;
        private CharacterObject _dryad;
        private CharacterObject _treeman;
        private CharacterObject _raider;
        private CharacterObject _wraith;
        private CharacterObject _artilleryCrew;

        private const int UndeadCountVillages = 5;
        private const int UndeadCountTowns = 20;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Initialize);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, AddArtilleryCrewmanToPartyOnEnteringSettlement);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, AddUndeadToPartyOnEnteringSettlement);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, AddDryadsToPartyOnEnteringSettlement);
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, TORRecruitmentBehavior);
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, DailyTickEvents);
        }

        private void AddArtilleryCrewmanToPartyOnEnteringSettlement(MobileParty party, Settlement settlement, Hero leaderHero)
        {
            int recruitmentNumber = 2;
            if (party != null &&
                party.IsLordParty &&
                !party.IsMainParty &&
                !party.IsDisbanding &&
                party.LeaderHero != null &&
                party.LeaderHero.CanPlaceArtillery() &&
                party.LeaderHero.Culture.StringId == TORConstants.Cultures.EMPIRE &&
                !party.Party.IsStarving &&
                party.MapFaction.IsKingdomFaction &&
                party.Party.NumberOfAllMembers + recruitmentNumber < party.Party.PartySizeLimit &&
                !party.IsWageLimitExceeded() &&
                party.LeaderHero.Gold > HeroHelper.StartRecruitingMoneyLimit(party.LeaderHero) &&
                (party.LeaderHero == party.LeaderHero.Clan.Leader || party.LeaderHero.Clan.Gold > HeroHelper.StartRecruitingMoneyLimitForClanLeader(party.LeaderHero)))
            {
                if (_artilleryCrew == null) return;
                var crewInPartySum = party.MemberRoster.GetTroopRoster().Where(x => x.Character.GetAttributes().Contains("ArtilleryCrew")).Sum(x => x.Number);
                if (crewInPartySum > 10) { return;} //2 crew per gun, max 3 guns for certain ai parties
                var cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(_artilleryCrew, party.LeaderHero).ResultNumber * recruitmentNumber;
                if (party.LeaderHero.Gold > cost)
                {
                    GiveGoldAction.ApplyBetweenCharacters(party.LeaderHero, null, (int)cost);
                    party.AddElementToMemberRoster(_artilleryCrew, recruitmentNumber);
                    CampaignEventDispatcher.Instance.OnTroopRecruited(party.LeaderHero, settlement, settlement.Notables.FirstOrDefault(), _artilleryCrew, recruitmentNumber);
                }
            }
        }

        private void DailyTickEvents(MobileParty party)
        {
            if (party.IsLordParty && !party.IsMainParty && party.LeaderHero != null )
            {
                var clan = party.LeaderHero.Clan;
                if (clan != null && clan.Kingdom != null && clan.Kingdom.IsCastleFaction() && !clan.Kingdom.Settlements.AnyQ(x => x.IsTown))
                {
                    if (party.LeaderHero.Culture.StringId == TORConstants.Cultures.SYLVANIA ||
                        party.LeaderHero.Culture.StringId == TORConstants.Cultures.MOUSILLON)
                    {
                        if (party.ActualClan.Kingdom.Settlements.AnyQ(x => x.IsTown))
                        {
                            return;
                        }
                        
                        if (party.Party.PartySizeLimit > party.MemberRoster.TotalManCount + UndeadCountTowns)
                        {
                            var count = party.LeaderHero.HasAttribute("BloodDragon") ? UndeadCountVillages : UndeadCountTowns;
                            party.MemberRoster.AddToCounts(_skeleton, count, false, 0);

                            if (party.ActualClan.StringId.Contains("necrarch"))
                            {
                                party.MemberRoster.AddToCounts(_wraith, 3, false, 0);

                                party.Party.MemberRoster.AddXpToTroop(_skeleton, 100);
                                party.Party.MemberRoster.AddXpToTroop(_wraith, 100);

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
            _dryad = MBObjectManager.Instance.GetObject<CharacterObject>("tor_we_dryad");
            _treeman = MBObjectManager.Instance.GetObject<CharacterObject>("tor_we_treeman");
            _artilleryCrew = MBObjectManager.Instance.GetObject<CharacterObject>("tor_empire_veteran_artillery_crew");
        }

        private void AddDryadsToPartyOnEnteringSettlement(MobileParty party, Settlement settlement, Hero leaderHero)
        {
            if (party == null || settlement == null || leaderHero == null || !leaderHero.IsSpellCaster() || leaderHero.Culture.StringId != TORConstants.Cultures.ASRAI|| settlement.IsHideout || party.IsMainParty) return;
            if (!settlement.StringId.Contains("AL")) return;

            if (party.MemberRoster.TotalManCount < party.Party.PartySizeLimit)
            {
                if (_treeman !=null && party.MemberRoster.GetTroopCount(_treeman) is int count && count < 3)
                { 
                    if (MBRandom.RandomFloat < 0.05f)
                    party.MemberRoster.AddToCounts(_treeman, 1);
                    return;
                }

                if (_dryad == null || party.MemberRoster.GetTroopCount(_dryad)>=75) return;
                
                var number = settlement.IsVillage ? UndeadCountVillages : UndeadCountTowns;
                party.MemberRoster.AddToCounts(_dryad, Math.Min(number, party.Party.PartySizeLimit - party.MemberRoster.TotalManCount));
            }
        }
        
        private void AddUndeadToPartyOnEnteringSettlement(MobileParty party, Settlement settlement, Hero leaderHero)
        {
            if (party == null || settlement == null || leaderHero == null || !leaderHero.IsNecromancer() || settlement.IsHideout || party.IsMainParty) return;
            if (party.MemberRoster.TotalManCount < party.Party.PartySizeLimit)
            {
                if (_skeleton != null)
                {
                    var number = settlement.IsVillage ? UndeadCountVillages : UndeadCountTowns;
                    party.MemberRoster.AddToCounts(_skeleton, Math.Min(number, party.Party.PartySizeLimit - party.MemberRoster.TotalManCount));
                }
            }
        }

        public override void SyncData(IDataStore dataStore) { }

        private void TORRecruitmentBehavior(Hero recruiter, Settlement settlement, Hero recruitmentSource, CharacterObject troop, int amount)
        {
            if (recruiter == null) return;
            if (recruiter == Hero.MainHero) return;

            var roster = recruiter.PartyBelongedTo.MemberRoster;
            roster.ValidateTroopListCache(); //Sly : the roster passed in has its properties changed by the Add and Remove further below, but the underlying data cache for the elements is unchanged. Performing the validation forces the cached values to be updated so that the TroopCount below is not counting a stale copy and avoids the issue of a noble recruiting multiple troops simultaneosuly and the subsequent troop swaps using stale values
            //This is generally not a problem here because this class receives the event before AssimilationCampaignBehavior
            var verifiedAmount = recruiter?.PartyBelongedTo?.MemberRoster?.GetTroopCount(troop);
            if (verifiedAmount == null || verifiedAmount < 1) return;
            amount = (int)verifiedAmount; //Sly : if multiple behaviours remove troops (which makes use of the roster index), then the roster index may vary and a troop with no valid index causes the troop roster to fetch data[-1], ie. AssimilationCampaignBehavior

            if (recruiter.CharacterObject.IsBloodDragon())
            {
                if (troop.StringId == "tor_vc_vampire_newblood") return;
                int count = 0;
                for (int i = 0; i < amount; i++)
                {
                    var random = MBRandom.RandomFloat;
                    if ((!troop.IsBasicTroop && random > 0.25f) || random > 0.75f)
                        count ++;
                }
                
                var bloodKnightInitate = MBObjectManager.Instance.GetObject<CharacterObject>("tor_bd_blooddragon_initiate");
                if (count > 0 && bloodKnightInitate != null)
                {
                    recruiter.PartyBelongedTo.AddElementToMemberRoster(bloodKnightInitate, count);
                    recruiter.PartyBelongedTo.AddElementToMemberRoster(troop, -count);
                }
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