using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.Reinforcements
{
    public class GreenKnightAgentOrigin : IAgentOriginBase
    {
        private readonly GreenKnightBehavior _behavior;
        private readonly CustomBattleCombatant _battleCombatant;
        private readonly UniqueTroopDescriptor _descriptor;
        private readonly int _rank;
        private readonly bool _hasThrownWeapon;
        private readonly bool _hasHeavyArmor;
        private readonly bool _hasShield;
        private readonly bool _hasSpear;
        private Banner _banner;
        private bool _isRemoved;

        public MapEvent MapEvent { get; }
        public CharacterObject Troop { get; }
        public bool IsGreenKnight { get; }
        public IBattleCombatant BattleCombatant => _battleCombatant;
        public Banner Banner => _banner;
        public int UniqueSeed => _descriptor.UniqueSeed;
        public int Seed => Troop.GetDefaultFaceSeed(_rank);
        public bool IsUnderPlayersCommand => false;
        public bool IsInSameArmyAsPlayer => false;
        public uint FactionColor => _battleCombatant.BasicCulture.Color;
        public uint FactionColor2 => _battleCombatant.BasicCulture.Color2;
        BasicCharacterObject IAgentOriginBase.Troop => Troop;
        bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;
        bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;
        bool IAgentOriginBase.HasShield => _hasShield;
        bool IAgentOriginBase.HasSpear => _hasSpear;

        public GreenKnightAgentOrigin(GreenKnightBehavior behavior, MapEvent mapEvent, CustomBattleCombatant battleCombatant, CharacterObject troop, bool isGreenKnight, int rank)
        {
            _behavior = behavior;
            _battleCombatant = battleCombatant;
            _descriptor = new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed);
            _rank = rank;
            _banner = battleCombatant.Banner;
            MapEvent = mapEvent;
            Troop = troop;
            IsGreenKnight = isGreenKnight;
            AgentOriginUtilities.GetDefaultTroopTraits(Troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
        }

        public void SetWounded()
        {
            if (_isRemoved)
                return;

            _behavior.RemoveVirtualTroop(MapEvent, IsGreenKnight);
            _isRemoved = true;
        }

        public void SetKilled()
        {
            if (_isRemoved)
                return;

            _behavior.RemoveVirtualTroop(MapEvent, IsGreenKnight);
            _isRemoved = true;
        }

        public void SetRouted(bool isOrderRetreat)
        {
            if (_isRemoved)
                return;

            if (!isOrderRetreat)
                _behavior.RemoveVirtualTroop(MapEvent, IsGreenKnight);

            _isRemoved = true;
        }

        public void OnAgentRemoved(float agentHealth)
        {
        }

        public void OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
        {
        }

        public void SetBanner(Banner banner)
        {
            _banner = banner;
        }

        public TroopTraitsMask GetTraitsMask()
        {
            return AgentOriginUtilities.GetDefaultTraitsMask(this);
        }
    }
}
