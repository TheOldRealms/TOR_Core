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
        private CharacterObject _dwarfArtilleryCrew;
        private CharacterObject _troll;
        private CharacterObject _slayer;

        private const int UndeadCountVillages = 5;
        private const int UndeadCountTowns = 20;
        private const int MaxTrollsPerParty = 20;
        private const int MaxSlayersPerParty = 40;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, Initialize);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, AddArtilleryCrewmanToPartyOnEnteringSettlement);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, AddUndeadToPartyOnEnteringSettlement);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, AddDryadsToPartyOnEnteringSettlement);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, AddTrollsToPartyOnEnteringSettlement);
            CampaignEvents.AfterSettlementEntered.AddNonSerializedListener(this, AddSlayersToPartyOnEnteringSettlement);
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
                (party.LeaderHero.Culture.StringId == TORConstants.Cultures.EMPIRE ||
                 party.LeaderHero.Culture.StringId == TORConstants.Cultures.DAWI) &&
                !party.Party.IsStarving &&
                party.MapFaction.IsKingdomFaction &&
                party.Party.NumberOfAllMembers + recruitmentNumber < party.Party.PartySizeLimit &&
                !party.IsWageLimitExceeded() &&
                party.LeaderHero.Gold > HeroHelper.StartRecruitingMoneyLimit(party.LeaderHero) &&
                (party.LeaderHero == party.LeaderHero.Clan.Leader || party.LeaderHero.Clan.Gold > HeroHelper.StartRecruitingMoneyLimitForClanLeader(party.LeaderHero)))
            {
                // Determine which crew type to recruit based on culture
                CharacterObject crewToRecruit = party.LeaderHero.Culture.StringId == TORConstants.Cultures.EMPIRE
                    ? _artilleryCrew
                    : _dwarfArtilleryCrew;

                RecruitArtilleryCrew(party, settlement, crewToRecruit, recruitmentNumber);
            }
        }

        private void RecruitArtilleryCrew(MobileParty party, Settlement settlement, CharacterObject artilleryCrew, int recruitmentNumber)
        {
            if (artilleryCrew == null) return;

            var crewInPartySum = party.MemberRoster.GetTroopRoster()
                .Where(x => x.Character.GetAttributes().Contains("ArtilleryCrew"))
                .Sum(x => x.Number);

            if (crewInPartySum > 10) { return; } //2 crew per gun, max 3 guns for certain ai parties

            var cost = Campaign.Current.Models.PartyWageModel.GetTroopRecruitmentCost(artilleryCrew, party.LeaderHero).ResultNumber * recruitmentNumber;
            if (party.LeaderHero.Gold > cost)
            {
                GiveGoldAction.ApplyBetweenCharacters(party.LeaderHero, null, (int)cost);
                party.AddElementToMemberRoster(artilleryCrew, recruitmentNumber);
                CampaignEventDispatcher.Instance.OnTroopRecruited(party.LeaderHero, settlement, settlement.Notables.FirstOrDefault(), artilleryCrew, recruitmentNumber);
            }
        }

        private void DailyTickEvents(MobileParty party)
        {
            if (party.IsLordParty && !party.IsMainParty && party.LeaderHero != null)
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

                // SlayerLord Recruitment in Dwarf Towns
                if (party.LeaderHero.HasAttribute("SlayerLord") &&
                    party.CurrentSettlement != null &&
                    party.CurrentSettlement.IsDwarfKarak())
                {
                    ProcessSlayerRecruitment(party);
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
            _dwarfArtilleryCrew = MBObjectManager.Instance.GetObject<CharacterObject>("tor_dw_artillery_crew");
            _troll = MBObjectManager.Instance.GetObject<CharacterObject>("tor_gs_trolls");
            _slayer = MBObjectManager.Instance.GetObject<CharacterObject>("tor_dw_slayer");
        }

        private void AddDryadsToPartyOnEnteringSettlement(MobileParty party, Settlement settlement, Hero leaderHero)
        {
            if (party == null || settlement == null || leaderHero == null || !leaderHero.IsSpellCaster() || leaderHero.Culture.StringId != TORConstants.Cultures.ASRAI || settlement.IsHideout || party.IsMainParty) return;
            if (!settlement.StringId.Contains("AL")) return;

            if (party.MemberRoster.TotalManCount < party.Party.PartySizeLimit)
            {
                if (_treeman != null && party.MemberRoster.GetTroopCount(_treeman) is int count && count < 3)
                {
                    if (MBRandom.RandomFloat < 0.05f)
                        party.MemberRoster.AddToCounts(_treeman, 1);
                    return;
                }

                if (_dryad == null || party.MemberRoster.GetTroopCount(_dryad) >= 75) return;

                var number = settlement.IsVillage ? UndeadCountVillages : UndeadCountTowns;
                party.MemberRoster.AddToCounts(_dryad, Math.Min(number, party.Party.PartySizeLimit - party.MemberRoster.TotalManCount));
            }
        }

        private void AddTrollsToPartyOnEnteringSettlement(MobileParty party, Settlement settlement, Hero leaderHero)
        {
            if (party == null || settlement == null || leaderHero == null) return;
            if (party.IsMainParty || settlement.IsHideout) return;
            if (leaderHero.Culture.StringId != TORConstants.Cultures.GREENSKIN) return;
            if (settlement.Culture?.StringId != TORConstants.Cultures.GREENSKIN) return;
            if (_troll == null) return;

            if (party.MemberRoster.TotalManCount < party.Party.PartySizeLimit)
            {
                int currentTrolls = party.MemberRoster.GetTroopCount(_troll);
                if (currentTrolls >= MaxTrollsPerParty) return;

                // 5% chance to recruit 1 troll, same as treemen
                if (MBRandom.RandomFloat < 0.05f)
                {
                    party.MemberRoster.AddToCounts(_troll, 1);
                }
            }
        }

        private void AddSlayersToPartyOnEnteringSettlement(MobileParty party, Settlement settlement, Hero leaderHero)
        {
            if (party == null || settlement == null || leaderHero == null) return;
            if (party.IsMainParty || settlement.IsHideout) return;
            if (!leaderHero.HasAttribute("SlayerLord")) return;
            if (!settlement.IsDwarfKarak()) return;
            if (_slayer == null) return;

            if (party.MemberRoster.TotalManCount < party.Party.PartySizeLimit)
            {
                int currentSlayers = party.MemberRoster.GetTroopCount(_slayer);
                if (currentSlayers >= MaxSlayersPerParty) return;

                // Recruit 5-10 slayers when entering a Karak
                int slayersToRecruit = MBRandom.RandomInt(5, 11); // RandomInt is exclusive on upper bound, so 5-10
                int maxToAdd = Math.Min(slayersToRecruit, MaxSlayersPerParty - currentSlayers);
                int spaceAvailable = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
                int actualAdd = Math.Min(maxToAdd, spaceAvailable);

                if (actualAdd > 0)
                {
                    party.MemberRoster.AddToCounts(_slayer, actualAdd, false, 0);
                }
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

        private void ProcessSlayerRecruitment(MobileParty party)
        {
            // Check current slayer count
            int currentSlayers = party.MemberRoster.GetTroopCount(_slayer);
            if (currentSlayers >= MaxSlayersPerParty)
            {
                // Only do replacements if already at max
                ReplaceWeakestTroopsWithSlayers(party);
                return;
            }

            // Step 1: Add 3-5 new slayers to the party
            int slayerCount = MBRandom.RandomInt(3, 6); // RandomInt is exclusive on upper bound, so 3-5
            int maxToAdd = Math.Min(slayerCount, MaxSlayersPerParty - currentSlayers);

            // Check if party has room for new troops
            if (party.Party.PartySizeLimit > party.MemberRoster.TotalManCount + maxToAdd)
            {
                party.MemberRoster.AddToCounts(_slayer, maxToAdd, false, 0);
            }
            else if (party.Party.PartySizeLimit > party.MemberRoster.TotalManCount)
            {
                // Add as many as we can fit
                int spaceAvailable = party.Party.PartySizeLimit - party.MemberRoster.TotalManCount;
                int actualAdd = Math.Min(spaceAvailable, maxToAdd);
                party.MemberRoster.AddToCounts(_slayer, actualAdd, false, 0);
            }

            // Step 2: Replace weakest troops with slayers (25% chance per troop)
            ReplaceWeakestTroopsWithSlayers(party);
        }

        private void ReplaceWeakestTroopsWithSlayers(MobileParty party)
        {
            // Get all troops in roster (excluding heroes)
            var roster = party.MemberRoster.GetTroopRoster().ToList();

            // Filter for weakest non-elite troops (tier 1-3, basic troops only)
            var weakestTroops = roster
                .Where(element =>
                    !element.Character.IsHero &&
                    element.Character.Tier >= 1 &&
                    element.Character.Tier <= 3 &&
                    element.Character.IsBasicTroop &&
                    !element.Character.IsEliteTroop())
                .ToList();

            if (weakestTroops.Count == 0) return;

            // Check current slayer count to respect max limit
            int currentSlayers = party.MemberRoster.GetTroopCount(_slayer);
            int totalReplacements = 0;

            // Iterate through each weak troop type
            foreach (var troopElement in weakestTroops)
            {
                int troopsToReplace = 0;

                // Roll 25% chance for each individual troop
                for (int i = 0; i < troopElement.Number; i++)
                {
                    // Stop if we'd exceed max slayers
                    if (currentSlayers + totalReplacements >= MaxSlayersPerParty)
                        break;

                    if (MBRandom.RandomFloat < 0.25f)
                    {
                        troopsToReplace++;
                    }
                }

                if (troopsToReplace > 0)
                {
                    // Remove the weak troops
                    party.AddElementToMemberRoster(troopElement.Character, -troopsToReplace);
                    totalReplacements += troopsToReplace;
                }
            }

            // Add slayers to replace the removed troops
            if (totalReplacements > 0)
            {
                party.MemberRoster.AddToCounts(_slayer, totalReplacements, false, 0);
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
                        count++;
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
                recruiter.PartyBelongedTo.Party.AddMember(replacement, amount);
            }

            if (troop.IsEliteTroop() && recruiter.Culture.StringId == TORConstants.Cultures.BRETONNIA)
            {
                CharacterObject replacement = null;
                if (recruiter.HasAttribute("Bergerac"))
                {
                    replacement = MBObjectManager.Instance.GetObject<CharacterObject>("tor_ror_bergerac_ranger");
                }

                if (recruiter.HasAttribute("PeasantKnight"))
                {
                    replacement = MBObjectManager.Instance.GetObject<CharacterObject>("tor_ror_peasant_squight");
                }

                if (replacement != null)
                {
                    recruiter.PartyBelongedTo.Party.AddMember(replacement, amount);
                    var currentNumber = recruiter.PartyBelongedTo.Party.MemberRoster.GetTroopCount(troop);
                    recruiter.PartyBelongedTo.Party.AddMember(troop, MBMath.ClampInt(-amount, -currentNumber, 0));
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
                        recruiter.PartyBelongedTo.Party.AddMember(chaosKnight, amount);
                    }
                }
                else
                {
                    var raider = MBObjectManager.Instance.GetObject<CharacterObject>("tor_chaos_norscan_raider");
                    recruiter.PartyBelongedTo.Party.AddMember(raider, amount);
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