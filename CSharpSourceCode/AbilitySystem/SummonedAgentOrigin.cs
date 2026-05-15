using System;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.AbilitySystem
{
    public class SummonedAgentOrigin : IAgentOriginBase
    {
        private int _rank = -1;
        private UniqueTroopDescriptor _uniqueTroopDescriptor;
        public bool IsUnderPlayersCommand { get; private set; }

        public uint FactionColor { get; private set; }

        public uint FactionColor2 { get; private set; }

        public IBattleCombatant BattleCombatant { get; private set; }

        public int UniqueSeed => _uniqueTroopDescriptor.UniqueSeed;

        public int Seed => Troop.GetDefaultFaceSeed(_rank);

        public Banner Banner { get; private set; }

        public BasicCharacterObject Troop { get; private set; }

        public PartyBase OwnerParty { get; private set; }

        public bool HasThrownWeapon => Troop.FirstBattleEquipment.HasWeaponOfClass(WeaponClass.ThrowingKnife) || Troop.FirstBattleEquipment.HasWeaponOfClass(WeaponClass.ThrowingAxe);

        public bool HasHeavyArmor => Troop.FirstBattleEquipment.GetArmArmorSum() >= 24;

        public bool HasShield => Troop.FirstBattleEquipment.HasWeaponOfClass(WeaponClass.LargeShield) || Troop.FirstBattleEquipment.HasWeaponOfClass(WeaponClass.SmallShield);

        public bool HasSpear => Troop.FirstBattleEquipment.HasWeaponOfClass(WeaponClass.OneHandedPolearm);

        public SummonedAgentOrigin(Agent summoner, BasicCharacterObject summonedTroop)
        {
            Troop = summonedTroop;
            IsUnderPlayersCommand = summoner.Team.Leader == Agent.Main;
            FactionColor = summoner.Origin.FactionColor;
            FactionColor2 = summoner.Origin.FactionColor2;
            _rank = MBRandom.RandomInt(10000);
            _uniqueTroopDescriptor = new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed);
            Banner = summoner.Team.Banner;
            OwnerParty = summoner.Team.Leader?.Origin.BattleCombatant as PartyBase;
            var manager = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            BattleCombatant = manager.GetSummoningCombatant(summoner.Team);
        }

        public void OnAgentRemoved(float agentHealth) { }

        public void OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon) { }

        public void SetBanner(Banner banner) => Banner = banner;

        public void SetKilled() { }

        public void SetWounded() { }

        public void SetRouted(bool isOrderRetreat) { }

        public TroopTraitsMask GetTraitsMask()
        {
            TroopTraitsMask troopTraitsMask = TroopTraitsMask.None;
            if (Troop.IsMounted)
            {
                troopTraitsMask |= TroopTraitsMask.Mount;
            }
            if (Troop.IsRanged)
            {
                troopTraitsMask |= TroopTraitsMask.Ranged;
            }
            else
            {
                troopTraitsMask |= TroopTraitsMask.Melee;
            }
            if (HasShield)
            {
                troopTraitsMask |= TroopTraitsMask.Shield;
            }
            if (HasSpear)
            {
                troopTraitsMask |= TroopTraitsMask.Spear;
            }
            if (HasThrownWeapon)
            {
                troopTraitsMask |= TroopTraitsMask.Thrown;
            }
            if (HasHeavyArmor)
            {
                troopTraitsMask |= TroopTraitsMask.Armor;
            }
            return troopTraitsMask;
        }
    }

    public class SummonedCombatant : IBattleCombatant
    {
        public TextObject Name { get; private set; }

        public BattleSideEnum Side { get; private set; }

        //public IBattleCombatant BattleCombatant => ;

        public BasicCultureObject BasicCulture { get; private set; }

        public BasicCharacterObject General { get; private set; }

        public Tuple<uint, uint> PrimaryColorPair { get; private set; }

        public Tuple<uint, uint> AlternativeColorPair { get; private set; }

        public Banner Banner { get; private set; }

        public SummonedCombatant(Team team, BasicCultureObject culture)
        {
            Name = new TextObject("Summoned");
            Side = team.Side;
            BasicCulture = culture;
            General = team.GeneralAgent == null ? null : team.GeneralAgent.Character;
            PrimaryColorPair = new Tuple<uint, uint>(team.Color, team.Color2);
            AlternativeColorPair = new Tuple<uint, uint>(team.Color, team.Color2);
            Banner = team.Banner;
        }

        public int GetTacticsSkillAmount() => 30;
    }
}