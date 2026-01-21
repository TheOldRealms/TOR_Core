using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TOR_Core.Extensions;
using TOR_Core.Models;

namespace TOR_Core.Utilities
{
    public static class TORNotificationHelper
    {
        private const float MaxRelevantDistance = 75f;

        public static bool IsPartyRelevantToPlayer(PartyBase partyBase)
        {
            if (partyBase != null && PartyBase.MainParty != null)
            {
                var distance = partyBase.Position.Distance(PartyBase.MainParty.Position);
                if (distance <= MaxRelevantDistance)
                    return true;
            }
            return false;
        }

        public static bool IsHeroRelevantToPlayer(Hero hero)
        {
            if (hero == null) return false;
            if (hero == Hero.MainHero) return true;
            if (hero.Clan == Clan.PlayerClan) return true;

            if (hero.Clan?.Kingdom != null)
            {
                if (hero.Clan.Kingdom == Clan.PlayerClan?.Kingdom) return true;
                if (IsKingdomRelevantToPlayer(hero.Clan.Kingdom)) return true;
            }

            if (hero.PartyBelongedTo != null)
            {
                if (IsPartyRelevantToPlayer(hero.PartyBelongedTo.Party))
                    return true;
            }
            return false;
        }

        public static bool IsKingdomRelevantToPlayer(Kingdom kingdom)
        {
            if (kingdom == null) return false;

            var playerKingdom = Clan.PlayerClan?.Kingdom;
            if (playerKingdom == null) return false;
            if (kingdom == playerKingdom) return true;

            var distance = DiplomacyHelpers.GetKingdomDistance(kingdom, playerKingdom);
            if (distance <= MaxRelevantDistance)
                return true;

            if (playerKingdom.IsAtWarWith(kingdom)) return true;
            if (playerKingdom.IsAllyWith(kingdom)) return true;
            if (playerKingdom.HasTradeAgreementWith(kingdom)) return true;

            return false;
        }

        public static bool AreKingdomsRelevantToPlayer(params Kingdom[] kingdoms)
        {
            return kingdoms.Any(IsKingdomRelevantToPlayer);
        }

        public static bool IsFactionRelevantToPlayer(IFaction faction)
        {
            if (faction == null) return false;
            if (faction == Clan.PlayerClan) return true;
            if (faction == Clan.PlayerClan?.Kingdom) return true;

            if (faction is Kingdom kingdom)
                return IsKingdomRelevantToPlayer(kingdom);

            if (faction is Clan clan)
                return clan.Kingdom != null && IsKingdomRelevantToPlayer(clan.Kingdom);

            return false;
        }
    }
}