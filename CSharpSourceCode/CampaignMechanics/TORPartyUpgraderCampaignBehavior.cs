using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.LinQuick;
using TaleWorlds.ObjectSystem;

namespace TOR_Core.CampaignMechanics
{
    public class TORPartyUpgraderCampaignBehavior : CampaignBehaviorBase
    {
        private float _offTemplateRatio = 0.05f;

        ///<summary>Only troops above this level will have restricted upgrades</summary>
        /// <remarks>
        /// Tier is equivalent to : Ceiling(level/5 - 1) : level 25 = t4; 26 = t5
        /// because an upgrade target must be at least 1 tier above their source and because this is used in a context where a target exists, we can safely ignore any lower bound and evaluate the level directly
        /// </remarks>
        private int _cutoffLevel = 25;

        //culture id, total troops in template, list of troops above _cutoffLevel ordered by descending level
        private static Dictionary<string, Tuple<int, List<PartyTemplateStack>>> _cultureTemplateData = [];


        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(MapEventEnded));
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(DailyTickParty));
            CampaignEvents.WeeklyTickEvent.AddNonSerializedListener(this, new Action(WeeklyGarrisonUpgrade));
            CampaignEvents.OnAfterSessionLaunchedEvent.AddNonSerializedListener(this, CalculateTemplateDataOnLoad);
        }



        private void CalculateTemplateDataOnLoad(CampaignGameStarter starter)
        {
            //bandit cultures don't usually have a culture template, and I'm not sure if it would be used if they did
            var cultures = MBObjectManager.Instance.GetObjectTypeList<BasicCultureObject>().WhereQ(x => !x.IsBandit);
            foreach (var culture in cultures)
            {
                if (_cultureTemplateData.TryGetValue(culture.StringId, out _)) continue;

                var cultureObject = culture as CultureObject;
                var cultureTemplateStacks = cultureObject.DefaultPartyTemplate?.Stacks;

                if (cultureTemplateStacks == null) continue; //neutral culture

                var highTierCultureTemplateStacks = cultureTemplateStacks.WhereQ(tier => tier.Character.Level > _cutoffLevel).OrderByDescending(highFirst => highFirst.Character.Level).ToList();
                int totalTroopsInTemplate = cultureTemplateStacks.Sum(x => x.MaxValue);

                _cultureTemplateData.Add(culture.StringId, new Tuple<int, List<PartyTemplateStack>>(totalTroopsInTemplate, highTierCultureTemplateStacks));
            }
        }


        //Sly : garrison recruitment, upgrades, and deposits from lord parties are all in interaction. Eonir suffer greatly from upgrading city troops because the game is blind to their increased wages because it uses an AverageWage that is shared across all cultural template stacks for all MainFaction cultures.
        private void WeeklyGarrisonUpgrade()
        {//Sly : I could put a check in for player fiefs to check the auto-recruit button before performing upgrades, but I lean away from that because there may be players who want to stop recruiting new troops, but still have their current ones upgrade to higher tiers.
            foreach (var fortification in Town.AllFiefs.Where(x => !x.IsUnderSiege))
            {
                if (fortification.GarrisonParty is MobileParty party && party.MapEvent == null)
                {
                    UpgradeReadyTroops(party.Party);//unsure how the game handles garrison wages so i'm unsure if there may be issues due to the garrison being close to/at its limit and therefore prevented from upgrading by UpgradeTroop
                }
            }
        }

        private void MapEventEnded(MapEvent mapEvent)
        {
            foreach (PartyBase party in mapEvent.InvolvedParties)
            {
                if (party.MobileParty == null) continue;//god damn villager militia parties
                if (party.MobileParty == MobileParty.MainParty) { continue; }
                UpgradeReadyTroops(party);
            }
        }

        /// <summary>
        /// Calls upgrades for parties on the daily tick. Only needs to apply to parties that receive daily xp to troops.
        /// </summary>
        /// <remarks>
        /// Doesn't need to check : 
        ///        MainParty : player deals with their own shit. We could introduce a setting to allow the player to set their party to auto-upgrade if they want to follow the culture's template.
        ///        caravans : only the player clan's caravans will have a noble leading them to grant daily xp but we can ignore them all to process less parties as caravans with generally fixed stacks doesn't seem like a large risk of immersion breaking
        ///        militias : no source of daily xp. These are all 2 types of troops and only spawn the upgraded versions if the governor has a perk; therefore, there is nothing to upgrade even though they might have an upgrade target in the char definition.
        ///        bandits : no source
        ///        raiding parties : no source
        ///        quest parties : no notable source unless we start introducing long duration parties led by a noble (nuln engi quest can't be anything more than short duration because it starves out)
        ///        whatever parties are spawned for village raid defenses? are these also raider parties? : no source
        ///        villagers : no source
        ///        are villages militias also militia-typed party components?
        ///        other?
        /// </remarks>
        private void DailyTickParty(MobileParty party)
        {
            if (party.IsLordParty && party.MapEvent == null && party != MobileParty.MainParty)
            {
                UpgradeReadyTroops(party.Party);
            }
        }

        public void UpgradeReadyTroops(PartyBase party)
        {
            if (party.IsActive)
            {
                //more bodies are generally better than 1-2 high tier ones
                var list = GetTroopUpgradeList(party).OrderBy(troopArg => troopArg.UpgradeTarget.Level);
                PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
                foreach (var troopArgs in list)
                {
                    UpgradeTroop(partyWageModel, party, party.MemberRoster.FindIndexOfTroop(troopArgs.UpgradeSource), troopArgs);
                }
            }
        }

        /// <remarks>
        /// The random target selection is bothersome conceptually because it doesn't account for how battles have target ratios that don't account for culture which could lead to an overabundance of certain troop types initially, then reinforcement waves that are filled with whatever troop type is equally present via upgrades but is disfavoured by the ratios and therefore is all that's left.
        /// Additionally, garrisons should generally favour a higher ratio of ranged troops due to siege mechanics, but would it be better to have them stocked by garrisons that represent the "ideal" composition?
        /// </remarks>>
        private List<TORTroopUpgradeArgs> GetTroopUpgradeList(PartyBase party)
        {
            List<TORTroopUpgradeArgs> upgradesToPerform = [];
            PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;

            var memberRoster = party.MemberRoster;
            //descending order to check the highest tier targets first to count the number of missing troops among all potential upgrade paths from a troop
            IEnumerable<TroopRosterElement> upgradeableRoster = memberRoster.GetTroopRoster().Where(t => !t.Character.IsHero && t.Character.UpgradeTargets.Length != 0).OrderByDescending(x => x.Character.Level);
            
            //exclude heroes from the count so the composition is only based on troops
            float troopCount = memberRoster.TotalRegulars;
            var defaultTemplate = (party.MobileParty?.ActualClan != null) ? party.MobileParty.ActualClan.DefaultPartyTemplate : party.Culture?.DefaultPartyTemplate;//bandit and mercenary clan parties use a template taken from their Clan object, and if a clan has no party template then it falls back to the cultural template. The culture fallback exists here because RaidingPartyComponents don't have a clan atm, they are only assigned to a hero.

            var cultureTemplateStacks = defaultTemplate?.Stacks;
            Dictionary<CharacterObject, float> missingTroopsAtEachTier = []; //these could probably be stored as ints and rounded up at each step as the precision doesn't really matter; it will give slightly stronger parties.
            

            if (party?.Culture != null && _cultureTemplateData.TryGetValue(party.Culture.StringId, out var cultureData) && cultureData != null)
            {
                float totalTroopsInTemplate = cultureData.Item1;
                var highTierCultureTemplateStacks = cultureData.Item2; //only care about the high tier ones
                float partyRatioToTemplate = troopCount / totalTroopsInTemplate;

                //each t5+ troop stores its expected count *and* those of its targets downbranch so that any troop can be evaluated even if the party contains none of an intermediary upgrade
                foreach (var templateTroop in highTierCultureTemplateStacks)
                {
                    //expected number based on template
                    float countOfTroopAndTargets = templateTroop.MaxValue * partyRatioToTemplate;
                    //minus currently present in party
                    if (memberRoster.GetTroopCount(templateTroop.Character) > 0) { countOfTroopAndTargets -= memberRoster.GetTroopCount(templateTroop.Character); }
                    //add the values from their upgrade targets which will recursively include every target downtree as this iteration is highest level first
                    foreach (var target in templateTroop.Character.UpgradeTargets)
                    {
                        if (missingTroopsAtEachTier.TryGetValue(target, out float count))
                        {
                            countOfTroopAndTargets += count;
                        }
                    }
                    missingTroopsAtEachTier.Add(templateTroop.Character, countOfTroopAndTargets);
                }
            }

            foreach (var rosterElement in upgradeableRoster)
            {
                var upgradingCharacter = rosterElement.Character;
                var rosterCount = rosterElement.Number;
                var healthyCount = rosterCount - rosterElement.WoundedNumber;
                IEnumerable<CharacterObject> potentialTargets = upgradingCharacter.UpgradeTargets.Where(possibleTarget =>
                        BanditPerkAndItemCheck(party, upgradingCharacter, possibleTarget, partyTroopUpgradeModel)
                        && (healthyCount > 0));//this healthy condition will have an unintended consequence for non-lord parties who will only upgrade on map event end when they have the most wounded troops
                if (potentialTargets.Any() == false) continue;
                //if below a cutoff tier, troops can upgrade without restriction
                if (!potentialTargets.Where(target => target.Level > _cutoffLevel).Any())
                {
                    var upgradeTarget = potentialTargets.GetRandomElementInefficiently();
                    int xpToUpgrade = partyTroopUpgradeModel.GetXpCostForUpgrade(party, upgradingCharacter, upgradeTarget);
                    int troopXp = rosterElement.Xp;
                    if (xpToUpgrade <= troopXp)
                    {
                        int availableTroopUpgrade = troopXp / xpToUpgrade;
                        availableTroopUpgrade = Math.Min(availableTroopUpgrade, healthyCount);
                        upgradesToPerform.Add(new TORTroopUpgradeArgs(upgradingCharacter, upgradeTarget, availableTroopUpgrade, (int)partyTroopUpgradeModel.GetGoldCostForUpgrade(party, upgradingCharacter, upgradeTarget).ResultNumber, xpToUpgrade, 1));
                    }
                }
                else
                {
                    //account for expected troop counts based on cultural composition
                    List<CharacterObject> targetMissingTroops = [];
                    foreach (var target in potentialTargets)
                    {
                        bool inTemplate = missingTroopsAtEachTier.TryGetValue(target, out float missingCount);//if a high enough tier troop is in the cultural template, it will exist in the dictionary
                        //low tier troops, outside of the cultural template with targets below their ratio, or in the template and missing at least 1  targeted troop are possibilities
                        if (target.Level <= _cutoffLevel || (inTemplate && missingCount >= 1) ||
                            (!inTemplate && ((int)(troopCount * _offTemplateRatio) - memberRoster.GetTroopCount(target)) >= 1))
                        {
                            targetMissingTroops.Add(target);
                        }
                    }
                    if (!targetMissingTroops.Any()) continue;

                    var selectedTarget = targetMissingTroops.GetRandomElementInefficiently();

                    int xpToUpgrade = partyTroopUpgradeModel.GetXpCostForUpgrade(party, upgradingCharacter, selectedTarget);
                    int troopXp = rosterElement.Xp;
                    if (xpToUpgrade <= troopXp)
                    {
                        bool inTemplate = missingTroopsAtEachTier.TryGetValue(selectedTarget, out float missingCount);
                        if (selectedTarget.Level > _cutoffLevel && !inTemplate)
                        {
                            missingCount = (troopCount * _offTemplateRatio) - memberRoster.GetTroopCount(selectedTarget);
                        }
                        if (missingCount != 0 && missingCount < 1) continue;//targets below cutoff level will have missingCount == 0 (default float because TryGetValue is false)

                        int availableTroopUpgrade = Math.Min(troopXp / xpToUpgrade, healthyCount);
                        if (selectedTarget.Level > _cutoffLevel) { availableTroopUpgrade = Math.Min(availableTroopUpgrade, (int)missingCount); }
                        if (availableTroopUpgrade < 1) { continue; }
                        if (rosterElement.Number > 30) { availableTroopUpgrade = Math.Min(availableTroopUpgrade, (int)(rosterCount * 0.25)); }//weird effect at the boundary but i don't want to make it complicated
                        upgradesToPerform.Add(new TORTroopUpgradeArgs(upgradingCharacter, selectedTarget, availableTroopUpgrade, (int)partyTroopUpgradeModel.GetGoldCostForUpgrade(party, upgradingCharacter, selectedTarget).ResultNumber, xpToUpgrade, 1));
                    }
                }
            }
            return upgradesToPerform;
        }

        private bool BanditPerkAndItemCheck(PartyBase party, CharacterObject troopCharacter, CharacterObject upgradeTargetCharacter, PartyTroopUpgradeModel partyTroopUpgradeModel)
        {
            return (!party.Culture.IsBandit || upgradeTargetCharacter.Culture.IsBandit) && (troopCharacter.Occupation != Occupation.Bandit || partyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(party, troopCharacter, upgradeTargetCharacter));
        }

        private List<TORTroopUpgradeArgs> GetPossibleUpgradeTargets(PartyBase party, TroopRosterElement rosterElement)
        {
            PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
            List<TORTroopUpgradeArgs> list = [];
            CharacterObject troopCharacter = rosterElement.Character;
            int numPossibleTroopsToUpgrade = rosterElement.Number - rosterElement.WoundedNumber;
            if (numPossibleTroopsToUpgrade > 0)
            {
                PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
                for (int i = 0; i < troopCharacter.UpgradeTargets.Length; i++)
                {
                    CharacterObject upgradeTargetCharacter = troopCharacter.UpgradeTargets[i];
                    int upgradeXpCost = troopCharacter.GetUpgradeXpCost(party, i);
                    if (upgradeXpCost > 0) { numPossibleTroopsToUpgrade = MathF.Min(numPossibleTroopsToUpgrade, rosterElement.Xp / upgradeXpCost); }
                    //if(upgradeXpCost <= 0 || numPossibleTroopsToUpgrade * rosterElement.Xp < upgradeXpCost)

                    int upgradeGoldCost = troopCharacter.GetUpgradeGoldCost(party, i);
                    if (upgradeGoldCost > 0 && party.LeaderHero != null && numPossibleTroopsToUpgrade * upgradeGoldCost > party.LeaderHero.Gold)
                    {
                        numPossibleTroopsToUpgrade = party.LeaderHero.Gold / upgradeGoldCost;
                        if (numPossibleTroopsToUpgrade <= 1) { continue; }
                    }

                    //MaxWage for "unlimited" wages is 10k, but what does that matter? why would native call mobParty.HasLimitedWage which would ignore the conditional and allow an ai party to surpass the 10k limit anyways?
                    if (upgradeTargetCharacter.Tier > troopCharacter.Tier &&
                        party.MobileParty.HasLimitedWage() &&
                        !party.MobileParty.IsWageLimitExceeded() &&
                        party.MobileParty.TotalWage + numPossibleTroopsToUpgrade * (partyWageModel.GetCharacterWage(upgradeTargetCharacter) - partyWageModel.GetCharacterWage(troopCharacter)) > party.MobileParty.PaymentLimit)
                    {
                        numPossibleTroopsToUpgrade = (party.MobileParty.PaymentLimit - party.MobileParty.TotalWage) / (partyWageModel.GetCharacterWage(upgradeTargetCharacter) - partyWageModel.GetCharacterWage(troopCharacter));
                        if (numPossibleTroopsToUpgrade <= 1) { continue; }
                    }

                    if ((!party.Culture.IsBandit || upgradeTargetCharacter.Culture.IsBandit) && (troopCharacter.Occupation != Occupation.Bandit || partyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(party, troopCharacter, upgradeTargetCharacter)))
                    {
                        float upgradeChanceForTroopUpgrade = Campaign.Current.Models.PartyTroopUpgradeModel.GetUpgradeChanceForTroopUpgrade(party, troopCharacter, i);
                        list.Add(new TORTroopUpgradeArgs(troopCharacter, upgradeTargetCharacter, numPossibleTroopsToUpgrade, upgradeGoldCost, upgradeXpCost, upgradeChanceForTroopUpgrade));
                    }
                }
            }
            return list;
        }

        private TORTroopUpgradeArgs SelectPossibleUpgrade(List<TORTroopUpgradeArgs> possibleUpgrades)
        {
            TORTroopUpgradeArgs result = possibleUpgrades[0];
            if (possibleUpgrades.Count > 1)
            {
                float num = 0f;
                foreach (TORTroopUpgradeArgs troopUpgradeArgs in possibleUpgrades)
                {
                    num += troopUpgradeArgs.UpgradeChance;
                }
                float num2 = num * MBRandom.RandomFloat;
                foreach (TORTroopUpgradeArgs troopUpgradeArgs2 in possibleUpgrades)
                {
                    num2 -= troopUpgradeArgs2.UpgradeChance;
                    if (num2 <= 0f)
                    {
                        result = troopUpgradeArgs2;
                        break;
                    }
                }
            }
            return result;
        }

        private void UpgradeTroop(PartyWageModel partyWageModel, PartyBase party, int rosterIndex, TORTroopUpgradeArgs upgradeArgs)
        {
            TroopRoster memberRoster = party.MemberRoster;
            CharacterObject upgradeSource = upgradeArgs.UpgradeSource;
            CharacterObject upgradeTarget = upgradeArgs.UpgradeTarget;
            int possibleUpgradeCount = upgradeArgs.PossibleUpgradeCount;
            int totalWage = party.MobileParty.TotalWage;
            //payable wage check; this assumes that cheat/normal income is enough to cover the upgrade costs which may not be true for eonir due to massive recruitment costs
            //cheat gold and looted gold for the ai has been increased but a future issue may arrive when any sort of economy balancing is pursued
            if (upgradeTarget.Tier > upgradeSource.Tier &&
                !party.MobileParty.IsWageLimitExceeded() &&
                totalWage + possibleUpgradeCount * (partyWageModel.GetCharacterWage(upgradeTarget) - partyWageModel.GetCharacterWage(upgradeSource)) > party.MobileParty.PaymentLimit)
            {
                possibleUpgradeCount = (party.MobileParty.PaymentLimit - totalWage) / (partyWageModel.GetCharacterWage(upgradeTarget) - partyWageModel.GetCharacterWage(upgradeSource));
                if (possibleUpgradeCount < 1) { return; }
            }

            int xpToUpgradeCount = upgradeArgs.UpgradeXpCost * possibleUpgradeCount;
            if (xpToUpgradeCount > 0)
            {
                memberRoster.SetElementXp(rosterIndex, memberRoster.GetElementXp(rosterIndex) - xpToUpgradeCount);
                party.AddMember(upgradeArgs.UpgradeSource, -possibleUpgradeCount, 0);
                party.AddMember(upgradeArgs.UpgradeTarget, possibleUpgradeCount, 0);

                ApplyEffects(party, upgradeArgs);
            }
        }

        private void ApplyEffects(PartyBase party, TORTroopUpgradeArgs upgradeArgs)
        {
            //testing paying gold costs now that I fixed the upgrade costs and check for payable wages
            if (party.Owner != null && party.Owner.IsAlive)
            {
                SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.UpgradeSource, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
                GiveGoldAction.ApplyBetweenCharacters(party.Owner, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, true);
                return;
            }
            if (party.LeaderHero != null && party.LeaderHero.IsAlive)
            {
                SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.UpgradeSource, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
                GiveGoldAction.ApplyBetweenCharacters(party.LeaderHero, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, true);
            }
        }

        private readonly struct TORTroopUpgradeArgs(CharacterObject upgradeSource, CharacterObject upgradeTarget, int possibleUpgradeCount, int upgradeGoldCost, int upgradeXpCost, float upgradeChance)
        {
            public readonly CharacterObject UpgradeSource = upgradeSource;
            public readonly CharacterObject UpgradeTarget = upgradeTarget;
            public readonly int PossibleUpgradeCount = possibleUpgradeCount;
            public readonly int UpgradeGoldCost = upgradeGoldCost; //cost for 1 troop
            public readonly int UpgradeXpCost = upgradeXpCost; //cost for 1 troop
            public readonly float UpgradeChance = upgradeChance; //currently ignored because I put the target selection before the struct is built for a given troop
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}