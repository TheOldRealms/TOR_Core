using System;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TOR_Core.BattleMechanics.Reinforcements
{
    public class ReinforcementPartyAgentOrigin : IAgentOriginBase
    {
        private readonly MapEventSide _mapEventSide;
        private readonly UniqueTroopDescriptor _descriptor;
        private readonly int _rank;
        private readonly bool _hasThrownWeapon;
        private readonly bool _hasHeavyArmor;
        private readonly bool _hasShield;
        private readonly bool _hasSpear;
        private bool _isRemoved;

        public PartyBase Party { get; }
        public CharacterObject Troop { get; }
        public IBattleCombatant BattleCombatant => Party;
        public Banner Banner => Party.LeaderHero?.ClanBanner ?? Party.MapFaction.Banner;
        public int UniqueSeed => _descriptor.UniqueSeed;
        public int Seed => CharacterHelper.GetPartyMemberFaceSeed(Party, Troop, _rank);
        public bool IsUnderPlayersCommand => Troop == Hero.MainHero.CharacterObject || PartyBase.IsPartyUnderPlayerCommand(Party);
        public bool IsInSameArmyAsPlayer
        {
            get
            {
                var mobileParty = Party.MobileParty;
                var army = mobileParty?.Army;
                if (army == null || army != MobileParty.MainParty.Army || (army.LeaderParty != mobileParty && mobileParty.AttachedTo != army.LeaderParty))
                    return false;

                return army.LeaderParty == MobileParty.MainParty || MobileParty.MainParty.AttachedTo == army.LeaderParty;
            }
        }
        public uint FactionColor => Party.MapFaction.Color;
        public uint FactionColor2 => Party.MapFaction.Color2;
        BasicCharacterObject IAgentOriginBase.Troop => Troop;
        bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;
        bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;
        bool IAgentOriginBase.HasShield => _hasShield;
        bool IAgentOriginBase.HasSpear => _hasSpear;

        public ReinforcementPartyAgentOrigin(MapEventSide mapEventSide, PartyBase party, CharacterObject troop, UniqueTroopDescriptor descriptor, int rank)
        {
            _mapEventSide = mapEventSide;
            _descriptor = descriptor;
            _rank = rank;
            Party = party;
            Troop = troop;
            AgentOriginUtilities.GetDefaultTroopTraits(Troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
        }

        public void SetWounded()
        {
            if (_isRemoved)
                return;

            _mapEventSide.OnTroopWounded(_descriptor);
            _isRemoved = true;
        }

        public void SetKilled()
        {
            if (_isRemoved)
                return;

            _mapEventSide.OnTroopKilled(_descriptor);
            if (Troop.IsHero)
                KillCharacterAction.ApplyByBattle(Troop.HeroObject, null);

            _isRemoved = true;
        }

        public void SetRouted(bool isOrderRetreat)
        {
            if (_isRemoved)
                return;

            _mapEventSide.OnTroopRouted(_descriptor, isOrderRetreat);
            _isRemoved = true;
        }

        public void OnAgentRemoved(float agentHealth)
        {
            if (Troop.IsHero)
                Troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
        }

        public void OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
        {
            _mapEventSide.OnTroopScoreHit(_descriptor, (CharacterObject)victim, damage, isFatal, isTeamKill, attackerWeapon, false);
        }

        public void SetBanner(Banner banner)
        {
            throw new NotImplementedException();
        }

        public TroopTraitsMask GetTraitsMask()
        {
            return AgentOriginUtilities.GetDefaultTraitsMask(this);
        }
    }
}