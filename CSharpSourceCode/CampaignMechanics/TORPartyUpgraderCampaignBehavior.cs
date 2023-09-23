using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.CampaignMechanics
{
    public class TORPartyUpgraderCampaignBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, new Action<MapEvent>(MapEventEnded));
            CampaignEvents.DailyTickPartyEvent.AddNonSerializedListener(this, new Action<MobileParty>(DailyTickParty));
        }

        private void MapEventEnded(MapEvent mapEvent)
        {
            foreach (PartyBase party in mapEvent.InvolvedParties)
            {
                UpgradeReadyTroops(party);
            }
        }

        private void DailyTickParty(MobileParty party)
        {
            if (party.MapEvent == null)
            {
                UpgradeReadyTroops(party.Party);
            }
        }

        public void UpgradeReadyTroops(PartyBase party)
        {
            if (party != PartyBase.MainParty && party.IsActive)
            {
                TroopRoster memberRoster = party.MemberRoster;
                PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
                for (int i = 0; i < memberRoster.Count; i++)
                {
                    TroopRosterElement elementCopyAtIndex = memberRoster.GetElementCopyAtIndex(i);
                    if (partyTroopUpgradeModel.IsTroopUpgradeable(party, elementCopyAtIndex.Character))
                    {
                        List<TORTroopUpgradeArgs> possibleUpgradeTargets = GetPossibleUpgradeTargets(party, elementCopyAtIndex);
                        if (possibleUpgradeTargets.Count > 0)
                        {
                            TORTroopUpgradeArgs upgradeArgs = SelectPossibleUpgrade(possibleUpgradeTargets);
                            UpgradeTroop(party, i, upgradeArgs);
                        }
                    }
                }
            }
        }

        private List<TORTroopUpgradeArgs> GetPossibleUpgradeTargets(PartyBase party, TroopRosterElement rosterElement)
        {
            PartyWageModel partyWageModel = Campaign.Current.Models.PartyWageModel;
            List<TORTroopUpgradeArgs> list = new List<TORTroopUpgradeArgs>();
            CharacterObject character = rosterElement.Character;
            int num = rosterElement.Number - rosterElement.WoundedNumber;
            if (num > 0)
            {
                PartyTroopUpgradeModel partyTroopUpgradeModel = Campaign.Current.Models.PartyTroopUpgradeModel;
                for(int i = 0; i < character.UpgradeTargets.Length; i++)
                {
                    CharacterObject upgradeTargetObject = character.UpgradeTargets[i];
                    int upgradeXpCost = character.GetUpgradeXpCost(party, i);
                    if(upgradeXpCost <= 0 || num * rosterElement.Xp < upgradeXpCost)
                    {
                        bool partyHasEnoughGold = false;
                        int upgradeGoldCost = character.GetUpgradeGoldCost(party, i);
                        if (upgradeGoldCost <= 0 || (party.LeaderHero != null && upgradeGoldCost != 0 && num * upgradeGoldCost <= party.LeaderHero.Gold)) partyHasEnoughGold = true;
                        bool withinPaymentLimit = false;
                        if(upgradeTargetObject.Tier > character.Tier && 
                            party.MobileParty.PaymentLimit > 0 &&
                            party.MobileParty.CanPayMoreWage() && 
                            party.MobileParty.TotalWage + num * (partyWageModel.GetCharacterWage(upgradeTargetObject) - partyWageModel.GetCharacterWage(character)) <= party.MobileParty.PaymentLimit)
                        {
                            withinPaymentLimit = true;
                        }
                        if(partyHasEnoughGold && withinPaymentLimit)
                        {
                            if ((!party.Culture.IsBandit || upgradeTargetObject.Culture.IsBandit) && (character.Occupation != Occupation.Bandit || partyTroopUpgradeModel.CanPartyUpgradeTroopToTarget(party, character, upgradeTargetObject)))
                            {
                                float upgradeChanceForTroopUpgrade = Campaign.Current.Models.PartyTroopUpgradeModel.GetUpgradeChanceForTroopUpgrade(party, character, i);
                                list.Add(new TORTroopUpgradeArgs(character, upgradeTargetObject, num, upgradeGoldCost, upgradeXpCost, upgradeChanceForTroopUpgrade));
                            }
                        }
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

        private void UpgradeTroop(PartyBase party, int rosterIndex, TORTroopUpgradeArgs upgradeArgs)
        {
            TroopRoster memberRoster = party.MemberRoster;
            CharacterObject upgradeTarget = upgradeArgs.UpgradeTarget;
            int possibleUpgradeCount = upgradeArgs.PossibleUpgradeCount;
            int num = upgradeArgs.UpgradeXpCost * possibleUpgradeCount;
            memberRoster.SetElementXp(rosterIndex, memberRoster.GetElementXp(rosterIndex) - num);
            //memberRoster.AddToCounts(upgradeArgs.Target, -possibleUpgradeCount, false, 0, 0, true, -1);
            party.AddMember(upgradeArgs.Target, -possibleUpgradeCount, 0);
            //memberRoster.AddToCounts(upgradeTarget, possibleUpgradeCount, false, 0, 0, true, -1);
            party.AddMember(upgradeArgs.UpgradeTarget, possibleUpgradeCount, 0);
            if (party.Owner != null && party.Owner.Clan == Clan.PlayerClan && upgradeTarget.UpgradeRequiresItemFromCategory != null)
            {
                int num2 = possibleUpgradeCount;
                foreach (ItemRosterElement itemRosterElement in party.ItemRoster)
                {
                    if (itemRosterElement.EquipmentElement.Item.ItemCategory == upgradeTarget.UpgradeRequiresItemFromCategory && itemRosterElement.EquipmentElement.ItemModifier == null)
                    {
                        int num3 = MathF.Min(num2, itemRosterElement.Amount);
                        party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement.Item, -num3);
                        num2 -= num3;
                        if (num2 == 0)
                        {
                            break;
                        }
                    }
                }
            }
            if (possibleUpgradeCount > 0)
            {
                ApplyEffects(party, upgradeArgs);
            }
        }

        private void ApplyEffects(PartyBase party, TORTroopUpgradeArgs upgradeArgs)
        {
            if (party.Owner != null && party.Owner.IsAlive)
            {
                SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.Target, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
                GiveGoldAction.ApplyBetweenCharacters(party.Owner, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, true);
                return;
            }
            if (party.LeaderHero != null && party.LeaderHero.IsAlive)
            {
                SkillLevelingManager.OnUpgradeTroops(party, upgradeArgs.Target, upgradeArgs.UpgradeTarget, upgradeArgs.PossibleUpgradeCount);
                GiveGoldAction.ApplyBetweenCharacters(party.LeaderHero, null, upgradeArgs.UpgradeGoldCost * upgradeArgs.PossibleUpgradeCount, true);
            }
        }

        private readonly struct TORTroopUpgradeArgs
        {
            public TORTroopUpgradeArgs(CharacterObject target, CharacterObject upgradeTarget, int possibleUpgradeCount, int upgradeGoldCost, int upgradeXpCost, float upgradeChance)
            {
                Target = target;
                UpgradeTarget = upgradeTarget;
                PossibleUpgradeCount = possibleUpgradeCount;
                UpgradeGoldCost = upgradeGoldCost;
                UpgradeXpCost = upgradeXpCost;
                UpgradeChance = upgradeChance;
            }

            public readonly CharacterObject Target;

            public readonly CharacterObject UpgradeTarget;

            public readonly int PossibleUpgradeCount;

            public readonly int UpgradeGoldCost;

            public readonly int UpgradeXpCost;

            public readonly float UpgradeChance;
        }

        public override void SyncData(IDataStore dataStore) { }
    }
}
